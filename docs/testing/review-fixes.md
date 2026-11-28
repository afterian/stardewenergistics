# Final review fixes: in-game checks

Fixes from the final code review (after M6). None of these has been tried in the game yet. Use any test save with a powered network.

| # | Do this | Expect |
| --- | --- | --- |
| 1 | Machine Link on a Crystalarium with a gem in it; let it finish | The copy goes to storage and the Crystalarium starts the same gem again, without taking a new one |
| 2 | Power Feed on a Solar Panel; wait for two batteries | Both batteries reach the Power Rack (the panel keeps working after the first) |
| 3 | Machine Link on a Processor Press, inputs: Copper Bar; Refined Quartz in storage | The Press loads from storage and makes Copper Processors |
| 4 | Fill storage, then let a linked Keg finish | The wine stays in the Keg (nothing lost); it's collected once there's room |
| 5 | Put items on a Copper Disk, then just hover the Iron Disk recipe in the crafting menu, then close it | The Copper Disk still has all its items |
| 6 | Craft the Iron Disk from that Copper Disk | The Iron Disk has the same items |
| 7 | Pick up a Storage Rack with disks in it, and an empty one | They don't stack; placing the first back still shows its disks |
| 8 | Online co-op: the farmhand adds a Chest Link | The host's Terminal shows that chest's items straight away |
| 9 | T6 gates on: 100 Golden Walnuts found, Ginger Island visited, a Prismatic Shard in the inventory, T5 built | Maru's T6 letter arrives the next morning (it never could before) |
| 10 | Let a network's power fall to 3 days | HUD warning plus a letter from Maru |
| 11 | Outdoor wire with Network Ports and Machine Links: check the farmhouse Core's daily use | It includes the outdoor blocks (1 per Port, 0.3 per Link) |
| 12 | Generic Mod Config Menu: turn on "Terminals reach all buildings early" | A farmhouse Terminal shows a shed's storage without any Network Port |

## Decisions applied (2026-11-28)

| # | Do this | Expect |
| --- | --- | --- |
| 13 | Load a save whose config.json has `"CoalPower": 1` | It becomes 5 (and `"ConfigVersion": 1`); one Coal adds 5 fuel |
| 14 | Right-click the farm's Shipping Bin holding a Shipping Link | "Link attached to the Shipping Bin…"; the Link is used up |
| 15 | Run wire from an outdoor Computer Core (with power) to a tile right beside the bin; wrench the bin, add an item with Keep 2; put 10 in storage; sleep | 8 shipped overnight; the shipping summary shows them |
| 16 | Wrench the bin, press Remove | The Shipping Link drops back |
| 17 | Autocraft 2 Iron Bars' worth of something (e.g. a Sprinkler) with exactly the ore/bars it needs in storage | Right after Start, those ingredients are gone from the Terminal (set aside); the job still finishes and the item lands in storage |
| 18 | Start a job, then cancel it at once | The set-aside ingredients come back to storage |
| 19 | A Machine Link feeds Kegs from the same storage while a job runs | The job isn't starved: its items are set aside |
| 20 | Two Computer Cores touching one network | Right-click a Core: "two Cores" problem; nothing runs until one is moved |

## Controller targeting (Steam Deck playtest, 2026-11-28)

Bug: on a controller, a Battery Pack wouldn't go into a Power Rack and Coal wouldn't go into the Coal Generator. The mod only looked at the tile under the cursor or straight in front of the player. The game also lets controllers reach the 8 tiles around the player, and players often press X (use) instead of A.

| # | Do this (controller) | Expect |
| --- | --- | --- |
| 1 | Stand next to a Power Rack (also diagonally, or not facing it), hold a Battery Pack, press A | Battery goes in, "1/4 Battery Packs" |
| 2 | Same, press X | Same |
| 3 | Hold Coal next to the Coal Generator, press A or X | Fuel loaded message |
| 4 | Hold a Battery Pack next to a vanilla chest (no rack nearby), press A | Normal game behaviour |
| 5 | With the mouse, click a rack 1 tile away | Works as before; clicking empty ground does nothing special |

## Playtest fixes (2026-11-28)

1. Put a Copper Disk with items on it into a Storage Rack, then break the rack with an axe or pickaxe. The disk pops out. Put it into a rack again: the Terminal shows the same items.
2. Break a Power Rack holding batteries and a Coal Generator with fuel: Battery Packs and Coal pop out.
3. Open the Terminal and click a stack in your backpack. It leaves your backpack and shows in storage (no duplicate). Right-click stores one.
4. Click an item when the network has no disks: the message says there are no disks, and the item stays in your backpack.
5. Store a plain Pickaxe in the Terminal: it goes on a disk. Take it out: it's the same pickaxe level. An enchanted tool says to use a Chest Link.
6. Take a whole stack out: the disk meter's kinds and items go down.
7. Test menu (L3+R3 or F7) > Recover lost disks: a disk lost before this fix comes back in your backpack with its items. Running it again finds 0.
