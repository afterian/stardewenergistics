# Test deploys to the Steam Decks

Each Deck checks GitHub every time Stardew starts and installs the newest test build. You publish a build from your PC with one double-click.

```
PC: double-click deploy/publish-test.bat  ->  GitHub release "test-latest"  ->  Deck: start the game
```

## One-time setup on your PC

1. Install the [GitHub CLI](https://cli.github.com) and run `gh auth login` once.
2. Make a read-only token for the Decks: GitHub > Settings > Developer settings > Fine-grained tokens > Generate.
   - Repository access: only this repo.
   - Permissions: Contents = Read-only. Nothing else.
   - Skip this if the repo is public.

## One-time setup on each Steam Deck

1. Install SMAPI on the Deck (native Linux version): https://stardewvalleywiki.com/Modding:Installing_SMAPI_on_Steam_Deck
2. Switch to Desktop Mode and open Konsole.
3. Paste this, replacing `OWNER/REPO` and `TOKEN` (tip: open this page in the Deck's browser and copy it):

   ```
   curl -fsSL -H "Authorization: Bearer TOKEN" -H "Accept: application/vnd.github.raw" https://api.github.com/repos/OWNER/REPO/contents/deploy/deck/install.sh | bash -s -- OWNER/REPO TOKEN
   ```

4. In Steam: Stardew Valley > Properties > Launch Options, paste:

   ```
   ~/stardew-energistics/launch.sh %command%
   ```

5. Go back to Game Mode and play. That's it.

## Every test round

1. On the PC: double-click `deploy/publish-test.bat`.
2. On each Deck: start Stardew. The new build installs before the game opens.

Both Decks must start the game after the same publish so they run the same version for multiplayer.

## If something looks wrong

- `~/stardew-energistics/update.log` on the Deck says what the updater did.
- No wifi or GitHub down: the game still starts with the last installed build.
- The mod's `config.json` is kept across updates.
- To stop auto-updating, clear the Launch Options line.
