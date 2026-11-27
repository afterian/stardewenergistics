# Stardew Energistics - publish a test build by hand from this folder (any branch).
# The watcher does this automatically for 'main'; use this to test a branch or
# local changes on the Decks. The next new commit on 'main' replaces it.
$ErrorActionPreference = "Continue"
$root = Split-Path $PSScriptRoot -Parent
$branch = (git -C $root rev-parse --abbrev-ref HEAD).Trim()
Write-Host "Building '$branch' from $root ..." -ForegroundColor Cyan
& (Join-Path $PSScriptRoot "pc\build-and-publish.ps1") -Source $root -Label $branch
if ($LASTEXITCODE -ne 0) { Write-Host "Build failed. Log: $env:TEMP\StardewEnergistics-build.log" -ForegroundColor Red }
