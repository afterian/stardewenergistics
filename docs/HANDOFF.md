# Stardew Energistics — Handoff for implementation

Date: 2026-11-27. Status: **M0 in progress.** Hello-world mod (`src/StardewEnergistics/`) plus the M0 scaffold: `StardewEnergistics.Core` with the §3 contracts of `docs/implementation-plan.md`, the 36-entry art catalogue, art export checks, xUnit tests and CI. Waiting on the user's PC setup (SMAPI, .NET SDK) for the first in-game load. Core done ahead of the game side: M1 wire layer + wire save model, and M2 network graph, disk storage and power ledger (88 tests). Next on the game side: M1 lane B/C (content injection, placeholders, wire placement and rendering) once the mod builds on the PC.

## What this project is

A Stardew Valley SMAPI (C#) mod inspired by Minecraft's Applied Energistics 2. The player builds a computer in each building, lays copper wire to chests and machines, stores everything on tiered disks, configures machines to run from storage, autocrafts, and finally links every building to the farmhouse. Aimed at relaxed players, not network engineers: **no channels, no auto mode, vanilla resources only.**

## Read these, in order

1. `docs/HANDOFF.md` — this file: status, decisions, next steps.
2. `docs/design-plan.md` — the full approved design (mechanics, tiers, 26-item catalog, architecture, roadmap). **Source of truth.**
3. `docs/implementation-plan.md` — code-level plan: solution layout, Core contracts, parallel lanes A–G, spikes, milestone tasks, how code depends on art.
4. `docs/art-asset-list.md` — 36 art files with sizes, frames, descriptions. Art is made in parallel with another tool.
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
- [ ] Exact installed game / SMAPI versions on the PC and Decks (record in `docs/decisions.md`).

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
