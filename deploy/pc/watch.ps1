# Stardew Energistics - build watcher. Started hidden at Windows sign-in by the
# scheduled task "Stardew Energistics build watcher" (made by setup-pc.bat).
# Every minute it asks GitHub whether 'main' has a new commit. If so it builds
# that commit in its own copy of the repo and publishes it for the Decks.
# It only ever reaches out to GitHub; nothing on GitHub can start anything here.
$ErrorActionPreference = "Continue"
. (Join-Path $PSScriptRoot "common.ps1")
$s = Get-Settings
$work  = Join-Path $SEDir "build"
$state = Join-Path $SEDir "last-built.txt"
$log   = Join-Path $SEDir "watcher.log"
function Log($t) {
  Add-Content -Path $log -Value "$(Get-Date -Format 'yyyy-MM-dd HH:mm:ss') $t"
  if ((Get-Item $log).Length -gt 1MB) { Get-Content $log -Tail 500 | Set-Content "$log.tmp"; Move-Item "$log.tmp" $log -Force }
}

# Only one watcher at a time.
$mutex = New-Object Threading.Mutex($false, "Local\StardewEnergisticsWatcher")
if (-not $mutex.WaitOne(0)) { exit 0 }

Log "watcher started (following $($s.Branch) of $($s.Repo), plus the other mods in deploy\mods.json)"
# Lets build-and-publish.ps1 notice when a new commit changes this script (it then restarts the task).
Set-Content -Path (Join-Path $SEDir "watcher-running.hash") -Value (Get-FileHash $PSCommandPath -Algorithm SHA256).Hash
while ($true) {
  try {
    if (-not (Test-Path (Join-Path $work ".git"))) {
      Log "making a build copy of the repo in $work"
      git clone --quiet "https://github.com/$($s.Repo).git" $work 2>&1 | Out-Null
    }
    Push-Location $work
    git fetch --quiet origin $s.Branch 2>&1 | Out-Null
    $remote = (git rev-parse FETCH_HEAD 2>$null)
    $last = if (Test-Path $state) { (Get-Content $state -Raw).Trim() } else { "" }
    if ($remote -and $remote -ne $last) {
      git reset --quiet --hard FETCH_HEAD 2>&1 | Out-Null
      git clean -qfdx -e bin -e obj 2>&1 | Out-Null
      Log "new commit $($remote.Substring(0,7)); building"
      Pop-Location
      & (Join-Path $PSScriptRoot "build-and-publish.ps1") -Source $work -Label $s.Branch *>> $log
      Log $(if ($LASTEXITCODE -eq 0) { "published $($remote.Substring(0,7))" } else { "build failed for $($remote.Substring(0,7)); waiting for the next commit" })
      # Optional: run the in-game self-test on the new build (deploy\selftest.bat -Watcher on).
      # Skipped while you're playing, so it never takes over the game.
      $s = Get-Settings
      if ($LASTEXITCODE -eq 0 -and $s.SelfTest -eq $true) {
        if (Get-Process -Name "StardewModdingAPI", "Stardew Valley" -ErrorAction SilentlyContinue) {
          Log "self-test skipped for $($remote.Substring(0,7)): the game is open"
        } else {
          Log "self-testing $($remote.Substring(0,7))"
          & (Join-Path $PSScriptRoot "selftest.ps1") -Source $work -ZipDir (Join-Path $env:TEMP "StardewEnergistics-zip") -Commit $remote -Publish *>> $log
          Log "self-test exit code $LASTEXITCODE for $($remote.Substring(0,7))"
        }
      }
      # Remember it either way, so a broken commit isn't rebuilt every minute.
      Set-Content -Path $state -Value $remote
    } else {
      Pop-Location
    }
  } catch {
    Log "error: $($_.Exception.Message)"
    try { Pop-Location } catch { }
  }
  # The other mods of the pack (deploy\mods.json). Separate script, so a problem there never stops Energistics.
  try { & (Join-Path $PSScriptRoot "build-others.ps1") *>> $log } catch { Log "other mods: $($_.Exception.Message)" }
  Start-Sleep -Seconds 60
}
