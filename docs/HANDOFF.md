# Stardew Energistics — Handoff for implementation

Date: 2026-11-28. Status: **M0–M6 merged, all art final, final code review done (`docs/testing/review-fixes.md`); nothing playtested in game yet.** Core library with tests and CI; game side builds on the PC (Stardew 1.6.15, SMAPI 4.5.2, .NET SDK 8.0.428). In-game checks: `docs/testing/m1.md` … `m6.md`. M3 added Machine/Shipping Links, Iron/Gold Disks and the Link settings screen. M4 adds the Crafting Rack, the craft planner (stock first, sub-recipes, Furnace smelting, leftovers reused, cycles cut), jobs saved on the Computer Core, and the Terminal's Autocraft screen (pick, amount, plan, start, jobs with cancel). Known gaps: whole/flavoured items stay in linked chests (S4); remote farmhands can't craft or autocraft; autocraft doesn't reserve ingredients (a job waits if they're taken); category ingredients ("any egg") aren't planned; Link overlays are drawn on chests and machines (`World/OverlayRenderer.cs`). M5 adds Network Ports (placed outside, right beside a building, on wire; every building whose Port shares an outdoor network joins one group with shared storage; power stays per building and outdoor wire uses the farmhouse's), the Wireless Terminal, the Power Feed (Lightning Rod / Solar Panel batteries go into Power Racks), and the Iridium Disk. Not done yet: Terminal building filter, farmhand messages for combined views. M6 adds the Advanced Crafting Rack (cooking, keep-in-stock rules run overnight, Furnaces in every linked building), Prismatic Disk, Island Uplink (every network with an Uplink joins the others), the T6 letter, Generic Mod Config Menu options, a version-mismatch warning in multiplayer, manifest 0.6.0 (0.6.1 after the final review), and a Nexus page draft (`docs/nexus-page.md`). Before release: in-game checks M1–M6 on PC and both Decks,  Nexus mod ID for UpdateKeys.

## What this project is

A Stardew Valley SMAPI (C#) mod inspired by Minecraft's Applied Energistics 2. The player builds a computer in each building, lays copper wire to chests and machines, stores everything on tiered disks, configures machines to run from storage, autocrafts, and finally links every building to the farmhouse. Aimed at relaxed players, not network engineers: **no channels, no auto mode, vanilla resources only.**

## Read these, in order

1. `docs/HANDOFF.md` — this file: status, decisions, next steps.
2. `docs/design-plan.md` — the full approved design (mechanics, tiers, 26-item catalog, architecture, roadmap). **Source of truth.**
3. `docs/implementation-plan.md` — code-level plan: solution layout, Core contracts, parallel lanes A–G, spikes, milestone tasks, how code depends on art.
4. `docs/art-asset-list.md` — 36 art files with sizes, frames, descriptions. Art is made in parallel with another tool. Final art so far (Maru purple-and-oak style, `docs/artwork-pipeline.md`): Computer Core, Network Terminal, Copper Wire tiles (normal + Network View) and Heavy Cable tiles; the other 31 still use placeholders. The Terminal shows its "on" frame while its network is powered.
5. `docs/steam-deck-setup.md` — step-by-step PC runner + Steam Deck setup; `deploy/README.md` lists the scripts.
6. `research/MODDING_AND_ASSET_SPECS.md` — earlier research on sprite sizes and data assets. Note: it suggests a Content Patcher prototype; the approved plan is a single C# SMAPI mod that injects its own content.

Living (online, editable) copies of the plan and art list: https://claude.ai/artifact/XZ6h7hFyLrDj7a15X1MQ1d and https://claude.ai/artifact/48iRmHg7rUvvhcSvZQv4DP. If they differ from the files here, ask the user which wins.

## Key decisions (do not reopen without the user)

- **Name** Stardew Energistics. Credit AE2 as inspiration ("Not affiliated"). No AE2 art, code, item names like "ME Drive", or copied text.
- **Wires without channels.** Copper Wire (small) and Heavy Cable (big, paintable in 20 chest colours). Anything touching wire is connected. Wire is on its own floor layer, can be covered by flooring, and the Network Wrench hides/shows it.
- **Big computer.** Computer Core + racks placed beside it (Storage, Power, Crafting, Advanced Crafting).
- **Each building is a subnetwork** with its own Core. Network Port on the outside wall + Heavy Cable joins a building to the farmhouse. Outdoor wire runs into the farmhouse port (no outdoor Core). Ginger Island via Island Uplink.
- **Terminal = crafting table** using network storage, plus a Storage tab; reaches all linked subnetworks, indoors or out.
- **Vanilla resources only.** New crafted parts: Copper/Gold/Iridium Processors (Processor Press) and wire.
- **Power:** Battery Packs last ~2 weeks for a starter network; Coal Generator as low-tech backup; Power Feed for Lightning Rods/Solar later.
- **No auto mode.** Every Machine Link does nothing until configured (inputs, keep-back, output, schedule; copy/paste settings).
- **No Harvester or Planter.**
- **Gated pace:** components → simple storage + basic crafting → bigger storage + automation → autocrafting → linked buildings → advanced crafting. Maru mails blueprints by hearts.
- **Multiplayer:** host-authoritative, one shared farm network.
- **Testing:** two Steam Decks auto-updating from a private GitHub release before any Nexus/public release.

## Open items

- [x] **GitHub repo** `afterian/stardewenergistics`; filled into `deploy/README.md` and `manifest.json`.
- [ ] **Terminal reach before T5:** plan says a Terminal sees only its own building until Network Ports exist. User may prefer all buildings from the start. Ask before M5.
- [ ] **Coal value:** the user edited "1 Coal = 10 power" to "1 Coal = 1 power". Confirm intent in the first power playtest.
- [ ] Installed versions: PC has Stardew 1.6.15, SMAPI 4.5.2, .NET SDK 8.0.428. Decks still to record.

## Environment

- **Project folder:** `E:\Projects\stardewenergistics\` on the user's Windows PC (also the git repo root).
- **User:** has never made a Stardew mod. Wants Claude to write code, test, and deploy. Explain steps simply; give exact clicks/commands.
- **Test devices:** Windows PC (build + local test, split-screen co-op for MP) and two Steam Decks (native Linux SMAPI) for real MP testing.
- **Deploy pipeline (written, untested end to end):** `deploy/setup-pc.bat` sets up a hidden PC watcher (scheduled task at sign-in, no admin) that polls GitHub every minute, builds each new `main` commit (the mod can't build on GitHub's servers without the game DLLs), and uploads it to release `test-latest` with version `x.y.z-build.N`, a `pc-build` commit status and `last-build-log.txt` (read that for build errors). Decks run `~/stardew-energistics/launch.sh %command%`, which installs the newest zip on every launch (offline-safe, keeps `config.json`), self-updates, and starts through SMAPI; `deck/install.sh` also installs SMAPI. `deploy/publish-test.bat` publishes any branch by hand. Guide: `docs/steam-deck-setup.md`.

## Proposed repo layout (create in M0)

```
StardewEnergistics.sln
src/StardewEnergistics/            SMAPI mod (net6.0, Pathoschild.Stardew.ModBuildConfig, EnableModZip)
  ModEntry.cs, manifest.json, assets/ (placeholder sprites until artwork/exports/ lands)
src/StardewEnergistics.Core/       game-independent logic: wire graph, storage index, power, recipe planner
tests/StardewEnergistics.Tests/    xUnit tests for Core (run without the game)
deploy/                            Steam Deck + publish scripts (exists)
docs/, research/, artwork/         (exist)
```

Use namespaced string IDs, e.g. `StardewEnergistics.ComputerCore`. Keep Core free of Stardew references so tests run in CI or a plain shell.

## Next steps (M0 → M1)

The detailed task breakdown per milestone and lane is in `docs/implementation-plan.md` §7.

1. Confirm the GitHub repo exists and is cloned at the project folder; get `owner/repo`.
2. Walk the user through PC setup: .NET 6 SDK, Visual Studio 2022 Community or Rider, SMAPI, Git, GitHub CLI (`gh auth login`). Record versions in `docs/decisions.md`.
3. Scaffold the solution above; `ModEntry` logs "Stardew Energistics loaded"; add `manifest.json` with `UpdateKeys: []`.
4. Build on the PC; confirm it loads in SMAPI.
5. Set up the PC runner and both Decks per `docs/steam-deck-setup.md`; confirm both Decks load the mod. M0 done.
6. Start M1: Processor Press + processors, Copper Wire and Heavy Cable layer with 4-bit auto-tiling, Wrench cover/paint, save/load. Use placeholder art.

## Working agreements

- Follow `docs/design-plan.md`; propose changes to the user rather than silently diverging. Record every confirmed decision in `docs/decisions.md`.
- Each milestone ends with a playable build, a `publish-test` to the Decks, and the milestone's "Done when" check.
- Bugs: ask the user for the SMAPI log link from https://smapi.io/log.
- Don't copy AE2 code or textures (AE2 code LGPL-3.0, API MIT, textures CC BY-NC-SA 3.0); reading it for ideas is fine.


## Final review (2026-11-28)

Fixed: machines restart after a Link collects (Crystalarium, Solar Panel, Mushroom Box, tappers, Bee House honey recalculated), full storage leaves output in the machine instead of risking it, Machine Links can run the Processor Press, hovering a disk recipe no longer moves the old disk's contents, picked-up racks/generators/Cores holding state don't stack, the host rebuilds networks when a farmhand changes a block (and peers are told at once), Ports re-check buildings when one is built or moved, outdoor blocks joined by a Port now use the farmhouse's power, the T6 gate (Qi's Walnut Room = 100 Golden Walnuts) can be met, Maru's low-power letter, and the "reach all buildings early" option now works (off by default; in GMCM). In-game checks: `docs/testing/review-fixes.md`.

Still open (needs the user or a playtest): every "Proposed" rule in `docs/decisions.md` (including wire connecting only on the block's own tile), Coal value, Terminal reach default. Known limits: autocraft doesn't reserve ingredients; online farmhands can't craft or autocraft from a Terminal (storage only); Shipping Links attach to a Mini-Shipping Bin only; whole/flavoured items stay in linked chests; category ingredients aren't planned; Nexus mod ID for UpdateKeys.

## Art status (2026-11-27)

All 38 files in `docs/art-asset-list.md` are final Maru purple-and-oak exports in `artwork/exports/` (no placeholders left), including the two animated data-signal overlays (#37, #38). Wired in: item and machine sprites, state frames, wire/cable plus powered data signal (`AnimateDataCables` config), Link and Power Feed overlays on the objects they're attached to, Network View markers (Core, Terminal, Storage Rack full, linked machines), the Terminal's green screen frame and tab icons, meters, settings icons. `mod-icon.png` and `nexus-banner.png` are for the Nexus page only.
