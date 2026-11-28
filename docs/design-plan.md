# Stardew Energistics — Mod Design Plan (v3, approved)

Approved 2026-11-27. Living copy: https://claude.ai/artifact/XZ6h7hFyLrDj7a15X1MQ1d (this file is the local source of truth for implementation).

**Stardew Energistics** is a SMAPI (C#) mod, made in tribute to Applied Energistics 2, that lets a player wire their farm into one computer network: storage, machine automation and crafting. Version 3 brings wires back without channels, makes each building its own subnetwork, and uses only vanilla resources.

## 1. Vision and design pillars

The player builds a big computer in each building, runs copper wire out to their machines and chests, and later links every building to the farmhouse. By Year 2 the whole farm runs from one terminal. Target player: a relaxed Stardew player who wants to stop juggling items and automate the farm, not a network engineer.

- **Wires, not channels.** You craft wire and lay it to connect everything. No channel count, routing or capacity. If it touches the wire, it's connected.
- **A computer you can see grow.** The Computer Core is expanded by placing racks beside it, so a late-game setup looks like a wall of servers.
- **Each building is a subnetwork.** A shed's computer runs the shed. Later, a Network Port lets you run cable outside and join that building to the farmhouse.
- **Vanilla resources only.** Copper, Iron, Gold, Iridium, Refined Quartz, Battery Packs, Coal, Diamonds etc. The only new crafted parts are **Processors** and **Wire**.
- **Power that doesn't nag.** A Battery Pack runs a small network for about two weeks; a coal-burning generator is the low-tech option.
- **You set every rule.** Machines do nothing until the player configures them. There is NO automatic mode.
- **Gated pace.** Components first, then simple storage and crafting, then bigger storage and automation, then autocrafting, and finally linked buildings and advanced crafting.

## 2. What we keep from AE2, and what we cut

Rules reference: [AE2 1.21.1 guide](https://guide.appliedenergistics.org/1.21.1/).

| AE2 system | Decision | Stardew version |
| --- | --- | --- |
| ME network, one shared inventory | Keep | One network per building, joinable into one farm network |
| Cables | Keep, simplified | Copper Wire (small) + Heavy Cable (big, paintable); connected by touch; no capacity |
| Channels, dense cable, routing | Cut | Nothing to count |
| Controller multiblock | Keep the look | Computer Core plus racks placed beside it |
| Subnetworks | Keep | Every building is a subnetwork |
| P2P Tunnels, Quantum Bridge | Simplify | Network Port on a building's outside wall; Island Uplink for Ginger Island |
| Energy | Simplify | Battery Packs that last weeks, or a coal generator |
| Storage cells and drives | Simplify | Storage Rack with disks; a kind limit and an item limit, no byte maths |
| Terminals | Keep | Network Terminal (works like a crafting table), later a Wireless Terminal |
| Storage Bus | Keep | Chest Link |
| Import / Export Bus, Interface | Merge | Machine Link, configured by the player |
| Autocrafting, Crafting CPU | Keep, simplified | Crafting Rack on the computer; known recipes, no patterns |
| Level Emitter + Crafting Card | Simplify | Keep-in-stock rules (advanced crafting) |
| Upgrade cards | Cut | Tiered parts instead |
| Planes (harvester/planter), Spatial IO, tools, decorations | Cut | — (explicitly no Harvester or Planter) |
| Certus Quartz, Fluix, meteorites | Cut | Vanilla resources only |

## 3. How it plays in Stardew

A first network is a Computer Core in the player home, then a sub computer for each shed, a Storage Rack beside it, a Terminal, and copper wire running to a few chests. Later networks are a wall of racks with wire to every Keg in the building.

### Components first

| Component | Made in | From |
| --- | --- | --- |
| Copper Wire (x10) | Crafting menu | 1 Copper Bar + 1 Fiber |
| Copper Processor | Processor Press, overnight | 1 Copper Bar + 1 Refined Quartz |
| Gold Processor | Processor Press, overnight | 1 Gold Bar + 1 Refined Quartz + 1 Copper Processor |
| Iridium Processor | Processor Press, 2 days | 1 Iridium Bar + 1 Diamond + 1 Gold Processor |

### Wires and the computer

- **Two wire sizes, no channels.** Both connect anything they touch; neither has a capacity.
  - **Copper Wire** (small): thin, cheap, for indoors and short runs.
  - **Heavy Cable** (big): thick cable for trunk lines and outdoor runs between buildings. Paintable in any of Stardew's 20 chest colours (visual only).
- **Wire and cable sit on their own floor layer.** Walkable; flooring or paths can be laid on top, which hides them.
- **Network Wrench** (tool): tap a wire to cover/uncover it; hold to cover a whole connected run. Covered wire is invisible except in Network View. The wrench also paints Heavy Cable and opens a Link's settings.
- **Anything touching the wire network is connected**: racks, terminals, links, the port.
- **The Computer Core** is the brain; each network has exactly one. **Racks** placed beside the Core (or beside another rack), or connected by wire like any block, make it bigger: Storage Racks hold disks, Crafting Racks run autocrafting jobs, Power Racks hold batteries.
- **Network View** (hotkey, or holding the Wrench) shows all wire, covered or not, plus anything unpowered, unconfigured or full.

### Buildings as subnetworks

- Each building (farmhouse, sheds, barns, coops, greenhouse, cellar) has **its own Computer Core** and runs on its own.
- The outdoor farm needs no Core of its own: outdoor wire runs straight into the farmhouse's Network Port.
- **Network Port** (T5) mounts on a building's outside wall beside the door. Run Heavy Cable across the farm from one port to the farmhouse's port, and that building's network joins the farmhouse network.
- Once joined, the farmhouse terminal sees every linked building's storage, and crafting can pull ingredients from anywhere. Every Terminal, indoors or outside, reaches all linked subnetworks.
- **Island Uplink** (endgame) joins the Ginger Island farm, since you can't run wire across the sea.

### Power

- Each block uses a small amount of power per day. A starter network (Core, 1 rack, 1 terminal, 10 links) uses about 7 power a day.
- **Battery Pack** in a Power Rack = 100 power, about two weeks for a starter network. A Power Rack holds 4.
- **Coal Generator** (low-tech): load Coal or Hardwood; 1 Coal = 5 power (confirmed 2026-11-28; about 1–2 Coal a day for a starter network). Burns only when batteries are empty.
- **Out of power**: storage still opens, but machines and crafting pause. A mail note and an icon on the Core warn 3 days ahead.
- Late game, a Power Feed wired to Lightning Rods or Solar Panels sends their batteries straight into the Power Rack.

### Storage

- **Disks** go in Storage Racks (4 per rack): Copper, Iron, Gold, Iridium, Prismatic. Each shows kinds and items, e.g. "Gold Disk: 63 kinds, 32,000 items".
- **Quality and flavour don't use extra kinds.** All Parsnips are one kind; all Jellies are one kind. The terminal still lists each quality and flavour as its own row.
- Tools, rings and weapons are stored whole and count one kind each.
- **Chest Link** on any chest, fridge or Junimo Chest adds it to storage, so existing chests keep working.

### Machines (configured, never automatic)

Vanilla 1.6 machines all run from [Data/Machines](https://stardewvalleywiki.com/Modding:Machines), so one Machine Link works with every vanilla and modded machine.

- **Machine Link** sits on a machine and covers every identical machine touching it (a row of 30 Kegs needs one Link).
- **A new Link does nothing** until the player opens its settings and sets:
  - **Inputs**: which items to feed, in priority order ("Ancient Fruit, then Starfruit").
  - **Keep back**: amounts never to use ("leave 20 Hops for seeds").
  - **Output**: collect into storage (default) or leave in the machine.
  - **Schedule**: always, or only at certain hours.
- **Copy and paste settings** between Links with a shortcut.
- **Shipping Link** on the Shipping Bin is configured the same way: "ship all Starfruit Wine above 50".

### Crafting

- **The Terminal is a crafting table** (T2). It opens Stardew's familiar crafting menu with the player's known recipes, but ingredients come from network storage. It also has a Storage tab to search and take items.
- **It reaches every linked subnetwork.** A Terminal in any building, or outside on the farm, sees all buildings joined by Network Ports. Before T5 it sees its own building's network (OPEN: user may want all buildings from the start).
- **Autocrafting** (T4): a Crafting Rack lets you request "5 Quality Sprinklers". The network crafts sub-parts and uses linked Furnaces for bars. One job per Crafting Rack.
- **Advanced crafting** (T6): chains across linked buildings, cooking recipes, keep-in-stock rules ("always keep 100 Speed-Gro"), scheduled jobs.

## 4. Progression and unlock path

Components → small storage network in late Spring Y1 → machine automation in Summer → autocrafting in Fall → whole farm linked in Year 2.

| Tier | When | Gate | Unlocks |
| --- | --- | --- | --- |
| T1 Components | Spring Y1 | Talk to Maru once; she mails the Processor Press blueprint the next morning; Copper Bars + Refined Quartz | Processor Press, Copper Processor, Copper Wire |
| T2 Simple storage + basic crafting | late Spring Y1 | 10 Copper Processors crafted, Mines level 20, Maru 2 hearts | Computer Core, Storage Rack, Copper Disk, Terminal, Chest Link, Power Rack, Coal Generator, Network Wrench |
| T3 Bigger storage + automation | Summer Y1 | Mines level 80 (Gold), Maru 5 hearts | Gold Processor, Iron and Gold Disks, Machine Link, Shipping Link, Heavy Cable |
| T4 Autocrafting | Fall Y1 | Mines floor 120, Maru 7 hearts | Crafting Rack, multi-step crafting inside one building |
| T5 Linked buildings | Winter Y1 – Y2 | Skull Cavern floor 25, Iridium Bars, Maru 8 hearts | Iridium Processor, Network Port, Iridium Disk, Wireless Terminal, Power Feed |
| T6 Whole-farm automation | Year 2+ | Ginger Island, Qi's Walnut Room, Qi Gems, a Prismatic Shard | Advanced Crafting Rack, Prismatic Disk, Island Uplink |

- **Recipes arrive by mail** from Maru as friendship rises. Each letter explains what the new parts do.
- **Tiers also need the previous tier built**, not just unlocked (e.g. Machine Links need a working Storage Rack).
- **Disk tiers cost real farm output.** Each disk is crafted from the tier below plus bars and a processor.
- **End goal**: every building linked to the farmhouse, every machine configured, keep-in-stock rules running the farm.

## 5. Content catalog (v1) — 26 items

Recipes are first-pass; tune in playtests.

| Item | T | Does | Draft recipe |
| --- | --- | --- | --- |
| Processor Press | 1 | Machine that makes Processors | 5 Copper Bar + 2 Iron Bar + 1 Refined Quartz |
| Copper Processor | 1 | Basic component | Press: 1 Copper Bar + 1 Refined Quartz |
| Gold Processor | 3 | Mid component | Press: 1 Gold Bar + 1 Refined Quartz + 1 Copper Processor |
| Iridium Processor | 5 | Late component | Press: 1 Iridium Bar + 1 Diamond + 1 Gold Processor |
| Copper Wire (x10) | 1 | Connects everything it touches | 1 Copper Bar + 1 Fiber |
| Heavy Cable (x10) | 3 | Thick, paintable cable for trunk and outdoor runs | 2 Copper Wire + 1 Iron Bar + 1 Cloth |
| Network Wrench | 2 | Cover/uncover wire, paint Heavy Cable, open Link settings | 2 Iron Bar + 1 Copper Processor |
| Computer Core | 2 | The network's brain; one per building | 4 Copper Processor + 5 Iron Bar + 1 Battery Pack |
| Storage Rack | 2 | Beside the Core; holds 4 disks | 2 Copper Processor + 3 Iron Bar |
| Power Rack | 2 | Holds 4 Battery Packs | 1 Copper Processor + 2 Copper Bar + 1 Battery Pack |
| Coal Generator | 2 | Burns Coal or Hardwood for power | 1 Copper Processor + 1 Furnace + 5 Stone |
| Copper Disk | 2 | 20 kinds, 2,000 items | 2 Copper Bar + 1 Copper Processor |
| Iron Disk | 3 | 40 kinds, 8,000 items | Copper Disk + 3 Iron Bar + 1 Copper Processor |
| Gold Disk | 3 | 63 kinds, 32,000 items | Iron Disk + 3 Gold Bar + 1 Gold Processor |
| Iridium Disk | 5 | 63 kinds, 128,000 items | Gold Disk + 3 Iridium Bar + 1 Iridium Processor |
| Prismatic Disk | 6 | Unlimited kinds, 512,000 items | Iridium Disk + 1 Prismatic Shard + 10 Qi Gems |
| Network Terminal | 2 | Crafting-table menu + Storage tab, from network storage | 2 Copper Processor + 1 Refined Quartz + 1 Copper Bar |
| Chest Link | 2 | A chest or fridge joins storage | 1 Copper Processor + 1 Copper Bar |
| Machine Link | 3 | Feeds and empties a machine group, per its settings | 1 Gold Processor + 2 Iron Bar |
| Shipping Link | 3 | Ships above set amounts each night | 1 Machine Link + 1 Gold Bar |
| Crafting Rack | 4 | Beside the Core; runs one autocraft job | 2 Gold Processor + 2 Gold Bar + 1 Diamond |
| Network Port | 5 | Building outside wall; joins its network to the farmhouse | 2 Iridium Processor + 5 Copper Wire + 1 Gold Bar |
| Power Feed | 5 | Wire to Lightning Rods / Solar Panels; batteries go to the Power Rack | 1 Gold Processor + 2 Iridium Bar |
| Wireless Terminal | 5 | Terminal anywhere on the farm and in linked buildings | Network Terminal + 1 Iridium Processor + 1 Battery Pack |
| Advanced Crafting Rack | 6 | Cross-building chains, cooking, keep-in-stock, schedules | Crafting Rack + 2 Iridium Processor + 5 Qi Gems |
| Island Uplink | 6 | Joins Ginger Island farm to the farmhouse network | 2 Iridium Processor + 1 Prismatic Shard + 5 Radioactive Bar |

A disk crafted into the next tier keeps everything stored on it.

## 6. Technical architecture

Single C# SMAPI mod (SMAPI 4.x, .NET 6, Stardew 1.6+). The C# mod injects its own content; Content Patcher is not required. Wires without channels need only a connected-tiles search, re-run when a wire or block is placed or removed.

1. **Content layer.** Inject items, recipes, Processor Press machine rules and Maru's letters into `Data/Objects`, `Data/BigCraftables`, `Data/CraftingRecipes`, `Data/Machines`, `Data/Mail` via SMAPI `AssetRequested`. Use namespaced string IDs (e.g. `StardewEnergistics.ComputerCore`).
2. **Wire graph.** Per location, flood fill from each Computer Core across wire tiles and adjacent blocks. Racks join through wire like other blocks, and also by touching the Core or another rack. Ports join two locations' graphs (building interior ↔ farm exterior ↔ farmhouse).
3. **Power service.** Daily power use summed per network at `DayStarted`; batteries, then generators drawn down; warnings mailed 3 days ahead.
4. **Storage service.** Index `ItemKey → count` across disks and Chest Links. `ItemKey` = item ID + quality + flavour; the kind limit counts base item IDs only.
5. **Machine service.** Each Machine Link finds its group of identical touching machines and applies its settings through the game's own machine logic (`MachineDataUtility` / `Object.PlaceInMachine` etc.).
6. **Crafting service.** Plans a recipe tree from known recipes and `Data/Machines`, uses stock first, runs one job per Crafting Rack.
7. **Tick scheduler.** Links run on `TimeChanged` (every 10 game minutes); overnight pass at `DayEnding` for finished machines and keep-in-stock.
8. **UI.** Terminal (crafting-table menu + Storage + Jobs tabs), Link settings menu, wire auto-tiling (4-bit N/E/S/W mask), Network View overlay, Wrench interactions.

**Persistence and multiplayer**
- Disk contents in the mod's save data (`helper.Data.WriteSaveData`), keyed by disk ID. Block settings in each object's `modData` (survive pick-up). Wire/cable layer + covered flag + colour stored per location in save data.
- Host-authoritative: the host runs all network logic. Farmhands send requests (take, craft, change settings) via SMAPI multiplayer messages and receive terminal views. One shared farm network for all players.

**Compatibility**
- Any machine in `Data/Machines` (including Content Patcher machines) works with Machine Links.
- Chests used by Automate still work with a Chest Link.
- Generic Mod Config Menu for mod-wide settings: power use, disk sizes, tier gates on/off.

**Performance budget**: under 1 ms per 10-minute tick with 100 Machine Links. Cache the wire graph, round-robin Links, dictionary lookups.

## 7. Development roadmap

Each milestone is playable from a fresh save up to that point.

1. **M0 — Setup.** Dev environment, GitHub repo, empty mod loads in SMAPI, Steam Deck test pipeline working end to end. Done when: SMAPI console shows "Stardew Energistics loaded" on PC and both Decks.
2. **M1 — Components and wire.** Processor Press, three processors, Copper Wire + Heavy Cable with auto-joining sprites, Network Wrench cover/uncover/paint, save and load. Done when: wire laid in a shed still joins up correctly after reload.
3. **M2 — Simple storage and basic crafting.** Computer Core, Storage Rack, disks, Terminal (crafting-table menu + Storage tab), Chest Link, Power Rack, Coal Generator. Done when: a shed network holds 20 kinds and runs two weeks on one Battery Pack.
4. **M3 — Machine automation.** Machine Link settings menu, machine groups, copy/paste settings, Shipping Link, overnight pass. Done when: one configured Link runs 30 Kegs for a whole season.
5. **M4 — Autocrafting.** Crafting Rack, recipe planner, Furnace steps, job screen. Done when: one request for a Quality Sprinkler smelts its bars from ore and crafts it.
6. **M5 — Linked buildings.** Network Port, outdoor cable runs, combined storage view, Wireless Terminal, Power Feed. Done when: the farmhouse terminal shows items from three buildings.
7. **M6 — Whole farm and release.** Advanced Crafting Rack, Island Uplink, Prismatic Disk, all tier gates and Maru's letters, multiplayer hardening, Nexus release. Done when: two Steam Decks share one linked farm in online co-op.

## 8. Setup, testing and release

- The user has never made a Stardew mod. Claude writes the code; the user installs tools once, builds and play-tests.
- **PC setup:** Stardew Valley + SMAPI; .NET 6 SDK; Visual Studio 2022 Community (".NET desktop development") or Rider; Git + GitHub CLI (`gh auth login`). Build with `Pathoschild.Stardew.ModBuildConfig` (auto-deploys to `Mods`). [Modder guide](https://stardewvalleywiki.com/Modding:Modder_Guide/Get_Started).
- **Logic tests:** keep storage maths, recipe planning and wire graph in game-independent classes with xUnit tests, so they run without the game.
- **Bug reports:** upload the SMAPI log at https://smapi.io/log and share the link.
- **Steam Deck testing:** see `deploy/README.md`. PC: `deploy/publish-test.bat` → private GitHub release `test-latest`. Decks: launch option `~/stardew-energistics/launch.sh %command%` auto-installs the newest build on every game start; offline-safe; keeps `config.json`.
- **Multiplayer testing:** local split-screen co-op on one PC first, then the two Decks online.
- **Release:** every MP player needs the same mod version. Publish on Nexus Mods with `UpdateKeys` in `manifest.json`. Semver; save-breaking change bumps major.
- **Reference code:** [AE2 source](https://github.com/AppliedEnergistics/Applied-Energistics-2) (code LGPL-3.0, API MIT, textures CC BY-NC-SA 3.0) — read for ideas only; never copy code or art. [Automate source](https://github.com/Pathoschild/StardewMods) for how a mod drives vanilla machines.

## 9. Name and credits

- Name: **Stardew Energistics**.
- Credit line (Nexus + README): "Inspired by Applied Energistics 2 by AlgorithmX2 and the AE2 team. Not affiliated with or endorsed by them."
- Own identity: no AE2 textures, no item names like "ME Drive", no copied text. Look: Stardew-native copper, gold and iridium hardware.

## 10. Art

All 36 asset files with sizes, frames and descriptions: `docs/art-asset-list.md`. Art is produced in parallel by another tool; code uses placeholder sprites until exports land in `artwork/exports/`.
