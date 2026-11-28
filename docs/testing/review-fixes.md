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
