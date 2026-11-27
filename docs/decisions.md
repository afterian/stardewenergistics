# Decisions

Confirmed decisions log. The full design is in `docs/design-plan.md`; status and next steps in `docs/HANDOFF.md`.

## Confirmed

- Project name: Stardew Energistics; inspired by Applied Energistics 2, credited, not affiliated; no AE2 art, code or names.
- Platform: Stardew Valley 1.6+, single C# SMAPI mod (.NET 6) that injects its own content; Content Patcher not required.
- Working root and git repo: `E:/Projects/stardewenergistics/`.
- Design v3 approved 2026-11-27 (see `docs/design-plan.md`).
- Wires without channels: Copper Wire (small) + Heavy Cable (big, paintable, 20 chest colours); Network Wrench hides wire; flooring can cover wire.
- Each building is a subnetwork with its own Computer Core; Network Port + Heavy Cable joins it to the farmhouse; outdoor wire runs into the farmhouse port; Island Uplink for Ginger Island.
- Terminal works like a crafting table on network storage and reaches all linked subnetworks.
- Vanilla resources only; new crafted parts are Processors (Copper/Gold/Iridium) and wire.
- Power: Battery Packs (~2 weeks for a starter network) and a Coal Generator.
- No auto mode: Machine Links are always player-configured.
- No Harvester or Planter.
- Gated progression via Maru's letters by friendship hearts (T1–T6 in the plan).
- Multiplayer: host-authoritative, one shared farm network.
- Testing: Windows PC + two Steam Decks auto-updating from a private GitHub release before Nexus.
- Deck builds (Proposed, 2026-11-27): built on the user's PC as a GitHub self-hosted runner on every push to `main` (game files can't go on GitHub's machines), published to release `test-latest`; Deck launcher installs it on every game start and launches through SMAPI. Mod version stamped `x.y.z-build.N` so multiplayer players can compare builds.
- GitHub repo `afterian/stardewenergistics`; mod UniqueID `afterian.StardewEnergistics`, author `afterian` (2026-11-27).
- Build (Proposed, 2026-11-27): mod and Core target `net6.0` (the game's runtime); the test project targets `net8.0` so the current .NET 8 SDK runs it. Install the **.NET 8 SDK** on the PC; it builds net6.0 mods too.
- Sprites (Proposed, 2026-11-27): each art file is its own game asset `Mods/afterian.StardewEnergistics/Sprites/<name>`; item IDs are `afterian.StardewEnergistics_<Name>` (see `src/StardewEnergistics/Ids.cs`).
- Core rules (Proposed, 2026-11-27, change on request): racks join only by touching the Core or another rack (wire doesn't connect racks); two Cores on one network disable it; inserts go first to a disk already holding that kind, then rack order; extracts empty the last disk first; an unpowered day draws no power; default daily use Core 2, Storage/Crafting Rack 1, Terminal 1, each Link 0.3, Port 1 (starter network = 7/day, one Battery = 14 days); low-power mail once per drain.
- Art: produced in parallel by another tool from `docs/art-asset-list.md` (36 files).

## Open

- Wire connection rule: wire on the block's own tile only (current reading of the user's reply) vs also the 4 neighbouring tiles. Confirm before M2.
- Terminal reach before Network Ports (own building only vs all buildings).
- Coal Generator value (1 Coal = 1 power vs 10).
- Installed game and SMAPI versions on PC and Decks.
