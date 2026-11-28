# Stardew Energistics - in-game self-test, start to finish, with one command.
# Builds the mod (or uses a zip you give it), puts it in a separate Mods folder (your
# normal Mods folder is not touched), starts Stardew through SMAPI with SE_SELFTEST=1,
# waits while the mod loads your most recent save, runs every scenario in a scratch
# location and quits WITHOUT saving, then prints the report.
#
#   deploy\selftest.bat                         build this folder and test it
#   powershell -File deploy\pc\selftest.ps1 -Only disks,terminal
#   deploy\showcase.bat                         build a whole base slowly, with a screenshot per step
#   powershell -File deploy\pc\selftest.ps1 -Save "Farm_123456789"
#
# Exit code: 0 all passed, 1 something failed, 2 couldn't run (game open, no save, timeout, build failed).
# Windows PowerShell 5.1.
param(
  [string]$Source = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path,
  [string]$ZipDir = "",          # use the mod zip in this folder instead of building
  [string]$Save = "",            # save folder name; default: the most recently played save
  [string]$Only = "",            # comma-separated scenario IDs; default: all
  [string]$GamePath = "",
  [int]$TimeoutMinutes = 6,
  [switch]$Showcase,             # run the slow showcase (screenshots in %LOCALAPPDATA%\StardewEnergistics\selftest\shots); the game stays open afterwards
  [switch]$Close                 # with -Showcase: close the game at the end anyway
)
$ErrorActionPreference = "Continue"
$SEDir = Join-Path $env:LOCALAPPDATA "StardewEnergistics"

$work   = Join-Path $SEDir "selftest"
$mods   = Join-Path $work "Mods"
$report = Join-Path $work "report.txt"
New-Item -ItemType Directory -Force -Path $work | Out-Null

function Done([int]$code, [string]$text) {
  if ($code -eq 0) { Write-Host $text -ForegroundColor Green } else { Write-Host $text -ForegroundColor Red }
  exit $code
}

# ---- Find the game
# The pack's PC setup (Stardew Mod Master, or the older Energistics one) remembers the game folder.
foreach ($f in @((Join-Path $env:LOCALAPPDATA "StardewModMaster\settings.json"), (Join-Path $SEDir "settings.json"))) {
  if (-not $GamePath -and (Test-Path $f)) { try { $GamePath = (Get-Content $f -Raw | ConvertFrom-Json).GamePath } catch { } }
}
if (-not $GamePath) {
  $targets = Join-Path $env:USERPROFILE "stardewvalley.targets"
  if (Test-Path $targets) {
    $m = [regex]::Match((Get-Content $targets -Raw), '<GamePath>([^<]+)</GamePath>')
    if ($m.Success) { $GamePath = $m.Groups[1].Value.Trim() }
  }
}
if (-not $GamePath) { Done 2 "Game path not found. Pass -GamePath, or set up Stardew Mod Master on this PC." }
$smapi = Join-Path $GamePath "StardewModdingAPI.exe"
if (-not (Test-Path $smapi)) { Done 2 "SMAPI not found at $smapi. Pass -GamePath." }

# ---- The game must not be running already (it would be your own play session)
if (Get-Process -Name "StardewModdingAPI", "Stardew Valley" -ErrorAction SilentlyContinue) {
  Done 2 "Stardew Valley is already running. Close it and run the self-test again."
}

# ---- Build (or take the given zip)
if (-not $ZipDir) {
  $ZipDir = Join-Path $work "zip"
  if (Test-Path $ZipDir) { Remove-Item $ZipDir -Recurse -Force }
  $buildLog = Join-Path $work "build.log"
  Write-Host "Building the mod from $Source ..."
  & dotnet build (Join-Path $Source "src\StardewEnergistics\StardewEnergistics.csproj") -c Release `
      "-p:EnableModDeploy=false" "-p:ModZipPath=$ZipDir" "-p:GamePath=$GamePath" *> $buildLog
  if ($LASTEXITCODE -ne 0) { Get-Content $buildLog -Tail 30; Done 2 "Build failed; log: $buildLog" }
}
$zip = Get-ChildItem $ZipDir -Filter *.zip -ErrorAction SilentlyContinue | Sort-Object LastWriteTime -Descending | Select-Object -First 1
if (-not $zip) { Done 2 "No mod zip in $ZipDir." }

# ---- A Mods folder with only this mod in it
if (Test-Path $mods) { Remove-Item $mods -Recurse -Force }
New-Item -ItemType Directory -Force -Path $mods | Out-Null
Expand-Archive -Path $zip.FullName -DestinationPath $mods -Force

# ---- Run
$shots = Join-Path $work "shots"
if ($Showcase) {
  $Only = "showcase"
  if ($TimeoutMinutes -lt 10) { $TimeoutMinutes = 10 }
  if (Test-Path $shots) { Remove-Item $shots -Recurse -Force }
}
$env:SE_SELFTEST_SHOTS = $shots
Remove-Item $report, ([IO.Path]::ChangeExtension($report, ".json")) -ErrorAction SilentlyContinue
$env:SE_SELFTEST = "1"
$env:SE_SELFTEST_REPORT = $report
$env:SE_SELFTEST_SAVE = $Save
$env:SE_SELFTEST_ONLY = $Only
$keepOpen = $Showcase -and -not $Close
$env:SE_SELFTEST_HOLD = if ($keepOpen) { "1" } else { "" }
Remove-Item (Join-Path $work "selftest-crash.txt") -ErrorAction SilentlyContinue
if ($keepOpen) {
  Write-Host "Starting Stardew Valley for the showcase. Watch it build; when it's done the game stays open for you to try (nothing is saved; sleeping closes the game) ..."
} else {
  Write-Host "Starting Stardew Valley for the self-test (it closes by itself; please don't touch it) ..."
}
$started = Get-Date
$proc = Start-Process -FilePath $smapi -ArgumentList @("--mods-path", "`"$mods`"") -WorkingDirectory $GamePath -PassThru
$deadline = $started.AddMinutes($TimeoutMinutes)
$finished = $false
while ((Get-Date) -lt $deadline) {
  if ($proc.WaitForExit(2000)) { $finished = $true; break }
  # Showcase: the game stays open, so stop waiting once the report is written.
  if ($keepOpen -and (Test-Path $report) -and ((Get-Item $report).LastWriteTime -ge $started)) { $finished = $true; Start-Sleep -Seconds 2; break }
}
if (-not $finished) {
  Write-Host "Timed out after $TimeoutMinutes minutes; closing the game."
  Stop-Process -Id $proc.Id -Force -ErrorAction SilentlyContinue
  Get-Process -Name "Stardew Valley" -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
}
Remove-Item Env:SE_SELFTEST, Env:SE_SELFTEST_REPORT, Env:SE_SELFTEST_SAVE, Env:SE_SELFTEST_ONLY, Env:SE_SELFTEST_SHOTS, Env:SE_SELFTEST_HOLD -ErrorAction SilentlyContinue

# Keep the SMAPI log next to the report.
$smapiLog = Join-Path $env:APPDATA "StardewValley\ErrorLogs\SMAPI-latest.txt"
if (Test-Path $smapiLog) { Copy-Item $smapiLog (Join-Path $work "SMAPI-latest.txt") -Force }

# ---- Result
$code = 2
$summary = "No report: the game closed or timed out before the self-test finished. See $work\SMAPI-latest.txt"
if ((Test-Path $report) -and ((Get-Item $report).LastWriteTime -ge $started)) {
  $text = Get-Content $report -Raw
  Write-Host ""
  Write-Host $text
  $line = ($text -split "`n" | Where-Object { $_ -like "SE-SELFTEST RESULT:*" } | Select-Object -First 1)
  if ($line) { $summary = $line.Trim() }
  $code = if ($line -like "*RESULT: PASS*") { 0 } else { 1 }
  $crash = Join-Path $work "selftest-crash.txt"
  if (Test-Path $crash) { Write-Host "Where it stopped (also in $crash):" -ForegroundColor Yellow; Get-Content $crash | Select-Object -First 40 | Write-Host }
} else {
  # Leave the reason in the report file too, for Claude.
  Set-Content -Path $report -Value "SE-SELFTEST RESULT: FAIL (did not finish)`n$summary"
}

if ($Showcase) { Write-Host "Screenshots: $shots" }

Done $code $summary
