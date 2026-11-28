# Test mode (cheat menu)

Test mode is **on by default** while we playtest. Switch it off before release (config `TestMode`, or Generic Mod Config Menu → Stardew Energistics → Testing → "Test mode (cheats)").

## Open the menu

- **Steam Deck / controller:** click **both sticks in at the same time** (L3 + R3).
- **Keyboard:** **F7**.
- Change the button in GMCM ("Test menu button") or `TestMenuKey` in `config.json`.

It only opens when you're free to move (no menu or dialogue open). After loading a save a message reminds you how to open it.

## Choices

| Choice | What it does | Console equivalent |
|---|---|---|
| Item catalog | A free shop with every Stardew Energistics item, then the vanilla materials. Buy as many as you like (hold the button to buy more). | `se_give <name> [count]` |
| Starter kit | Computer Core, 2 Storage Racks, Power Rack, Coal Generator, Network Terminal, Crafting Rack, Processor Press, Network Wrench, 200 Copper Wire, 100 Heavy Cable, 2 Copper Disks, Iron Disk, 5 Chest Links, 5 Machine Links, Shipping Link, processors, 10 Battery Packs, 50 Coal, 4 Chests. | several `se_give` |
| Crafting materials | Bars, Refined Quartz, Battery Packs, Coal, Hardwood, Cloth, Fiber, Stone, Diamonds, Prismatic Shards, Qi Gems, Radioactive Bars, 2 Furnaces. | — |
| Unlock all recipes | Teaches every recipe (all six tiers). | `se_unlock all` |

Items that don't fit in the backpack open the usual "take these items" box.

## Multiplayer

Each player uses the menu on their own: the host, farmhands on another Deck, and split-screen players. It only changes your own recipes and backpack, so nothing needs the host.

## Check

1. Load a save. A message says how to open the test menu.
2. Click both sticks in. The test menu appears; pick each choice once and check the items/recipes arrive.
3. On the second Deck, join as a farmhand and repeat step 2.
4. GMCM → turn test mode off → both sticks does nothing.
