# Shared helpers for the PC scripts. Windows PowerShell 5.1 compatible.
$Global:SEDir = Join-Path $env:LOCALAPPDATA "StardewEnergistics"
$Global:SESettingsPath = Join-Path $Global:SEDir "settings.json"

function Get-Settings {
  if (-not (Test-Path $Global:SESettingsPath)) { throw "Not set up yet. Double-click deploy\setup-pc.bat first." }
  Get-Content $Global:SESettingsPath -Raw | ConvertFrom-Json
}

function Invoke-GhApi([string]$Method, [string]$Path, $Data) {
  # gh is signed in already; its token never leaves this PC except to GitHub.
  $headers = @{
    Authorization          = "Bearer $((gh auth token).Trim())"
    Accept                 = "application/vnd.github+json"
    "X-GitHub-Api-Version" = "2022-11-28"
  }
  [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
  $url = if ($Path -like "https://*") { $Path } else { "https://api.github.com/$Path" }
  $json = if ($null -ne $Data) { $Data | ConvertTo-Json -Depth 5 } else { $null }
  Invoke-RestMethod -Method $Method -Uri $url -Headers $headers -Body $json -ContentType "application/json" -ErrorAction Stop
}

function Upload-Asset([string]$Repo, $ReleaseId, [string]$File, [string]$Name, [string]$ContentType) {
  $headers = @{ Authorization = "Bearer $((gh auth token).Trim())"; Accept = "application/vnd.github+json" }
  $url = "https://uploads.github.com/repos/$Repo/releases/$ReleaseId/assets?name=$([uri]::EscapeDataString($Name))"
  Invoke-RestMethod -Method POST -Uri $url -Headers $headers -InFile $File -ContentType $ContentType -ErrorAction Stop
}

function Get-OrCreate-Release([string]$Repo, [string]$Tag, [string]$Sha) {
  try { return Invoke-GhApi GET "repos/$Repo/releases/tags/$Tag" $null } catch { }
  Invoke-GhApi POST "repos/$Repo/releases" @{
    tag_name = $Tag; target_commitish = $Sha; name = "Test build (Steam Decks)"
    body = "Rolling test build."; prerelease = $true; make_latest = "false"
  }
}

# Upload under a temporary name, delete the old zip, then rename, so a Deck never sees a release without a zip.
function Publish-Zip([string]$Repo, [string]$Tag, [string]$Zip, [string]$Version, [string]$Notes, [string]$Sha) {
  $rel = Get-OrCreate-Release $Repo $Tag $Sha
  Invoke-GhApi PATCH "repos/$Repo/releases/$($rel.id)" @{ body = $Notes; prerelease = $true } | Out-Null
  try { Invoke-GhApi PATCH "repos/$Repo/git/refs/tags/$Tag" @{ sha = $Sha; force = $true } | Out-Null } catch { }
  $new = Upload-Asset $Repo $rel.id $Zip "StardewEnergistics-uploading-$([guid]::NewGuid().ToString('N').Substring(0,8)).zip" "application/zip"
  foreach ($a in (Invoke-GhApi GET "repos/$Repo/releases/$($rel.id)/assets?per_page=100" $null)) {
    if ($a.id -ne $new.id -and $a.name -like "StardewEnergistics*.zip") { Invoke-GhApi DELETE "repos/$Repo/releases/assets/$($a.id)" $null | Out-Null }
  }
  Invoke-GhApi PATCH "repos/$Repo/releases/assets/$($new.id)" @{ name = "StardewEnergistics-test.zip"; label = $Version } | Out-Null
}

# The build log goes on the release too, so Claude can read build errors without the PC.
function Publish-Log([string]$Repo, [string]$Tag, [string]$Log, [string]$Sha) {
  try {
    $rel = Get-OrCreate-Release $Repo $Tag $Sha
    foreach ($a in (Invoke-GhApi GET "repos/$Repo/releases/$($rel.id)/assets?per_page=100" $null)) {
      if ($a.name -eq "last-build-log.txt") { Invoke-GhApi DELETE "repos/$Repo/releases/assets/$($a.id)" $null | Out-Null }
    }
    Upload-Asset $Repo $rel.id $Log "last-build-log.txt" "text/plain" | Out-Null
  } catch { Write-Host "Couldn't upload the build log: $($_.Exception.Message)" }
}

# Shows a green tick or red cross next to the commit on GitHub.
function Set-CommitStatus([string]$Repo, [string]$Sha, [string]$State, [string]$Text) {
  if ($Text.Length -gt 139) { $Text = $Text.Substring(0, 139) }
  try { Invoke-GhApi POST "repos/$Repo/statuses/$Sha" @{ state = $State; context = "pc-build"; description = $Text } | Out-Null } catch { }
}
