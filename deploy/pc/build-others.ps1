# Stardew Energistics - build the other mods of the pack (deploy/mods.json).
# Called by the watcher every minute, after its own Energistics check. For each
# other mod: fetch its branch into its own build copy, and if there's a new commit,
# build it and publish <name>-test.zip to this repo's test-latest release.
# One mod failing (or its repo missing) never stops the others. Windows PowerShell 5.1.
$ErrorActionPreference = "Continue"
. (Join-Path $PSScriptRoot "common.ps1")
$s = Get-Settings
$list = Join-Path $PSScriptRoot "..\mods.json"
if (-not (Test-Path $list)) { exit 0 }
$mods = (Get-Content $list -Raw | ConvertFrom-Json).mods

foreach ($mod in $mods) {
  if ($mod.repo -eq $s.Repo) { continue }   # Energistics itself is built by the watcher
  $name = $mod.name
  $branch = if ($mod.branch) { $mod.branch } else { "main" }
  $work = Join-Path $SEDir "build-$name"
  $state = Join-Path $SEDir "last-built-$name.txt"
  try {
    if (-not (Test-Path (Join-Path $work ".git"))) {
      Write-Host "$(Get-Date -Format 'yyyy-MM-dd HH:mm:ss') ${name}: making a build copy of $($mod.repo)"
      git clone --quiet "https://github.com/$($mod.repo).git" $work 2>&1 | Out-Null
      if (-not (Test-Path (Join-Path $work ".git"))) { Write-Host "  ${name}: couldn't clone $($mod.repo); skipping"; continue }
    }
    git -C $work fetch --quiet origin $branch 2>&1 | Out-Null
    $remote = (git -C $work rev-parse FETCH_HEAD 2>$null)
    if (-not $remote) { continue }
    $last = if (Test-Path $state) { (Get-Content $state -Raw).Trim() } else { "" }
    if ($remote -eq $last) { continue }
    git -C $work reset --quiet --hard FETCH_HEAD 2>&1 | Out-Null
    git -C $work clean -qfdx -e bin -e obj 2>&1 | Out-Null
    Write-Host "$(Get-Date -Format 'yyyy-MM-dd HH:mm:ss') ${name}: new commit $($remote.Substring(0,7)); building"
    if (-not (Test-Path (Join-Path $work $mod.project))) {
      Write-Host "  ${name}: $($mod.project) isn't in the repo yet; skipping this commit"
    } else {
      & (Join-Path $PSScriptRoot "build-and-publish.ps1") -Source $work -Label $branch -Project $mod.project -Name $name -ModRepo $mod.repo
      Write-Host $(if ($LASTEXITCODE -eq 0) { "  ${name}: published" } else { "  ${name}: build failed; waiting for its next commit" })
    }
    Set-Content -Path $state -Value $remote   # remember it either way, so a broken commit isn't rebuilt every minute
  } catch {
    Write-Host "  ${name}: error: $($_.Exception.Message)"
  }
}
