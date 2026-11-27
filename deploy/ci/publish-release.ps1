# Uploads a mod zip to the rolling "test-latest" GitHub release.
# Used by .github/workflows/deck-build.yml. Needs GITHUB_TOKEN, GITHUB_REPOSITORY
# and GITHUB_SHA (GitHub Actions sets them). Windows PowerShell 5.1 compatible.
param(
  [Parameter(Mandatory = $true)][string]$Zip,
  [Parameter(Mandatory = $true)][string]$Version,
  [string]$Tag = "test-latest"
)
$ErrorActionPreference = "Stop"
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12

$repo = $env:GITHUB_REPOSITORY
$sha  = $env:GITHUB_SHA
$api  = "https://api.github.com/repos/$repo"
$headers = @{
  Authorization          = "Bearer $env:GITHUB_TOKEN"
  Accept                 = "application/vnd.github+json"
  "X-GitHub-Api-Version" = "2022-11-28"
}
$branch = $env:GITHUB_REF_NAME
$when = (Get-Date).ToUniversalTime().ToString("yyyy-MM-dd HH:mm 'UTC'")
$body = "Version $Version, built from $branch @ $($sha.Substring(0,7)) on $when.`n`nRolling test build for the Steam Decks. Not for players."

function Invoke-Gh($Method, $Url, $Data) {
  $json = if ($Data) { $Data | ConvertTo-Json -Depth 5 } else { $null }
  Invoke-RestMethod -Method $Method -Uri $Url -Headers $headers -Body $json -ContentType "application/json"
}

$release = $null
try { $release = Invoke-Gh GET "$api/releases/tags/$Tag" $null } catch { }
if ($release) {
  $release = Invoke-Gh PATCH "$api/releases/$($release.id)" @{ body = $body; prerelease = $true }
} else {
  $release = Invoke-Gh POST "$api/releases" @{
    tag_name = $Tag; target_commitish = $sha; name = "Test build (Steam Decks)"
    body = $body; prerelease = $true; make_latest = "false"
  }
}

# Point the tag at this commit (best effort; the Decks only look at the asset).
try { Invoke-Gh PATCH "$api/git/refs/tags/$Tag" @{ sha = $sha; force = $true } | Out-Null }
catch { Write-Warning "Could not move tag $Tag to $sha (harmless): $($_.Exception.Message)" }

# Upload under a temporary name first, then swap, so a Deck never sees a release without a zip.
$name = "StardewEnergistics-test.zip"
$temp = "StardewEnergistics-uploading-$($env:GITHUB_RUN_ID).zip"
$uploadUrl = "https://uploads.github.com/repos/$repo/releases/$($release.id)/assets?name=$temp"
$new = Invoke-RestMethod -Method POST -Uri $uploadUrl -Headers $headers -InFile $Zip -ContentType "application/zip"

$assets = Invoke-Gh GET "$api/releases/$($release.id)/assets?per_page=100" $null
foreach ($a in $assets) {
  if ($a.id -ne $new.id -and $a.name -like "StardewEnergistics*.zip") {
    Invoke-Gh DELETE "$api/releases/assets/$($a.id)" $null | Out-Null
  }
}
Invoke-Gh PATCH "$api/releases/assets/$($new.id)" @{ name = $name; label = $Version } | Out-Null

Write-Host "Published $Version to release $Tag. Start the game on each Deck to get it."
