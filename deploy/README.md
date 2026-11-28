# Deploy scripts

**Installing and updating the mods on the Steam Decks moved to [Stardew Mod Master](https://github.com/afterian/stardewmodmaster)** (PC build watcher, Deck installer and launcher, the list of mods in the pack). Its watcher builds this repo's `main` and publishes it with the other mods. Setup guide: [Mod Master's Steam Deck setup](https://github.com/afterian/stardewmodmaster/blob/main/docs/steam-deck-setup.md).

This repo is now just the Energistics mod, so anyone can install it alone: build `src/StardewEnergistics` (it deploys to the game's `Mods` folder), or take only the `StardewEnergistics` zip from a release.

| File | What it does |
|---|---|
| `selftest.bat` -> `pc/selftest.ps1` | In-game self-test in one go: builds, starts Stardew with only this mod (separate Mods folder), loads the latest save without saving, runs every scenario, quits and prints the report. See [docs/testing/selftest.md](../docs/testing/selftest.md). |
| `showcase.bat` | The playthrough slowly, with a screenshot per step; the game stays open afterwards. |
| `deck/launch.sh` | Hand-off only. Decks set up before the move still start through `~/stardew-energistics/launch.sh`, which updates itself from this file: it sets up `~/stardew-modmaster` and runs the Mod Master launcher. Delete once every Deck has run the Mod Master installer. |
| `pc/build-and-publish.ps1` | Hand-off only. The old "Stardew Energistics build watcher" runs it after pulling a new commit; it turns that scheduled task off. Delete once the old watcher is gone. |
