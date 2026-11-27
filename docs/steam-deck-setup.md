# Steam Deck setup: auto-updating test builds

Once this is set up, testing a change looks like this:

```
Claude pushes to main  ->  your PC builds the mod (about a minute)  ->  release "test-latest" on GitHub
                                                                          |
                         Steam Deck: start Stardew  <-  launcher installs the newest build first
```

You never copy files by hand. Start the game on a Deck and it has the newest build.

Why the PC does the building: a SMAPI mod can only be compiled against the game's own files, and those can't be put on GitHub's servers. So your PC acts as GitHub's build machine (a "self-hosted runner"). It must be switched on and awake for new builds to appear. If it is off, the build waits (up to a day) and runs when the PC comes back.

There are three one-time parts: the PC (part 1), a token for the Decks (part 2), and each Deck (part 3). Part 4 is the everyday loop.

---

## Part 1. Your PC builds the mod for GitHub (once, about 10 minutes)

You need the **.NET 8 SDK** (https://dotnet.microsoft.com/download/dotnet/8.0, "SDK", Windows x64) and Stardew Valley installed on the PC. SMAPI on the PC is only needed to play on the PC.

### 1a. Find your game folder

1. In Steam, right-click **Stardew Valley** > **Manage** > **Browse local files**.
2. Click the address bar of the window that opens and copy the path, for example `E:\SteamLibrary\steamapps\common\Stardew Valley`.

### 1b. Tell GitHub where the game is

1. Open https://github.com/afterian/stardewenergistics/settings/variables/actions
2. Click **New repository variable**.
3. Name: `STARDEW_GAME_PATH`. Value: the path you copied. Click **Add variable**.

### 1c. Install the runner

1. Open https://github.com/afterian/stardewenergistics/settings/actions/runners/new
2. Pick **Windows** and **x64**.
3. Open the Start menu, type `PowerShell`, right-click **Windows PowerShell** > **Run as administrator**.
4. Run the commands from the GitHub page **one block at a time**, top to bottom. The first one is `mkdir actions-runner; cd actions-runner`. Do this in `C:\`, not inside the project folder (type `cd C:\` first).
5. The `./config.cmd ...` step asks questions. Answer:
   - Runner group: press **Enter**
   - Runner name: press **Enter**
   - **Additional labels: type `stardew`** and press Enter (this one matters)
   - Work folder: press **Enter**
   - Run as service? type **Y**
   - Service account: press **Enter**
6. Skip the last `./run.cmd` line on the GitHub page; the service already runs and starts with Windows.
7. Refresh https://github.com/afterian/stardewenergistics/settings/actions/runners. Your PC should show as **Idle** (green).

### 1d. First build

1. Open https://github.com/afterian/stardewenergistics/actions/workflows/deck-build.yml
2. Click **Run workflow** > branch **main** > **Run workflow**.
3. After a minute or two it should turn green, and https://github.com/afterian/stardewenergistics/releases/tag/test-latest has `StardewEnergistics-test.zip`.

If it fails, click the red run, then the failed step, and send Claude a screenshot or the text.

---

## Part 2. A read-only token for the Decks (once)

The repo is private, so the Decks need a key that can only read it.

1. Open https://github.com/settings/personal-access-tokens/new
2. Token name: `Steam Decks`. Expiration: **Custom**, a date about a year from now (GitHub's limit).
3. Repository access: **Only select repositories** > `afterian/stardewenergistics`.
4. Permissions > Repository permissions > **Contents: Read-only**. Leave everything else alone.
5. Click **Generate token** and copy it (starts with `github_pat_`). Keep it somewhere private, like a password manager. Both Decks use the same token. Don't paste it into the repo, chats or issues.

When it expires the Decks keep playing the last build, and `update.log` says "token is wrong or expired". Make a new token and redo step 3c on each Deck.

---

## Part 3. Each Steam Deck (once per Deck, about 10 minutes)

Do all of part 3 in **Desktop Mode**: press the **Steam** button > **Power** > **Switch to Desktop**. To type, press **Steam + X** for the on-screen keyboard. A mouse and keyboard over USB-C or Bluetooth make this much easier.

### 3a. Make sure the game runs as the Linux version

In Steam, right-click **Stardew Valley** > **Properties** > **Compatibility**. "Force the use of a specific Steam Play compatibility tool" must be **unchecked**. (If it was checked, uncheck it and let Steam download the Linux version.) Start the game once and quit, so its folders exist.

### 3b. Install SMAPI

1. Open a web browser (Firefox: install it from the **Discover** app if there isn't one) and go to https://smapi.io. Click the big download button for the newest SMAPI. It lands in **Downloads**.
2. Open the **Dolphin** file manager > **Downloads**, right-click the SMAPI zip > **Extract** > **Extract archive here**.
3. Open **Konsole** (the terminal; find it in the app launcher, bottom left) and paste this, then press Enter (paste in Konsole is **Ctrl + Shift + V**):

   ```
   bash ~/Downloads/SMAPI*/"install on Linux.sh"
   ```

4. When it asks, choose **1** (install). If it asks for the game path, it's usually `/home/deck/.local/share/Steam/steamapps/common/Stardew Valley` (or on the SD card under `/run/media/...`).
5. If the installer tells you to set Steam launch options, you can skip that. Step 3d sets one line that does both jobs.

### 3c. Install the auto-updater

1. Still in Konsole, paste this and press Enter:

   ```
   read -rsp "Paste the token, then press Enter: " T; echo; curl -fsSL -H "Authorization: Bearer $T" -H "Accept: application/vnd.github.raw" https://api.github.com/repos/afterian/stardewenergistics/contents/deploy/deck/install.sh | bash -s -- afterian/stardewenergistics "$T"
   ```

2. Paste the token from part 2 (nothing shows while you paste; that's normal) and press Enter.
3. It should say "OK, the Deck can see afterian/stardewenergistics" and "Found the game and SMAPI". The token is saved only on this Deck, in `~/stardew-energistics/config`, readable only by you.

### 3d. Set the launch options

1. In Steam, right-click **Stardew Valley** > **Properties** > **General** > **Launch Options**.
2. Replace whatever is there with exactly:

   ```
   ~/stardew-energistics/launch.sh %command%
   ```

3. Close the window. Switch back with the **Return to Gaming Mode** icon on the desktop.

### 3e. Check it

Start Stardew in Game Mode. The first start takes a few seconds longer while it downloads the mod. You should see the SMAPI console and, after loading a save, the message "Stardew Energistics is running!".

---

## Part 4. The everyday loop

1. Claude (or you) pushes a change to `main`. Your PC builds it and publishes it. Check https://github.com/afterian/stardewenergistics/actions if you want to watch.
2. On each Deck: fully quit Stardew (Steam button > **Exit game**) and start it again. The new build installs before the game opens.
3. Test.

**Testing a branch before it's merged:** open https://github.com/afterian/stardewenergistics/actions/workflows/deck-build.yml > **Run workflow** > pick the branch > **Run workflow**. The Decks get that branch's build next launch. The next push to `main` replaces it.

**Which build am I on?** The SMAPI console lists `Stardew Energistics 0.0.1-build.N`. N goes up with every build. For multiplayer, every player must show the same N, so restart both Decks after the same build.

**The PC as a player:** the runner doesn't touch the PC's own Mods folder. To play the latest build on the PC, pull the latest `main` and build in Visual Studio (that copies it into the PC's Mods folder), or unzip `StardewEnergistics-test.zip` from the release into the PC's `Mods` folder.

**No PC available?** `deploy/publish-test.bat` still builds and uploads by hand (needs the GitHub CLI signed in).

---

## If something looks wrong

On the Deck, the launcher writes what it did to `~/stardew-energistics/update.log`. To read it, in Desktop Mode open Konsole and run:

```
tail -n 20 ~/stardew-energistics/update.log
```

| Log says | What to do |
|---|---|
| `up to date (0.0.1-build.N)` | Nothing; you have the newest build. If you expected a newer one, check the Actions page: maybe the PC was off or the build failed. |
| `installed 0.0.1-build.N` | Worked. |
| `token is wrong or expired (401)` | Make a new token (part 2), redo step 3c. |
| `release 'test-latest' not found (404)` | Nothing has been published yet (do step 1d), or the token can't see the repo. |
| `could not reach GitHub` | No wifi. The game starts with the last build. |
| `SMAPI not found in the game folder` | Redo step 3b. Game updates sometimes need SMAPI reinstalled. |
| `game folder not found` | Start the game once without launch options, then try again. |

- The mod's `config.json` is kept across updates.
- The launcher also updates itself from `main`; changes take effect the launch after.
- To pause updates (keep the current build), add a line `AUTO_UPDATE=0` to `~/stardew-energistics/config`. To stop using the launcher, clear the Launch Options line.
- To remove everything: clear the Launch Options, delete `~/stardew-energistics` and the game's `Mods/StardewEnergistics` folder.
- Game won't start at all: clear the Launch Options and start it. If it starts, the problem is in the launcher; send Claude `update.log`.

## Security notes

- The runner only runs this repo's own workflow, and only while the repo is **private**. Don't make the repo public while the runner is attached (anyone could then run code on your PC through a pull request). Remove it first: Settings > Actions > Runners > your PC > Remove.
- The Deck token can only read this one repo. It lives only in `~/stardew-energistics/config` on each Deck, never in the repo (the `config` file name is also in `.gitignore`).
