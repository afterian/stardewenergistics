# Stardew Energistics - publish a test build for the Steam Decks.
# Double-click publish-test.bat (or run this in PowerShell from the repo root).
# Needs: .NET 6 SDK, GitHub CLI (gh) signed in with `gh auth login`.
$ErrorActionPreference = "Stop"
Set-Location (Split-Path $PSScriptRoot -Parent)

$proj = "src\StardewEnergistics\StardewEnergistics.csproj"
$tag  = "test-latest"
$out  = "deploy\StardewEnergistics-test.zip"

Write-Host "1/3 Building..." -ForegroundColor Cyan
dotnet build $proj -c Release
if ($LASTEXITCODE -ne 0) { throw "Build failed - see errors above." }

$zip = Get-ChildItem "src\StardewEnergistics\bin\Release" -Recurse -Filter *.zip |
       Sort-Object LastWriteTime -Descending | Select-Object -First 1
if (-not $zip) { throw "No mod zip found. Is EnableModZip on in the .csproj?" }
Copy-Item $zip.FullName $out -Force

Write-Host "2/3 Making sure the '$tag' release exists..." -ForegroundColor Cyan
gh release view $tag *> $null
if ($LASTEXITCODE -ne 0) {
  gh release create $tag --prerelease --title "Test build (Steam Decks)" --notes "Rolling test build."
}

Write-Host "3/3 Uploading..." -ForegroundColor Cyan
gh release upload $tag $out --clobber
$commit = (git rev-parse --short HEAD) 2>$null
gh release edit $tag --notes "Built from $commit on $(Get-Date -Format 'yyyy-MM-dd HH:mm')."

Write-Host "Done. Start the game on each Deck to get this build." -ForegroundColor Green
