# Stardew Energistics - build the mod from a folder and publish it to the
# GitHub release "test-latest", where the Steam Decks pick it up.
# Used by the watcher (watch.ps1) and by publish-test.bat. Windows PowerShell 5.1.
#   -Source  folder with the repo checkout to build
#   -Label   text for the release notes (branch name)
# Needs: dotnet, gh (signed in). Settings come from %LOCALAPPDATA%\StardewEnergistics\settings.json.
param(
  [Parameter(Mandatory = $true)][string]$Source,
  [string]$Label = "main",
  [string]$Tag = "test-latest"
)
$ErrorActionPreference = "Continue"
. (Join-Path $PSScriptRoot "common.ps1")
$s = Get-Settings
$repo = $s.Repo

Push-Location $Source
try {
  $sha   = (git rev-parse HEAD).Trim()
  $count = (git rev-list --count HEAD).Trim()
  $dirty = [bool](git status --porcelain --untracked-files=no)

  # Version x.y.z-build.N, N = number of commits, so it only goes up on main.
  $manifestPath = "src\StardewEnergistics\manifest.json"
  $original = [IO.File]::ReadAllText((Resolve-Path $manifestPath))
  $m = [regex]::Match($original, '"Version"\s*:\s*"([^"]+)"')
  if (-not $m.Success) { throw "No Version in $manifestPath" }
  $base = $m.Groups[1].Value -replace '-.*$', ''
  $version = "$base-build.$count"
  if ($Label -ne "main") { $version += "." + (($Label -replace '[^A-Za-z0-9]', '')).ToLower() }
  if ($dirty) { $version += ".local" }
  $stamped = $original.Replace($m.Value, '"Version": "' + $version + '"')
  [IO.File]::WriteAllText((Resolve-Path $manifestPath), $stamped, (New-Object Text.UTF8Encoding $false))

  $out = Join-Path $env:TEMP "StardewEnergistics-zip"
  if (Test-Path $out) { Remove-Item $out -Recurse -Force }
  $log = Join-Path $env:TEMP "StardewEnergistics-build.log"
  & dotnet build "src\StardewEnergistics\StardewEnergistics.csproj" -c Release --no-incremental `
      "-p:DebugType=None" "-p:DebugSymbols=false" "-p:PathMap=$((Get-Location).Path)=/src" `
      "-p:EnableModDeploy=false" "-p:ModZipPath=$out" "-p:GamePath=$($s.GamePath)" *> $log
  $ok = ($LASTEXITCODE -eq 0)
  [IO.File]::WriteAllText((Resolve-Path $manifestPath), $original, (New-Object Text.UTF8Encoding $false))
} finally { Pop-Location }

$short = $sha.Substring(0, 7)
$when = [DateTime]::UtcNow.ToString("yyyy-MM-dd HH:mm") + " UTC"
if (-not $ok) {
  Set-CommitStatus $repo $sha "failure" "PC build failed ($version). See the local build log."
  Write-Host "Build FAILED for $short. Log: $log" -ForegroundColor Red
  exit 1
}
$zip = Get-ChildItem $out -Filter *.zip | Select-Object -First 1
if (-not $zip) { Set-CommitStatus $repo $sha "failure" "Build made no zip"; exit 1 }

$notes = "Version $version, built from $Label @ $short on $when.`n`nRolling test build for the Steam Decks. Not for players."
Publish-Zip $repo $Tag $zip.FullName $version $notes $sha
Set-CommitStatus $repo $sha "success" "Published $version to test-latest"
Write-Host "Published $version ($short). Start the game on each Deck to get it." -ForegroundColor Green
exit 0
