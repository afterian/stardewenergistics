# Stardew Energistics: modding guide and asset specs

Researched 2026-11-27. This is a Windows PC, Stardew Valley 1.6-first plan, pending confirmation of the installed build. No in-game test has been run yet.

## What to install

1. Install the normal PC version of Stardew Valley. If you're using Steam, choose the E: library if available and launch the game once to the title screen. Steam Library > Stardew Valley > Manage > Browse local files opens the game directory.
2. For mod testing, use [SMAPI](https://smapi.io/) and [Content Patcher](https://github.com/Pathoschild/StardewMods/tree/stable/ContentPatcher). Follow the [Windows install guide](https://stardewvalleywiki.com/Modding:Installing_SMAPI_on_Windows). Confirm the exact game, SMAPI and Content Patcher versions from the installed build and startup log before pinning dependencies. SMAPI 4.5 currently lists 1.6.14 as its minimum PC game version, not necessarily our minimum.
3. Start with a clean test save. Keep the game files intact, and keep our source, exports, copies of reference assets, and tracked test notes under this E: project. Don't overwrite vanilla XNB files or use Ken's main save as the test bed.

## First asset brief

| Asset | Export | First pass | In-game registration |
|---|---|---|---|
| Placeable, one-tile machine | Transparent PNG, **16 x 32 px per frame** | One idle frame. For a distinct active look, use a **32 x 32 px** horizontal sheet with idle at index 0 and active at index 1. The 32 px height is drawing space, not a two-tile footprint. | `Data/BigCraftables` (`(BC)` item) points to its custom texture and base `SpriteIndex`; `Data/Machines` defines behavior. |
| Resource in inventory, as input or output | Transparent PNG, **16 x 16 px per icon** | One icon per resource. Three examples side by side form a **48 x 16 px** sheet at indexes 0, 1 and 2, or use separate 16 x 16 files at index 0. | `Data/Objects` (`(O)` item) maps each texture/index to name, description, category, price and tags as needed. |
| Optional prop/node in the world | Don't assume the inventory icon is the world sprite. | Decide whether it is a placed object, a harvestable map prop, a clump or a spawn. | Verify the appropriate data and interaction before fixing dimensions or draw origin. |
| Optional animated overlay / custom UI | Separate from the base sprite sheet. | Start static. Size any effect or UI sprites against their own render path at prototype time. | A timed working effect is not the same as a persistent machine sprite. Don't assume a third machine index is selected automatically. |

Sprite cells are indexed left to right, then top to bottom, starting at 0. Use an RGBA PNG with transparent empty pixels; no gutters, labels or preset scaled-up export in a runtime sheet. Use nearest-neighbor for 4x/8x review previews, not as the runtime resolution. Keep the machine's base and silhouette anchored consistently across states; suggest a visible silhouette, color and value change that still reads on dirt, grass, and a dark shed floor. Keep source files in `artwork/source/` and exports in `artwork/exports/`. Keep any vanilla texture dumps as private references only, not shipped art.

## Recommended implementation path

Use a Content Patcher pack for the first prototype: a manifest identifying Content Patcher, a content file, and original PNGs in its assets folder. Load each custom PNG at a unique virtual asset path (or use Content Patcher's InternalAssetKey token), then add records with EditData. Use stable, namespaced string IDs for items; machine-rule keys use the qualified `(BC)` item ID, and custom input/output references can use the qualified `(O)` ID. Do not overwrite vanilla numeric IDs or edit the vanilla sprite sheets just to add ours.

- **Placement and item appearance:** `Data/BigCraftables` for the machine, `Data/Objects` for resource items.
- **Processing:** `Data/Machines` defines triggers, inputs, quantity, duration and output. Its two optional next-index flags both point to the **next** cell relative to the base index: `ShowNextIndexWhileWorking` and `ShowNextIndexWhenReady`. For the first pass, use the swapped cell for processing only, and verify the normal output indicator on completion. For unique idle, processing and ready visuals, prototype effects or custom drawing rather than assuming three contiguous frames will cycle. Don't bake a floating output icon into the machine body.
- **Crafting/unlock:** `Data/CraftingRecipes` is separate from machine processing. Use the installed version's recipe schema; compare with one existing big-craftable recipe before wiring ingredients, result, unlock, or localized name. A resource icon doesn't define how that resource spawns or is obtained.
- **Where code starts:** If Energistics is meant to include a shared inventory across machines, power/cable pathfinding, a terminal, or custom network state, plan a separate SMAPI C# logic mod alongside the content pack. A basic processor does not need that layer. Don't add a legacy item/machine framework unless a specific requirement calls for it.

## First test and what the install lets us verify

Make or stub one machine and one original resource. For a concrete smoke test, use a vanilla item such as copper ore as the input and our resource as the output; use a short processing time for testing. That is a test fixture, not a design decision. Check hotbar/inventory icon, placement, collision and layering behind the player, state changes and collection, removal, crafting/unlock and save/reload in a clean test save. Add host/client and reconnect tests before making multiplayer claims.

Use the installed game to check the live `TileSheets/Craftables`, `Maps/springobjects`, `Data/BigCraftables`, `Data/Objects`, `Data/Machines`, and `Data/CraftingRecipes` assets. Compare Furnace or Recycling Machine for scale and state treatment; compare Copper Ore and Refined Quartz for small material silhouettes. Content Patcher's `patch export "TileSheets/Craftables"` (and equivalent commands for the other keys) writes into the **game install's** patch export folder. Use that route only if the install is on E: and we are authorized to run the mod tools; otherwise use a read-only inspection or a separately approved E: reference workflow. `patch summary` helps diagnose unapplied edits. Note exact version and whether other mods affected the reference.

## Sources

- [Content Patcher author guide](https://github.com/Pathoschild/StardewMods/blob/stable/ContentPatcher/docs/author-guide.md), [EditData](https://github.com/Pathoschild/StardewMods/blob/stable/ContentPatcher/docs/author-guide/action-editdata.md), and [troubleshooting / exports](https://github.com/Pathoschild/StardewMods/blob/stable/ContentPatcher/docs/author-guide/troubleshooting.md).
- [Big craftable data](https://stardewvalleywiki.com/Modding:Big_craftables), [object data](https://stardewvalleywiki.com/Modding:Objects), [machine rules and effects](https://stardewvalleywiki.com/Modding:Machines), and [crafting recipe data](https://stardewvalleywiki.com/Modding:Recipe_data).
- [Json Assets maintainer's sprite dimensions](https://github.com/spacechase0/StardewValleyMods/blob/develop/framework/JsonAssets/docs/author-guide.md), used as a sprite-size reference only; Json Assets isn't needed for this prototype.
