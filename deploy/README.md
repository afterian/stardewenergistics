# Test deploys to the Steam Decks

**Full step-by-step setup guide: [docs/steam-deck-setup.md](../docs/steam-deck-setup.md).**

Each Deck checks GitHub every time Stardew starts and installs the newest test build. Builds are made automatically on your PC whenever `main` changes.

```
push to main -> PC (self-hosted runner) builds -> GitHub release "test-latest" -> Deck: start the game
```

| File | What it does |
|---|---|
| `../.github/workflows/deck-build.yml` | Builds on the PC (runner label `stardew`) on every push to `main` or by hand ("Run workflow", any branch). Stamps the version as `x.y.z-build.N`. |
| `ci/publish-release.ps1` | Used by the workflow: uploads the zip to release `test-latest` without a gap where the Decks see no zip. |
| `deck/install.sh` | One-time Deck setup: checks the token, saves `~/stardew-energistics/config`, downloads the launcher. Re-run it to change the token. |
| `deck/launch.sh` | Steam launch option `~/stardew-energistics/launch.sh %command%`. Installs a newer build (keeps `config.json`), updates itself, then starts the game through SMAPI. Offline-safe. Log: `~/stardew-energistics/update.log`. |
| `publish-test.bat` / `.ps1` | Manual fallback: build and upload from the PC with the GitHub CLI. |

Secrets: the Deck token lives only in `~/stardew-energistics/config` on each Deck. Nothing secret is in the repo or the workflow (it uses GitHub's built-in token).
