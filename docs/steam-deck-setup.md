# Steam Deck setup: auto-updating test builds

Once this is set up, testing a change works like this:

```
change pushed to main -> your PC builds the mod (a minute or two) -> GitHub release "test-latest"
                                                                       |
                        Steam Deck: start Stardew  <-  the newest build installs first
```

Setup is one double-click on the PC and one double-click on each Deck. No typing on the Deck.

**More than one mod (temporary pack loader):** the same watcher and launcher also handle the other mods listed in [`deploy/mods.json`](../deploy/mods.json) (now Stardew Food Time too). The PC builds each mod's new commits, every mod goes on the same release, and each Deck installs all of them on every start. One mod failing to build or download never blocks the others. To add a mod, add a line to `deploy/mods.json`. Nothing to set up again on the PC or the Decks: both update themselves.

**How it works, and why it's safe:** a SMAPI mod can only be built against the game's own files, and those can't go on GitHub's servers. So your PC does the building. A small hidden "watcher" starts when you sign in to Windows. Every minute it asks GitHub "is there a new commit on `main`?". If there is, it builds that commit and uploads the zip to the release. Only your PC reaches out. Nothing on GitHub, and nobody else, can start anything on your PC. It only builds `main`, which only you (and Claude through pull requests you merge) can change. That's the same code you'd build and play yourself anyway. It needs no administrator rights and installs no service.

Why not build on GitHub's own servers? That would need the game's DLLs (or "reference" copies made from them) stored on GitHub. Those are ConcernedApe's copyrighted files, so that's not something to upload, even to a private repo. Most SMAPI mods build locally for the same reason.

The PC must be **on and signed in** for new builds to appear. If it's off, the Decks keep the last build, and the watcher catches up when you sign in.

---

## Private or public repo?

This guide assumes the repo is **public**, so the Decks need no token and the installer is a single double-click. Public is safe with this setup, because nothing on GitHub can start anything on your PC.

To make it public: https://github.com/afterian/stardewenergistics/settings > scroll to **Danger Zone** > **Change visibility** > **Public**, then confirm.

If you keep it private, the Decks need a read-only token (part 2), and the Deck install uses the "private repo" steps at the end of part 3.

---

## Part 1. PC (once, about 5 minutes)

1. Get the latest project files: in your local Stardew Energistics checkout, pull `main` (GitHub Desktop: **Fetch origin**, then **Pull**; or `git pull` in a terminal).
2. In File Explorer, open the `deploy` folder and double-click **`setup-pc.bat`**.
3. A window works through 5 steps. It:
   - installs Git, the .NET 8 SDK and the GitHub CLI if they're missing (click **Yes** if Windows asks)
   - opens your browser once to sign in to GitHub: copy the 8-letter code from the window, paste it in the browser, click **Authorize**
   - finds Stardew Valley by itself (if it can't, it asks you to paste the game folder: in Steam right-click Stardew Valley > **Manage** > **Browse local files** and copy the address bar)
   - sets up the watcher
   - builds and publishes the first build
4. When it says **PC setup done**, press a key to close it.

To check it worked, look at https://github.com/afterian/stardewenergistics/releases/tag/test-latest, which should have `StardewEnergistics-test.zip`. Each commit on `main` also gets a green tick ("pc-build") once it's published, or a red cross if the build failed. Raw build logs stay on the build PC.

Running the setup again is safe. Do it if you move the game to another drive.

---

## Part 2. Deck token (private repo only; skip if public)

The Decks need a key that can only read this one repo.

1. Open https://github.com/settings/personal-access-tokens/new
2. Token name: `Steam Decks`. Expiration: **Custom**, about a year from now (the longest GitHub allows).
3. Repository access: **Only select repositories** > `afterian/stardewenergistics`.
4. Repository permissions > **Contents** > **Read-only**. Leave everything else alone.
5. **Generate token**, and copy it. It starts with `github_pat_`.

Getting it onto the Deck without typing it: send it to yourself in **Steam chat** from the PC. On the Deck, in Desktop Mode, open Steam > Friends & Chat, and copy it from there. Delete the message afterwards. Both Decks use the same token.

When it expires, the Decks keep the last build and `update.log` says "token is wrong or expired". Make a new one and run the Deck command again.

---

## Part 3. Each Steam Deck (once, about 3 minutes, no typing)

You only need the touchscreen or trackpads: move the mouse with the right trackpad; R2 is left-click and L2 is right-click.

1. **Install Stardew and start it once** in Game Mode, then quit. (It must be the normal Linux version: in Properties > Compatibility, "Force the use of a specific Steam Play compatibility tool" must be **off**, which is the default.)
2. **Switch to Desktop Mode:** **Steam** button > **Power** > **Switch to Desktop**.
3. **Download the installer:** open a web browser on the Deck (Firefox is in the **Discover** app if there's no browser yet). Open **[the installer file](https://github.com/afterian/stardewenergistics/blob/main/deploy/deck/Install-Stardew-Energistics.desktop)** (tap that link if you're reading this on the Deck; or type the address github.com/afterian/stardewenergistics/blob/main/deploy/deck/Install-Stardew-Energistics.desktop with **Steam + X**). Tap the **Download raw file** button: the down-arrow icon at the top right of the file box, next to "Raw" and the copy icon. The file `Install-Stardew-Energistics.desktop` lands in **Downloads**.
4. **Run it:** open the **Dolphin** file manager (folder icon in the taskbar) > **Downloads**, and double-click **Install-Stardew-Energistics.desktop**.
   - If a box asks whether to run or trust it, choose **Execute**, **Continue** or **Run**.
   - If it opens as text instead, close that, right-click the file > **Properties** > **Permissions** > tick **Is executable** (or **Allow executing file as program**) > **OK**, then double-click it again.
5. A terminal window shows the progress. It installs SMAPI, installs the auto-updater, and sets Stardew's Launch Options for you. **Steam closes and reopens by itself** during this; that's expected.
6. When the "All set!" box appears, click **OK**, double-click **Return to Gaming Mode** on the desktop, and start Stardew. The first start takes a few extra seconds. You'll see the SMAPI console, then "Stardew Energistics is running!" after loading a save.

If SMAPI is already installed, the installer keeps it and skips that step. You don't need to uninstall anything. Run the installer again any time to refresh everything. Everything it shows is also saved in `~/stardew-energistics/install.log`.

**If the box says "Almost done":** Steam didn't close, so set the Launch Options by hand. In Steam, right-click Stardew Valley > **Properties** > **General** > **Launch Options**, and enter the line the box shows.

**If the automatic SMAPI install fails:** download SMAPI from https://smapi.io in the Deck's browser. In Dolphin, right-click the zip in Downloads > **Extract** > **Extract archive here**, open the new folder and double-click **install on Linux.sh** (choose **Execute**; if it opens as text, mark it executable as in step 4). Choose install. Then run the Stardew Energistics installer again.

**Private repo instead:** the double-click installer can't download from a private repo. In Desktop Mode open **Konsole** (app launcher > System > Konsole), copy this line from the guide in the Deck's browser, right-click in Konsole > **Paste**, and press Enter (Steam + X > Enter key). When it asks, paste the token from part 2 the same way.

```
read -rsp "Paste the token, then Enter: " T; echo; curl -fsSL -H "Authorization: Bearer $T" -H "Accept: application/vnd.github.raw" https://api.github.com/repos/afterian/stardewenergistics/contents/deploy/deck/install.sh | bash -s -- "$T"
```

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
| `token is wrong or expired (401)` | New token (part 2), then run the private-repo Deck command again. |
| `release 'test-latest' not found (404)` | Nothing built yet (part 1), or the token can't see the repo. |
| `could not reach GitHub` | No wifi. The game starts with the last build. |
| `SMAPI not found in the game folder` | Run the Deck installer again. It reinstalls SMAPI (game updates sometimes remove it). |

- The mod's `config.json` is kept across updates. The launcher also updates itself.
- Pause updates: add the line `AUTO_UPDATE=0` to `~/stardew-energistics/config`.
- Game won't start at all: clear the Launch Options and try. If it starts, send Claude `update.log`.
- The installer keeps a backup of Steam's settings file as `localconfig.vdf.stardew-energistics.bak` next to it.
- Remove everything: clear the Launch Options, delete `~/stardew-energistics` and the game's `Mods/StardewEnergistics`.
- PC watcher log: `%LOCALAPPDATA%\StardewEnergistics\watcher.log` (paste that into the File Explorer address bar).
- Stop the PC watcher: Start menu > **Task Scheduler** > Task Scheduler Library > "Stardew Energistics build watcher" > **Disable**.

## Security notes

- Nothing on GitHub can trigger the PC. The watcher only reaches out, and only builds `main`.
- It uses your normal GitHub sign-in from the GitHub CLI, stored by Windows. No token is written into the repo or any file in it.
- The Deck token can only read this repo. It's stored only on each Deck, in `~/stardew-energistics/config`, readable only by the Deck user.
