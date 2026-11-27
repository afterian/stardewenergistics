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
| `pc/build-and-publish.ps1`, `pc/common.ps1` | Builds (version `x.y.z-build.N`, N = commit count), uploads the zip without a gap, attaches `last-build-log.txt`, and sets a `pc-build` tick or cross on the commit. |
| `publish-test.bat` | Build whatever is in the project folder now (any branch) and send it to the Decks. |
| `deck/Install-Stardew-Energistics.desktop` | Double-click installer for the Deck (public repo). Downloads and runs `install.sh`. Also published as an asset of the `deck-installer` release, so a browser downloads it instead of showing it as text. |
| `deck/install.sh` | One-time Deck setup with no typing: checks GitHub access (asks for the token in a popup if the repo is private), installs or updates SMAPI, installs the launcher, and sets Stardew's Launch Options. It closes and reopens Steam to do that, and backs up `localconfig.vdf`. Re-runnable. |
| `deck/set_launch_options.py` | Edits Steam's `localconfig.vdf` to set app 413150's LaunchOptions (Steam must be closed). |
| `deck/launch.sh` | Steam launch option `~/stardew-energistics/launch.sh %command%`. Installs a newer build (keeps `config.json`), updates itself, starts the game through SMAPI. Offline-safe. Log: `~/stardew-energistics/update.log`. |

Secrets: the PC uses the GitHub CLI's sign-in. The Deck token, needed only while the repo is private, lives only in `~/stardew-energistics/config` on each Deck.
