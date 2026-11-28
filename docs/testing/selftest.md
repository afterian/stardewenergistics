# In-game play-tester (self-test)

Stardew can't run headless, so the mod carries its own play-tester: scripted scenarios that play the mod
like a player, the way unit tests exercise code. It drives the real game and
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

## Playthrough and showcase (start to finish)

`playthrough` plays the whole mod from a new farmer to the journal's last page (launched sessions only):
Maru's journal letter, every tier letter as hearts, mine depth and progress grow, every recipe crafted at the
game's crafting menu, processors pressed at the Processor Press, the base built tile by tile, Machine, Shipping
and Chest Links, Heavy Cable, Power Feed, Wireless Terminal, Network Ports linking the farmhouse and two new
Sheds across the real farm, keep-in-stock, the Prismatic Disk and an Island Uplink on Ginger Island.

- Before each tier letter it checks the letter **waits** while its last requirement is missing, then that it
  arrives once met. A letter that never comes stops the run with `PROGRESSION: <tier> letter arrives` and the
  requirement that's short (`progression blocked: mines 64/80 (short), ...`).
- After each chapter every journal goal must be ticked, and Maru's thank-you letter must bring the reward.
- The finale checks every goal, letter, recipe and built tier.
- Time, hearts and depth are set; mornings run the mod's DayStarted work (including the day's power use) and
  evenings its DayEnding work, without a real new day (which would save). Ten-minute steps use the game clock.

`showcase` is the same playthrough slowly, with a screenshot (world, HUD and menus) after each step. Double-click
`deploy\showcase.bat` (or `se_selftest showcase`). Screenshots and `index.json` captions go to
`%LOCALAPPDATA%\StardewEnergistics\selftest\shots`, which opens at the end; with `-Publish` they're uploaded to
the `test-latest` release as `selftest-shots.zip`. When it's done **the game stays open** on the built base so
you can walk around (you get your backpack back plus a Network Wrench). Nothing is ever saved: if the game tries
to save (for example you sleep), it closes instead. `-Close` closes it at the end.

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
| coverage | Every item has data, a recipe with real ingredients and a play test; every scenario is on the coverage list |
| placement | Every block places from the hand and breaks back into exactly one item with a pickaxe |
| wire | Mouse lays on the cursor tile (within 5), else the facing tile; controller with hidden cursor uses the facing tile; top half of a big craftable means its base; pickaxe picks wire up |
| network | Core, racks and Terminal on one powered network; spare wire end removal; two Cores stop it |
| disks | Insert with mouse and controller; empty hand removes the last disk with its ID and contents; rack holds 4 |
| terminal | Store a stack / one, withdraw a stack / one through the Terminal screen; item leaves the backpack exactly once; no disks gives the item back with the reason |
| capacity | Kind and item limits refuse with the right reason; withdrawing frees space |
| targeting | Controller picks a diagonal rack; mouse on an empty tile does nothing; top-half click; Battery Pack with controller X |
| break-rack | Breaking racks and generators drops disks, batteries and coal |
| chest-link | Link on, Terminal stores into and takes from the chest, wrench removes and drops the Link |
| power | No batteries = unpowered; a battery powers it; fuel lifts a morning outage; unconnected Power Rack warns |
| power-day | Morning power pass takes one day's use (launched only) |
| machine-link | Unconfigured Link does nothing; configured one loads a Furnace from storage, collects the bar, reloads |
| autocraft | Crafting Rack makes 20 Copper Wire from storage; too big a job is refused |
| tools | A Pickaxe stores on a disk and comes back as a working tool |
| press | Processor Press makes Copper, Gold and Iridium Processors; a bar without its other inputs is refused with the reason |
| heavy-cable | Heavy Cable lays, the wrench changes its colour and hides it, a pickaxe picks it up as Heavy Cable |
| disk-upgrade | Crafting an Iron Disk from a Copper Disk keeps its ID and contents (launched only) |
| shipping-link | A Shipping Link ships from storage into the farm's Shipping Bin in the evening (launched only) |
| power-feed | A Power Feed moves a struck Lightning Rod's Battery Pack into the Power Rack; refused elsewhere; wrench removes it |
| wireless | The Wireless Terminal opens the network; with none it says so |
| keep-stock | A keep-in-stock rule starts a job overnight and tops the item up (launched only) |
| save | Mod save data and the game's own location XML keep wire, disks and block data |
| quest | Short quest check: journal letter, T1/T2 letters and their recipes, first goals (launched only) |
| playthrough | Everything, start to finish (above; launched only) |
| showcase | The playthrough slowly, with screenshots (only when named) |

Not covered yet: multiplayer (farmhands, split screen), drawing and menus' looks beyond the screenshots.
Those stay on the manual checklists (`m1.md` … `m6.md`).

## Coverage: every feature ships with a play test

`src/StardewEnergistics.Core/Testing/PlayTestCoverage.cs` maps every item (`item:<Key>` from `Ids.cs`), Maru
letter, journal goal and player-facing feature to the scenarios that play it. `dotnet test` (CI) fails when an
item, goal or letter has no entry, when an entry names a scenario that doesn't exist, or when `Scenarios.All` and
the list differ. The in-game `coverage` scenario checks the same against the live game data.

**When you add a feature:** add or extend a scenario that plays it the way a player would, then add the
feature (and any new item) to `PlayTestCoverage` with that scenario's id. If it's part of progression, extend the
playthrough's chapter too, so the start-to-finish run still reaches the end.

## Adding a scenario

Add a method (in `Scenarios.cs`, `FeatureScenarios.cs` or its own file under `Testing/SelfTest/`), a line to
`Scenarios.All`, and the id to `PlayTestCoverage.ScenarioIds`. A scenario is an iterator: `yield return null`
waits a tick, `yield return Wait.Ticks(n)` waits n ticks, `yield return c.Show("caption")` takes a showcase
screenshot. Helpers go through the same code a player's actions do:

- Building: `Place` (uses a crafted one from the backpack if there is one), `LayWire`, `Action`, `UseTool`,
  `Break`, `InsertDisk`, `BuildBasicNetwork`, `GameAction` (the game's own action on a tile: machines, chests).
- Menus: `Craft` (the game's crafting menu), `OpenTerminal`, `ClickInventorySlot`, `ClickStoredItem`, `CloseMenu`.
- Machines: `PressProcessors` (hold the bar, use it on the press, collect).
- Places: `Visit(location, origin)` (warps; then every helper works there), `GoHome`, `FindFreeRow`, `FindPath`,
  `FreeTileBeside`, `BuildOnFarm`.
- Time: `TimePasses(n)` (the game clock, so handlers run in their real order), `Quest.Morning`, `Quest.Evening`.
- Quest: `Quest.ReadMail` (the mailbox), `Quest.CheckRecipes`, `Quest.Done`, `Quest.SetMaruHearts`.
- Backpack: `Give`, `Remove`, `TakeFromBackpack`, `HoldFromBackpack`, `Carried`, `BigBackpack`.
- Results: `Check`, `CheckEqual`, `Require` (stops the scenario). Use `Step("...")` before anything that might
  fail so the report says where.

Mark a scenario `ThrowawayOnly` if it changes anything outside the scratch building (the farm, mail, recipes,
other networks). Synthetic presses reach the mod's handlers (`GuideService`, `WireInput`, `BlockInteraction`)
in registration order; the game's own handling of a press isn't simulated, so check that our handler took the
press, or use `GameAction` for the game's side.
