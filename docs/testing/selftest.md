# In-game self-test

Stardew can't run headless, so the mod carries its own integration test. It drives the real game and
mod code: the game's placement (`Utility.tryToPlaceItem`), tools (`Tool.DoFunction`), the game's save
serializer, and the mod's own input handlers and Terminal screen, fed synthetic button presses and menu
clicks. The Core library's xUnit tests (`tests/`) cover the game-independent logic; this covers the game side.

## Run it

**One command (PC):** double-click `deploy\selftest.bat`. It:

1. builds the mod from the project folder,
2. puts it in a separate Mods folder (`%LOCALAPPDATA%\StardewEnergistics\selftest\Mods`); your normal Mods folder isn't touched,
3. starts Stardew through SMAPI with `SE_SELFTEST=1`,
4. the mod loads your most recently played save, runs every scenario, writes the report and quits **without saving**,
5. the window shows the report. Exit code 0 = all passed, 1 = something failed, 2 = couldn't run.

Leave the game window alone while it runs (about a minute). The game must be closed first.

Options (`powershell -File deploy\pc\selftest.ps1 ...`): `-Only disks,terminal`, `-Save <save folder name>`,
`-ZipDir <folder with a built zip>`, `-TimeoutMinutes 6`, `-Watcher on|off`.

**While playing:** with test mode on, type `se_selftest` in the SMAPI console (`se_selftest list` for the
scenarios, `se_selftest disks terminal` for some). It builds in a temporary location, then puts your
backpack and position back. Scenarios that would touch your other networks (the morning power pass) only
run in a session launched by the script.

**Every build:** `deploy\selftest.bat -Watcher on` makes the PC build watcher self-test each new `main`
build after publishing it: a `pc-selftest` tick or cross on the commit, and the report as
`last-selftest.txt` on the `test-latest` release. It skips a build if the game is open. Restart the watcher
(sign out and in) after merging changes to `watch.ps1`. Re-running `setup-pc.bat` turns it off again.

## Showcase (watch it build)

Double-click `deploy\showcase.bat` (or `se_selftest showcase` in the console). It builds a whole working base
in the scratch building, slowly enough to follow: wire, Core, racks, power, disks, a linked chest and Furnace,
Network View, storing and taking items at the Terminal, the Autocraft screen, an autocraft job and the Furnace
running from storage. Each step is checked and photographed; the screenshots (with `index.json` captions) go to
`%LOCALAPPDATA%\StardewEnergistics\selftest\shots`, which opens at the end. With `-Publish` they are also
uploaded to the `test-latest` release as `selftest-shots.zip`. It starts with the quest: Maru's journal letter,
the journal, and her first two blueprint letters as hearts and progress grow; it ends with the journal's goals
ticked and her automation and autocrafting letters. When it's done **the game stays open** on the built base so
you can walk around and try it (you get your backpack back plus a Network Wrench). Nothing is ever saved: if the
game tries to save (for example you sleep), it closes instead. `-Close` closes it at the end.

## When something fails

- Every step and synthetic press is logged (`[self-test]` lines in the SMAPI log, at Debug/Trace), and a press made
  while the player isn't free is a warning with the full state.
- A failed check records the step it happened in and the state (location, tile, menu, dialogue, held item).
- A scenario that stops writes `selftest-crash.txt` beside the report: scenario, last step, state, the exception with
  its stack trace, and the last 60 self-test log lines. The PC script prints it.

## Report

`%LOCALAPPDATA%\StardewEnergistics\selftest\report.txt` (and `.json`), plus `SMAPI-latest.txt` from the run.
In console runs: `selftest-report.txt` in the mod folder. Each scenario is PASS, FAIL, CRASH or SKIP with its
failed checks; the last line is `SE-SELFTEST RESULT: PASS|FAIL (...)`.

## Scenarios

| ID | What it checks |
|---|---|
| placement | Every block places from the hand and breaks back into exactly one item with a pickaxe |
| wire | Mouse lays on the cursor tile (within 5), else the facing tile; controller with hidden cursor uses the facing tile; top half of a big craftable means its base; pickaxe picks wire up |
| network | Core, racks and Terminal on one powered network; spare wire end removal; two Cores stop it |
| disks | Insert with mouse and controller; empty hand removes the last disk with its ID and contents; rack holds 4 |
| terminal | Store a stack / one, withdraw a stack / one through the Terminal screen; item leaves the backpack exactly once; withdrawing frees kinds and items; no disks gives the item back with the reason |
| capacity | Kind limit refuses a new kind with "kinds full", withdrawing a whole kind frees it; item limit refuses with "full", withdrawing frees space |
| targeting | Controller picks a diagonal rack; mouse on an empty tile does nothing; top-half click; Battery Pack with controller X |
| break-rack | Breaking a Storage Rack drops its disks (same IDs, contents kept) and an empty rack; Power Rack drops Battery Packs; generator drops its Coal |
| chest-link | Link on, Terminal stores into and takes from the chest, wrench removes and drops the Link |
| power | No batteries = unpowered; a battery powers it; adding fuel lifts a morning outage; unconnected Power Rack warns |
| power-day | Morning power pass takes one day's use (launched sessions only) |
| machine-link | Unconfigured Link does nothing; configured one loads a Furnace from storage, collects the bar, reloads |
| autocraft | Crafting Rack makes 20 Copper Wire from storage; too big a job is refused |
| tools | A Pickaxe stores on a disk and comes back as a working tool |
| quest | Maru's journal letter on day one (journal attached, recipe known), using the journal opens it, her T1/T2 letters wait for 2 hearts / 10 processors + Mines 20 and teach their recipes when read at the mailbox, journal goals tick (launched sessions only) |
| save | Mod save data (SMAPI save-data API, fresh stores) and the game's own location XML keep wire, disks and block data |

Not covered yet: Network Ports and linked buildings, Wireless Terminal, Shipping Links, Power Feed,
Advanced Crafting Rack, multiplayer, drawing and menus' looks. Those stay on the manual checklists (`m1.md` … `m6.md`).

## Adding a scenario

Add a method to `src/StardewEnergistics/Testing/SelfTest/Scenarios.cs` and a line to `Scenarios.All`. A scenario
is an iterator: `yield return null` waits a tick, `yield return Wait.Ticks(n)` waits n ticks. Use the context's
helpers (`Place`, `LayWire`, `Action`, `UseTool`, `Break`, `OpenTerminal`, `ClickInventorySlot`, `ClickStoredItem`)
so actions go through the same code a player's do, and `Check`/`CheckEqual`/`Require` for results.
Synthetic presses reach the mod's handlers (`WireInput`, `BlockInteraction`) in registration order; the
game's own handling of a press isn't simulated, so check that our handler took (suppressed) the press.
