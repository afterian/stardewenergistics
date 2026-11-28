# Quest guide journal: in-game test

The Tinkerer's Journal is Maru's quest guide: 7 chapters (a prologue plus one per tier), 40 goals, a tick for each goal done (per player), and a thank-you letter with a small vanilla gift for each finished chapter. Goal text, chapters and Maru's letters are in `i18n/default.json` (`guide.*`, `mail.guide*`); the goal list is `src/StardewEnergistics.Core/Guide/GuideBook.cs`.

Commands go in the SMAPI console: `se_guide` opens it, `se_guide give` gives you the journal, `se_guide reset` clears your ticks (they come back at the next check).

## Checks

| # | Do this | Expect |
| --- | --- | --- |
| 1 | Start a new save and sleep one night (or load an old save) | Maru's letter "A journal for you" with the Tinkerer's Journal attached; the journal recipe is in the crafting menu (1 Wood + 1 Fiber) |
| 2 | Hold the journal, press A (or right-click). Also press F8 | The journal opens on the next goal to do; "Open the Journal" ticks off |
| 3 | Controller: move up/down the chapters and goals with the stick or d-pad | The highlighted goal shows on the right at once; LB/RB change chapter; LT/RT scroll the page; B closes |
| 4 | Mouse: click chapters and goals, scroll the wheel over the page | Same, with arrows when there's more to read |
| 5 | Open "Read Maru's First Blueprints" with Maru under 2 hearts | It lists "Friendship with Maru: 0/2 hearts" with an empty box |
| 6 | Open a chapter 3 letter goal on a save with Mines floor 64 | "Deepest Mines floor: 64/80" and the other conditions, ticked or not |
| 7 | Look at any goal with parts | Each part shows its icon, name and recipe from the game, "makes 10" for wire and cable, and whether you know the recipe; processors show the Processor Press inputs |
| 8 | Test menu > Starter kit, then craft or hold items (a Processor Press, Copper Wire) | A HUD message "Journal: Squeeze the Day" with the quest sound, within a moment |
| 9 | Build a powered network: Core, Storage Rack with a disk, Power Rack with a Battery Pack, Terminal on wire | Within 10 game minutes: "A Core Memory", "Disk-overy" and "Power Nap" tick |
| 10 | Hide a wire tile with the wrench; link a chest; store 10 kinds through the Terminal | "Out of Sight", "Chest Friends" and "Everything in Its Place" tick |
| 11 | Finish every goal in chapter 1, then sleep | "Chapter done!" message; next morning Maru's "Nice chips!" letter with 5 Refined Quartz |
| 12 | Set up a Machine Link; put a Machine Link on a Furnace; start an autocraft job; close the Terminal | The matching goals tick right after the menu closes |
| 13 | Load a save that already has a lot built (`se_guide reset` on a busy save) | One message: "Tinkerer's Journal: N goals already done!" instead of a flood |
| 14 | Change the Coal power in GMCM, open "Keep the Home Fires Burning" | The text shows the new Coal value |
| 15 | Split-screen co-op: player 2 opens their journal | Their own ticks; network goals (shared farm) show for both; each gets their own chapter letters |
| 16 | Online co-op: a farmhand opens the journal | Works the same; network goals come from the host |

**Done when** a new save can follow the journal from Maru's first letter to the Island Uplink without needing any other guide.
