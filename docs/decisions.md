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
- Deck builds (Proposed, 2026-11-27): built on the user's PC by an outbound-only watcher (scheduled task, no admin, builds only `main`), not a GitHub self-hosted runner, which the user rejected as a security risk. Published to release `test-latest`; the Deck launcher installs it on every game start and launches through SMAPI. Version `x.y.z-build.N` (N = commit count) so multiplayer players can compare builds. Building on GitHub's servers was rejected: it would mean uploading the game's copyrighted DLLs. Public vs private repo: both supported; private only adds the Deck token step.
- Deck install (Proposed, 2026-11-27): no typing on the Deck. A double-click `.desktop` installer (public repo) installs SMAPI and the launcher and sets the Launch Options by editing Steam's localconfig.vdf while Steam is closed. The guide assumes a public repo; the token path stays as a fallback.
- GitHub repo `afterian/stardewenergistics`; mod UniqueID `afterian.StardewEnergistics`, author `afterian` (2026-11-27).
- Build (Proposed, 2026-11-27): mod and Core target `net6.0` (the game's runtime); the test project targets `net8.0` so the current .NET 8 SDK runs it. Install the **.NET 8 SDK** on the PC; it builds net6.0 mods too.
- Sprites (Proposed, 2026-11-27): each art file is its own game asset `Mods/afterian.StardewEnergistics/Sprites/<name>`; item IDs are `afterian.StardewEnergistics_<Name>` (see `src/StardewEnergistics/Ids.cs`).
- Core rules (Proposed, 2026-11-27, change on request): racks join only by touching the Core or another rack (wire doesn't connect racks); two Cores on one network disable it; inserts go first to a disk already holding that kind, then rack order; extracts empty the last disk first; an unpowered day draws no power; default daily use Core 2, Storage/Crafting Rack 1, Terminal 1, each Link 0.3, Port 1 (starter network = 7/day, one Battery = 14 days); low-power mail once per drain.
- M3 rules (Proposed, 2026-11-27, change on request): a Machine Link runs every 10 game minutes and once overnight; one Link drives every identical machine touching its machine (up to 500); inputs are used in list order, lower qualities first, keep-back counts all qualities; output goes to storage, or stays in the machine when storage is full. Shipping Links ship everything above keep-back each night and attach only to a Mini-Shipping Bin until M5. Crafting a bigger disk from a smaller one keeps its contents.
- M4 rules (Proposed, 2026-11-27, change on request): autocraft uses crafting recipes any farmer knows plus simple Furnace rules; the requested item is always crafted, its parts come from stock first; lowest quality ingredients are used first; Furnace steps use Furnaces with a Machine Link (and Furnaces touching them) even if the Link isn't set up; jobs advance every 10 game minutes, one job per Crafting Rack, and are stored on the Computer Core; ingredients are not reserved.
- M5 rules (Proposed, 2026-11-27, change on request): a Network Port is placed on the farm on a wired tile directly beside a building (any side, not only the door), since objects can't go on building walls; it joins the network with a Core inside that building. All buildings whose Ports share one outdoor network form one group with shared storage; the farmhouse leads the group. Power stays per building; outdoor wire (no Core) uses the leading network's power. A building with its own Crafting Rack uses only its own linked Furnaces. The Wireless Terminal opens the network where you stand, or the farmhouse's anywhere on the farm or in a farm building.
- M6 rules (Proposed, 2026-11-27, change on request): keep-in-stock rules live on the Advanced Crafting Rack and start jobs at the end of each day (only for recipes that can be fully made); the Advanced rack also cooks and uses linked Furnaces in every building; every network with an Island Uplink joins every other one.
- Final review rules (Proposed, 2026-11-28, change on request): a Machine Link collects output like a player (the machine's "output collected" rule runs, so Crystalariums and Solar Panels restart); output that doesn't fit in storage stays in the machine; outdoor blocks joined by a Port add their daily use to the leading network; T6's "Qi's Walnut Room" gate means 100 Golden Walnuts found; Maru mails a low-power letter with each 3-day warning.
- Test mode (2026-11-28, user asked): a cheat menu, on by default until release (`TestMode` config + GMCM). Opens with both sticks clicked in (L3 + R3) or F7: free item catalog, starter kit, crafting materials, unlock all recipes. Acts on the local player only, so farmhands use it too. Switch off before release. See `docs/testing/test-mode.md`.
- Art: produced in parallel by another tool from `docs/art-asset-list.md` (36 files).
- Art style (2026-11-27): Maru purple-and-oak approved (amethyst cases, plum outlines, honey-oak supports, green CRTs). First final batch: `computer-core.png`, `network-terminal.png`, `wire-tiles.png`, `wire-tiles-glow.png`, `heavy-cable-tiles.png`. See `docs/artwork-pipeline.md`.
- Art complete (2026-11-27): all 38 assets final. Added #37 `copper-data-signal.png` and #38 `heavy-data-signal.png` (8 phases × 16 masks, drawn on powered wire). Unpainted Heavy Cable is tinted Maru plum.

- Wire connection (confirmed by the user in the playtest, 2026-11-28): wire on a block's own tile **or on any of the 4 tiles beside it** connects (`ConnectionRule.OwnTileOrNeighbours`, the default). Config `WireUnderBlockOnly` switches back to own-tile only.

## Open

- Terminal reach before Network Ports (own building only vs all buildings). Default own building; since 2026-11-28 the config option "Terminals reach all buildings early" (GMCM) joins every building's network to the farmhouse's without Ports.
- Coal Generator value (1 Coal = 1 power vs 10).
- Installed game and SMAPI versions on PC and Decks.
