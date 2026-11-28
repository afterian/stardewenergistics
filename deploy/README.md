# Test deploys to the Steam Decks

**Step-by-step setup guide: [docs/steam-deck-setup.md](../docs/steam-deck-setup.md).**

```
new commit on main -> PC watcher builds it -> GitHub release "test-latest" -> Deck: start the game
```

The PC only reaches out to GitHub (it checks every minute). Nothing on GitHub can trigger it. No admin rights, no service, no GitHub Actions runner.

| File | What it does |
|---|---|
| `setup-pc.bat` -> `pc/setup-pc.ps1` | One-time PC setup: installs Git/.NET 8/GitHub CLI via winget, signs in to GitHub, finds the game, makes the hidden watcher task (runs at Windows sign-in), publishes the first build. Re-runnable. |
| `pc/watch.ps1` | The watcher. Keeps its own copy of the repo in `%LOCALAPPDATA%\StardewEnergistics\build`, builds each new `main` commit once. Log: `%LOCALAPPDATA%\StardewEnergistics\watcher.log`. |
| `pc/build-and-publish.ps1`, `pc/common.ps1` | Builds (version `x.y.z-build.N`, N = commit count), uploads the zip without a gap, keeps raw logs local, and sets a `pc-build` tick or cross on the commit. |
| `selftest.bat` -> `pc/selftest.ps1` | In-game self-test in one go: builds, starts Stardew with only this mod (separate Mods folder), loads the latest save without saving, runs every scenario, quits and prints the report. `-Watcher on` makes the watcher self-test each build (`pc-selftest` status on GitHub; full report stays local). See [docs/testing/selftest.md](../docs/testing/selftest.md). |
| `mods.json`, `pc/build-others.ps1` | Temporary pack loader: the other mods the watcher builds each minute (one build copy each, `%LOCALAPPDATA%\StardewEnergistics\build-<name>`) and publishes to this repo's `test-latest` as `<name>-test.mod` (a zip). A failure in one mod is logged and skipped. The watcher restarts itself when `watch.ps1` changes. |
| `publish-test.bat` | Build whatever is in the project folder now (any branch) and send it to the Decks. |
| `deck/Install-Stardew-Energistics.desktop` | Double-click installer for the Deck (public repo). Downloads and runs `install.sh`. Download it with GitHub's **Download raw file** button; a plain link would show it as text. |
| `deck/install.sh` | One-time Deck setup with no typing: checks GitHub access (asks for the token in a popup if the repo is private), installs or updates SMAPI, installs the launcher, and sets Stardew's Launch Options. It closes and reopens Steam to do that, and backs up `localconfig.vdf`. Re-runnable. |
| `deck/set_launch_options.py` | Edits Steam's `localconfig.vdf` to set app 413150's LaunchOptions (Steam must be closed). |
| `deck/launch.sh` | Steam launch option `~/stardew-energistics/launch.sh %command%`. Updates itself first, then installs every newer mod zip on the release into its own Mods folder (keeps each `config.json`), then starts the game through SMAPI. Offline-safe. Log: `~/stardew-energistics/update.log`. |

Secrets: the PC uses the GitHub CLI's sign-in. The Deck token, needed only while the repo is private, lives only in `~/stardew-energistics/config` on each Deck.
