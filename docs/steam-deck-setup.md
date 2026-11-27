# Steam Deck setup: auto-updating test builds

Once this is set up, testing a change works like this:

```
change pushed to main -> your PC builds the mod (a minute or two) -> GitHub release "test-latest"
                                                                       |
                        Steam Deck: start Stardew  <-  the newest build installs first
```

Setup is one double-click on the PC and one pasted command on each Deck.

**How it works, and why it's safe:** a SMAPI mod can only be built against the game's own files, and those can't go on GitHub's servers. So your PC does the building. A small hidden "watcher" starts when you sign in to Windows. Every minute it asks GitHub "is there a new commit on `main`?". If there is, it builds that commit and uploads the zip to the release. Only your PC reaches out. Nothing on GitHub, and nobody else, can start anything on your PC. It only builds `main`, which only you (and Claude through pull requests you merge) can change. That's the same code you'd build and play yourself anyway. It needs no administrator rights and installs no service.

Why not build on GitHub's own servers? That would need the game's DLLs (or "reference" copies made from them) stored on GitHub. Those are ConcernedApe's copyrighted files, so that's not something to upload, even to a private repo. Most SMAPI mods build locally for the same reason.

The PC must be **on and signed in** for new builds to appear. If it's off, the Decks keep the last build, and the watcher catches up when you sign in.

---

## Private or public repo?

Both work the same way. The scripts detect which one you have, and the watcher is equally safe either way.

| | Private (now) | Public |
|---|---|---|
| PC setup | Same | Same |
| Decks | Need a read-only token (part 2, a few minutes, once a year) | No token: skip part 2 |
| Who sees the code and test builds | Only you | Everyone |

So making it public only saves the token step on the Decks. Make it public when you want people to see the mod, not for the setup.

---

## Part 1. PC (once, about 5 minutes)

1. Get the latest project files: in `E:\Projects\stardewenergistics`, pull `main` (GitHub Desktop: **Fetch origin**, then **Pull**; or `git pull` in a terminal).
2. In File Explorer, open the `deploy` folder and double-click **`setup-pc.bat`**.
3. A window works through 5 steps. It:
   - installs Git, the .NET 8 SDK and the GitHub CLI if they're missing (click **Yes** if Windows asks)
   - opens your browser once to sign in to GitHub: copy the 8-letter code from the window, paste it in the browser, click **Authorize**
   - finds Stardew Valley by itself (if it can't, it asks you to paste the game folder: in Steam right-click Stardew Valley > **Manage** > **Browse local files** and copy the address bar)
   - sets up the watcher
   - builds and publishes the first build
4. When it says **PC setup done**, press a key to close it.

To check it worked, look at https://github.com/afterian/stardewenergistics/releases/tag/test-latest, which should have `StardewEnergistics-test.zip`. Each commit on `main` also gets a green tick ("pc-build") once it's published, or a red cross if the build failed. The build log is attached to the release as `last-build-log.txt`, so Claude can read the errors without your PC.

Running the setup again is safe. Do it if you move the game to another drive.

---

## Part 2. Deck token (private repo only, once)

The Decks need a key that can only read this one repo.

1. Open https://github.com/settings/personal-access-tokens/new
2. Token name: `Steam Decks`. Expiration: **Custom**, about a year from now (the longest GitHub allows).
3. Repository access: **Only select repositories** > `afterian/stardewenergistics`.
4. Repository permissions > **Contents** > **Read-only**. Leave everything else alone.
5. **Generate token**, and copy it. It starts with `github_pat_`.

Getting it onto the Deck without typing it: send it to yourself in **Steam chat** from the PC. On the Deck, in Desktop Mode, open Steam > Friends & Chat, and copy it from there. Delete the message afterwards. Both Decks use the same token.

When it expires, the Decks keep the last build and `update.log` says "token is wrong or expired". Make a new one and run the Deck command again.

---

## Part 3. Each Steam Deck (once, about 5 minutes)

1. **Switch to Desktop Mode:** press the **Steam** button > **Power** > **Switch to Desktop**. (On-screen keyboard: **Steam + X**. A USB or Bluetooth keyboard helps.)
2. **Make sure the game is the Linux version:** in Steam, right-click Stardew Valley > **Properties** > **Compatibility**. "Force the use of a specific Steam Play compatibility tool" must be **off**. Start the game once and quit, so its folders exist.
3. **Open Konsole** (app launcher, bottom-left > System > Konsole). Paste the line for your repo (paste is **Ctrl + Shift + V**) and press Enter.

   Private repo. It asks for the token. Nothing shows while you paste it, that's normal:

   ```
   read -rsp "Paste the token, then Enter: " T; echo; curl -fsSL -H "Authorization: Bearer $T" -H "Accept: application/vnd.github.raw" https://api.github.com/repos/afterian/stardewenergistics/contents/deploy/deck/install.sh | bash -s -- "$T"
   ```

   Public repo:

   ```
   curl -fsSL https://raw.githubusercontent.com/afterian/stardewenergistics/main/deploy/deck/install.sh | bash
   ```

   It checks GitHub, finds the game, installs SMAPI if it's missing, and installs the auto-updater. If the SMAPI installer asks anything, choose to install. At the end press Enter. Tip: open this guide in the Deck's browser and copy the line from there.
4. **Set the launch options:** in Steam, right-click Stardew Valley > **Properties** > **General** > **Launch Options**. Replace what's there with exactly:

   ```
   ~/stardew-energistics/launch.sh %command%
   ```

   (The script can't do this step. Steam overwrites its settings file while it's running.)
5. Double-click **Return to Gaming Mode** on the desktop and start Stardew. The first start takes a few extra seconds. You should see the SMAPI console, then "Stardew Energistics is running!" after loading a save.

**If the automatic SMAPI install fails:** in Desktop Mode open https://smapi.io in a browser and download SMAPI. In the Dolphin file manager, right-click the zip in Downloads > **Extract** > **Extract archive here**. Then in Konsole run `bash ~/Downloads/SMAPI*/"install on Linux.sh"` and choose install. Then run the Deck command from step 3 again.

---

## Part 4. Everyday

1. A change lands on `main`. Your PC builds it by itself.
2. On each Deck: quit Stardew completely (Steam button > **Exit game**) and start it again. The new build installs first.

- **Test a branch before merging:** check out the branch in your project folder and double-click `deploy\publish-test.bat`. It builds exactly what's in the folder and sends it to the Decks. The next new commit on `main` replaces it.
- **Same build for multiplayer:** the SMAPI console shows `Stardew Energistics 0.0.1-build.N`. Every player needs the same N.
- **Playing on the PC:** the watcher doesn't touch the PC's own Mods folder. Build in Visual Studio (that copies the mod into Mods), or unzip `StardewEnergistics-test.zip` from https://github.com/afterian/stardewenergistics/releases/tag/test-latest into the game's `Mods` folder.

---

## If something looks wrong

On a Deck, in Desktop Mode, open Konsole and run `tail -n 20 ~/stardew-energistics/update.log`.

| Log says | What to do |
|---|---|
| `installed 0.0.1-build.N` / `up to date` | All good. Expected a newer build? The PC may be off or signed out, or the build failed (red cross on the commit on GitHub). |
| `token is wrong or expired (401)` | New token (part 2), then run the Deck command again. |
| `release 'test-latest' not found (404)` | Nothing built yet (part 1), or the token can't see the repo. |
| `could not reach GitHub` | No wifi. The game starts with the last build. |
| `SMAPI not found in the game folder` | Run the Deck command again. It reinstalls SMAPI (game updates sometimes remove it). |

- The mod's `config.json` is kept across updates. The launcher also updates itself.
- Pause updates: add the line `AUTO_UPDATE=0` to `~/stardew-energistics/config`.
- Game won't start at all: clear the Launch Options and try. If it starts, send Claude `update.log`.
- Remove everything: clear the Launch Options, delete `~/stardew-energistics` and the game's `Mods/StardewEnergistics`.
- PC watcher log: `%LOCALAPPDATA%\StardewEnergistics\watcher.log` (paste that into the File Explorer address bar).
- Stop the PC watcher: Start menu > **Task Scheduler** > Task Scheduler Library > "Stardew Energistics build watcher" > **Disable**.

## Security notes

- Nothing on GitHub can trigger the PC. The watcher only reaches out, and only builds `main`.
- It uses your normal GitHub sign-in from the GitHub CLI, stored by Windows. No token is written into the repo or any file in it.
- The Deck token can only read this repo. It's stored only on each Deck, in `~/stardew-energistics/config`, readable only by the Deck user.
