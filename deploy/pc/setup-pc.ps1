# Stardew Energistics - one-time PC setup (double-click deploy\setup-pc.bat).
# No administrator rights and no Windows service. It:
#   1. installs Git, the .NET 8 SDK and the GitHub CLI if they're missing (winget)
#   2. signs in to GitHub (opens the browser once)
#   3. finds Stardew Valley
#   4. sets up the build watcher: a hidden task that starts when you sign in to
#      Windows, checks GitHub every minute, and builds + publishes new commits on main
#   5. publishes the first build
# Safe to run again (for example after moving the game to another drive).
$ErrorActionPreference = "Continue"
$Repo     = "afterian/stardewenergistics"
$Branch   = "main"
$TaskName = "Stardew Energistics build watcher"
$Dir      = Join-Path $env:LOCALAPPDATA "StardewEnergistics"
$Work     = Join-Path $Dir "build"

function Step($n, $text) { Write-Host ""; Write-Host "[$n/5] $text" -ForegroundColor Cyan }
function Ok($text)       { Write-Host "  $text" -ForegroundColor Green }
function Fail($text)     { Write-Host ""; Write-Host "  PROBLEM: $text" -ForegroundColor Red; Write-Host "  Send Claude a screenshot of this window."; exit 1 }
function Refresh-Path    { $env:Path = [Environment]::GetEnvironmentVariable("Path","Machine") + ";" + [Environment]::GetEnvironmentVariable("Path","User") }
function Has($cmd)       { [bool](Get-Command $cmd -ErrorAction SilentlyContinue) }

Write-Host "Stardew Energistics - PC setup" -ForegroundColor White
New-Item -ItemType Directory -Force -Path $Dir | Out-Null

# ---------------------------------------------------------------- 1. tools
Step 1 "Checking tools"
$sdk = $false
if (Has dotnet) { $sdk = [bool]((& dotnet --list-sdks 2>$null) -match '^(8|9|10)\.') }
$need = @()
if (-not (Has git)) { $need += @{ id = "Git.Git";                name = "Git" } }
if (-not $sdk)      { $need += @{ id = "Microsoft.DotNet.SDK.8"; name = ".NET 8 SDK" } }
if (-not (Has gh))  { $need += @{ id = "GitHub.cli";             name = "GitHub CLI" } }
if ($need.Count -gt 0) {
  if (-not (Has winget)) { Fail "winget is missing. Install 'App Installer' from the Microsoft Store, then run this again." }
  foreach ($p in $need) {
    Write-Host "  Installing $($p.name) (Windows may ask for permission; click Yes)..."
    winget install --id $p.id -e --silent --accept-source-agreements --accept-package-agreements
    if ($LASTEXITCODE -ne 0 -and $LASTEXITCODE -ne -1978335189) { Fail "Installing $($p.name) failed (code $LASTEXITCODE)." }
  }
  Refresh-Path
}
foreach ($c in "git", "dotnet", "gh") { if (-not (Has $c)) { Fail "$c still isn't found. Restart the PC and run this again." } }
Ok "Git, .NET SDK and GitHub CLI are ready."

# ---------------------------------------------------------------- 2. GitHub sign-in
Step 2 "Signing in to GitHub"
gh auth status 2>&1 | Out-Null
if ($LASTEXITCODE -ne 0) {
  Write-Host "  A browser window opens. Copy the code shown here, paste it there, and approve."
  gh auth login --hostname github.com --git-protocol https --web
  if ($LASTEXITCODE -ne 0) { Fail "GitHub sign-in didn't finish." }
}
gh auth setup-git 2>&1 | Out-Null
$push = gh api "repos/$Repo" --jq ".permissions.push" 2>$null
if ($push -ne "true") { Fail "This GitHub account can't publish to $Repo. Run 'gh auth logout', then this again, and sign in as the repo owner." }
$isPrivate = (gh api "repos/$Repo" --jq ".private") -eq "true"
Ok ("Signed in. The repo is " + $(if ($isPrivate) { "private." } else { "public." }))

# ---------------------------------------------------------------- 3. game folder
Step 3 "Finding Stardew Valley"
function Find-Game {
  $roots = @()
  foreach ($k in "HKCU:\Software\Valve\Steam", "HKLM:\SOFTWARE\WOW6432Node\Valve\Steam", "HKLM:\SOFTWARE\Valve\Steam") {
    $p = Get-ItemProperty $k -ErrorAction SilentlyContinue
    if ($p.SteamPath)   { $roots += $p.SteamPath }
    if ($p.InstallPath) { $roots += $p.InstallPath }
  }
  $roots += "C:\Program Files (x86)\Steam"
  $libs = @()
  foreach ($root in ($roots | Select-Object -Unique)) {
    $root = $root -replace '/', '\'
    $libs += $root
    $vdf = Join-Path $root "steamapps\libraryfolders.vdf"
    if (Test-Path $vdf) {
      foreach ($m in [regex]::Matches((Get-Content $vdf -Raw), '"path"\s+"([^"]+)"')) { $libs += ($m.Groups[1].Value -replace '\\\\', '\') }
    }
  }
  foreach ($lib in ($libs | Select-Object -Unique)) {
    $g = Join-Path $lib "steamapps\common\Stardew Valley"
    if (Test-Path (Join-Path $g "Stardew Valley.dll")) { return $g }
  }
  foreach ($g in "C:\Program Files\GOG Galaxy\Games\Stardew Valley", "C:\GOG Games\Stardew Valley") {
    if (Test-Path (Join-Path $g "Stardew Valley.dll")) { return $g }
  }
  return $null
}
$game = Find-Game
if (-not $game) {
  Write-Host "  Couldn't find it by itself. In Steam: right-click Stardew Valley > Manage > Browse local files,"
  Write-Host "  click the address bar, copy the path and paste it here."
  $game = (Read-Host "  Game folder").Trim('"', ' ')
  if (-not (Test-Path (Join-Path $game "Stardew Valley.dll"))) { Fail "That folder doesn't contain 'Stardew Valley.dll'." }
}
@{ Repo = $Repo; Branch = $Branch; GamePath = $game } | ConvertTo-Json | Set-Content (Join-Path $Dir "settings.json")
Ok "Game: $game"

# ---------------------------------------------------------------- 4. watcher
Step 4 "Setting up the build watcher"
if (-not (Test-Path (Join-Path $Work ".git"))) {
  git clone --quiet "https://github.com/$Repo.git" $Work
  if ($LASTEXITCODE -ne 0) { Fail "Couldn't copy the repo from GitHub." }
}
git -C $Work fetch --quiet origin $Branch
git -C $Work reset --quiet --hard FETCH_HEAD
$watch = Join-Path $Work "deploy\pc\watch.ps1"
if (-not (Test-Path $watch)) { Fail "The watcher isn't on '$Branch' yet. Merge the Steam Deck pull request first, then run this again." }

Get-ScheduledTask -TaskName $TaskName -ErrorAction SilentlyContinue | ForEach-Object {
  Stop-ScheduledTask -TaskName $TaskName -ErrorAction SilentlyContinue
  Unregister-ScheduledTask -TaskName $TaskName -Confirm:$false
}
$action   = New-ScheduledTaskAction -Execute "powershell.exe" -Argument "-NoProfile -ExecutionPolicy Bypass -WindowStyle Hidden -File `"$watch`""
$trigger  = New-ScheduledTaskTrigger -AtLogOn -User "$env:USERDOMAIN\$env:USERNAME"
$settings = New-ScheduledTaskSettingsSet -AllowStartIfOnBatteries -DontStopIfGoingOnBatteries -ExecutionTimeLimit ([TimeSpan]::Zero) `
              -RestartCount 3 -RestartInterval (New-TimeSpan -Minutes 1) -MultipleInstances IgnoreNew
$principal = New-ScheduledTaskPrincipal -UserId "$env:USERDOMAIN\$env:USERNAME" -LogonType Interactive -RunLevel Limited
Register-ScheduledTask -TaskName $TaskName -Action $action -Trigger $trigger -Settings $settings -Principal $principal `
  -Description "Builds new Stardew Energistics commits and publishes them for the Steam Decks." | Out-Null
if (-not (Get-ScheduledTask -TaskName $TaskName -ErrorAction SilentlyContinue)) { Fail "Couldn't create the scheduled task." }
Ok "Watcher installed. It starts hidden whenever you sign in to Windows."

# ---------------------------------------------------------------- 5. first build
Step 5 "Building and publishing the first build (a minute or two)"
Remove-Item (Join-Path $Dir "last-built.txt") -ErrorAction SilentlyContinue
& (Join-Path $Work "deploy\pc\build-and-publish.ps1") -Source $Work -Label $Branch
$built = ($LASTEXITCODE -eq 0)
if ($built) { Set-Content (Join-Path $Dir "last-built.txt") (git -C $Work rev-parse HEAD) }
Start-ScheduledTask -TaskName $TaskName
if (-not $built) { Fail "The first build failed. The log is at $env:TEMP\StardewEnergistics-build.log (and on the GitHub release)." }

Write-Host ""
Write-Host "PC setup done. New commits on main now reach the Decks by themselves while you're signed in." -ForegroundColor Green
if ($isPrivate) { Write-Host "Next: make the read-only Deck token (guide part 2), then set up each Deck (part 3)." }
else            { Write-Host "Next: set up each Deck (guide part 3). No token needed because the repo is public." }
