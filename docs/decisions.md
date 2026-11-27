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
- GitHub repo `afterian/stardewenergistics`; mod UniqueID `afterian.StardewEnergistics`, author `afterian` (2026-11-27).
- Art: produced in parallel by another tool from `docs/art-asset-list.md` (36 files).

## Open

- Wire connection rule: wire on the block's own tile only (current reading of the user's reply) vs also the 4 neighbouring tiles. Confirm before M2.
- Terminal reach before Network Ports (own building only vs all buildings).
- Coal Generator value (1 Coal = 1 power vs 10).
- Installed game and SMAPI versions on PC and Decks.
