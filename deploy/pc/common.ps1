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
function Publish-Zip([string]$Repo, [string]$Tag, [string]$Zip, [string]$Version, [string]$Notes, [string]$Sha, [string]$AssetBase = "StardewEnergistics", [switch]$KeepNotes, [string]$Ext = ".zip") {
  $rel = Get-OrCreate-Release $Repo $Tag $Sha
  # Other mods in the pack share this release; only this repo's own builds rewrite the notes and move the tag.
  if (-not $KeepNotes) {
    Invoke-GhApi PATCH "repos/$Repo/releases/$($rel.id)" @{ body = $Notes; prerelease = $true } | Out-Null
    try { Invoke-GhApi PATCH "repos/$Repo/git/refs/tags/$Tag" @{ sha = $Sha; force = $true } | Out-Null } catch { }
  }
  $new = Upload-Asset $Repo $rel.id $Zip "$AssetBase-uploading-$([guid]::NewGuid().ToString('N').Substring(0,8))$Ext" "application/zip"
  foreach ($a in (Invoke-GhApi GET "repos/$Repo/releases/$($rel.id)/assets?per_page=100" $null)) {
    if ($a.id -ne $new.id -and ($a.name -eq "$AssetBase-test$Ext" -or $a.name -like "$AssetBase-uploading-*$Ext")) { Invoke-GhApi DELETE "repos/$Repo/releases/assets/$($a.id)" $null | Out-Null }
  }
  Invoke-GhApi PATCH "repos/$Repo/releases/assets/$($new.id)" @{ name = "$AssetBase-test$Ext"; label = $Version } | Out-Null
}

# Shows a green tick or red cross next to the commit on GitHub.
function Set-CommitStatus([string]$Repo, [string]$Sha, [string]$State, [string]$Text, [string]$Context = "pc-build") {
  if ($Text.Length -gt 139) { $Text = $Text.Substring(0, 139) }
  try { Invoke-GhApi POST "repos/$Repo/statuses/$Sha" @{ state = $State; context = $Context; description = $Text } | Out-Null } catch { }
}

# The watcher runs its script from its own build copy. When a new commit changes watch.ps1,
# restart the watcher task so the new version runs, after the old one has finished this build
# (it writes last-built.txt last, after any self-test). Does nothing outside the watcher.
function Update-Watcher([string]$Source, [string]$Sha) {
  try {
    $work = Join-Path $Global:SEDir "build"
    if ((Resolve-Path $Source).Path.TrimEnd('\') -ne (Resolve-Path $work -ErrorAction Stop).Path.TrimEnd('\')) { return }
    $script = Join-Path $work "deploy\pc\watch.ps1"
    $hashFile = Join-Path $Global:SEDir "watcher-running.hash"
    $now = (Get-FileHash $script -Algorithm SHA256).Hash
    $running = if (Test-Path $hashFile) { (Get-Content $hashFile -Raw).Trim() } else { "" }
    if ($now -eq $running) { return }
    $state = Join-Path $Global:SEDir "last-built.txt"
    $cmd = "`$end=(Get-Date).AddMinutes(20); while ((Get-Date) -lt `$end) { if ((Test-Path '$state') -and ((Get-Content '$state' -Raw).Trim() -eq '$Sha')) { break }; Start-Sleep 5 }; " +
           "Stop-ScheduledTask -TaskName 'Stardew Energistics build watcher'; Start-Sleep 3; Start-ScheduledTask -TaskName 'Stardew Energistics build watcher'"
    Start-Process powershell.exe -WindowStyle Hidden -ArgumentList "-NoProfile", "-ExecutionPolicy", "Bypass", "-Command", $cmd
    Write-Host "The watcher itself changed; it restarts with the new version after this build."
  } catch { }
}
