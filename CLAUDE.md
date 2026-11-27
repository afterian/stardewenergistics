# Stardew Energistics — instructions for Claude

Start every session by reading `docs/HANDOFF.md`, then `docs/design-plan.md`. They hold the approved design, decisions, open items and next steps.

- **What:** a Stardew Valley SMAPI (C#, .NET 6, Stardew 1.6+) mod inspired by Applied Energistics 2: wired computer networks per building, tiered disk storage, configured machine automation, autocrafting, linked buildings. No channels, no auto mode, vanilla resources only, no Harvester/Planter.
- **Source of truth:** `docs/design-plan.md` (design), `docs/art-asset-list.md` (art), `docs/decisions.md` (confirmed decisions log). Update `docs/decisions.md` and `docs/HANDOFF.md` status as work lands.
- **User:** first-time modder. Claude writes and tests the code; give the user short, exact steps for anything they must do on the PC or Steam Decks.
- **Testing:** game-independent logic lives in `src/StardewEnergistics.Core` with xUnit tests in `tests/`. In-game testing on the Windows PC, then two Steam Decks via `deploy/` (see `deploy/README.md`).
- **Art:** placeholders until PNGs appear in `artwork/exports/` with the file names from `docs/art-asset-list.md`.
- **Respect:** credit AE2 as inspiration; never copy AE2 code, textures or names like "ME Drive".
