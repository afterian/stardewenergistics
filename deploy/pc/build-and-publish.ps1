# Retires the old "Stardew Energistics build watcher".
# The mod pack's builds moved to Stardew Mod Master (https://github.com/afterian/stardewmodmaster),
# whose own watcher ("Stardew Mod Master build watcher") builds every mod, Energistics included.
# The old watcher runs this file after it pulls a new commit, so this turns the old scheduled task
# off and stops it. Nothing else is touched: %LOCALAPPDATA%\StardewEnergistics stays (the self-test
# still uses it). Delete this file once the old watcher is gone from every PC.
param([string]$Source = "", [string]$Label = "")
$task = "Stardew Energistics build watcher"
if (Get-ScheduledTask -TaskName $task -ErrorAction SilentlyContinue) {
  Write-Host "The pack moved to Stardew Mod Master; turning off the old '$task'."
  Disable-ScheduledTask -TaskName $task -ErrorAction SilentlyContinue | Out-Null
  # Stop it from a separate process, after this build step has returned and been logged.
  Start-Process powershell.exe -WindowStyle Hidden -ArgumentList "-NoProfile", "-Command",
    "Start-Sleep 5; Stop-ScheduledTask -TaskName '$task'"
}
exit 0
