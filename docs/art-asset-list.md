# Stardew Energistics — Art Asset List

Approved 2026-11-27. Living copy: https://claude.ai/artifact/48iRmHg7rUvvhcSvZQv4DP

Complete list of the 36 asset files the mod needs, ready to hand to an art tool. Everything must read as native Stardew Valley pixel art, not Minecraft or AE2. Put editable originals in `artwork/source/` and final PNGs in `artwork/exports/` using the file names below.

## Style guide

- **Stardew pixel style.** Hard pixels, no anti-aliasing or blur, a dark outline (not pure black), light from the upper left.
- **Homemade farm tech.** Riveted metal boxes, wood trims, chunky dials, small blinking LEDs and green CRT screens. Think Maru's lab, not a sci-fi spaceship.
- **Tier colour coding.** Copper = orange-brown, gold = warm yellow, iridium = purple, prismatic = rainbow shimmer.
- **Network accent.** One shared soft teal glow marks anything "on the network": screens, LEDs, wire highlights. Use sparingly.
- **Racks form a server wall.** The Computer Core and every rack share height, base line, top trim and side edges, so side-by-side placement reads as one long machine.
- **States are readable.** Powered = lit LED/screen; unpowered = dark; warnings = small amber or red light.
- **No AE2 copying.** No AE2 textures, shapes or "ME" logo. Our emblem is a small copper circuit-leaf.

## Sprite specs

| Asset kind | Size per frame (px) | Notes |
| --- | --- | --- |
| Inventory item | 16 × 16 | Inventory, chests, terminal |
| Placeable machine or rack | 16 × 32 | One tile wide, two tall, like vanilla Kegs/Furnaces; bottom 16 px sits on the tile |
| Wire tile | 16 × 16 | Floor layer, drawn under the player |
| Attachment overlay | 16 × 16 | Drawn on top of a vanilla machine or chest |
| Small UI icon | 9 × 9 or 16 × 16 | As noted |
| Menu frame | 9-slice, 16 px corners | Stretches to any menu size |

- Draw at 1×; the game scales by 4. Never upscale the files.
- PNG with transparency, no background, no colour profile.
- Frames left to right in one strip per asset, in the order listed, all the same size.
- File names lower-case with dashes.
- One file per asset; code packs sheets.

## Inventory items (16 × 16, one frame)

| # | File | Item | Description |
| --- | --- | --- | --- |
| 1 | `copper-processor.png` | Copper Processor | Small square chip with orange-brown copper pins on all four sides and a plain dark top. |
| 2 | `gold-processor.png` | Gold Processor | Same chip shape, larger, gold pins, faint etched circuit pattern. |
| 3 | `iridium-processor.png` | Iridium Processor | Same shape, purple-iridium body, tiny teal glow at its centre. |
| 4 | `copper-wire-spool.png` | Copper Wire | Wooden spool wound with shiny copper wire, a loose end curling off. |
| 5 | `copper-disk.png` | Copper Disk | Chunky cartridge like a thick floppy disk, copper casing, white label strip. |
| 6 | `iron-disk.png` | Iron Disk | Same cartridge in grey iron with two rivets. |
| 7 | `gold-disk.png` | Gold Disk | Same cartridge in gold with a small teal LED. |
| 8 | `iridium-disk.png` | Iridium Disk | Same cartridge in iridium purple with two teal LEDs. |
| 9 | `prismatic-disk.png` | Prismatic Disk | Same cartridge in a rainbow sheen, slightly glowing outline. |
| 10 | `wireless-terminal.png` | Wireless Terminal | Handheld like a chunky walkie-talkie with a small green screen and stubby antenna. |

## Placeable machines and racks (16 × 32)

The first frame also serves as the inventory icon. Items 12–16 must line up into one server wall.

| # | File | Item | Frames (left to right) | Description |
| --- | --- | --- | --- | --- |
| 11 | `processor-press.png` | Processor Press | idle · working 1 · working 2 | Small workbench press with a copper lever and a glass window; working = lever down, sparks in the window. |
| 12 | `computer-core.png` | Computer Core | off · on · on, LEDs blink · low power | The "big computer": tall cabinet, green CRT near the top, row of LEDs, power switch. Low power = amber light, dimmer screen. |
| 13 | `storage-rack.png` | Storage Rack | 0 disks · 1 · 2 · 3 · 4 | Cabinet with four horizontal disk slots; each filled slot shows a disk edge and a teal LED. |
| 14 | `power-rack.png` | Power Rack | 0 batteries · 1 · 2 · 3 · 4 | Cabinet with four bays shaped like vanilla Battery Packs and a vertical charge meter. |
| 15 | `crafting-rack.png` | Crafting Rack | idle · working 1 · working 2 | Cabinet with a small robotic arm behind glass; the arm moves while working. |
| 16 | `advanced-crafting-rack.png` | Advanced Crafting Rack | idle · working 1 · working 2 | Iridium-trimmed Crafting Rack with two arms and a purple glow. |
| 17 | `network-terminal.png` | Network Terminal | off · on | Desk-height console with keyboard and green screen showing a tiny item grid. Stands away from the wall. |
| 18 | `coal-generator.png` | Coal Generator | off · burning 1 · burning 2 | Stout iron stove with chimney pipe and copper coil; fire glows through the grate when burning. Clearly low-tech. |
| 19 | `network-port.png` | Network Port | off · connected | Weatherproof box on a building's outside wall beside the door, socket at the bottom where cable plugs in. Connected = teal LED. |
| 20 | `island-uplink.png` | Island Uplink | off · on 1 · on 2 | Small satellite dish on a post; on = teal signal pulse rising from the dish. |

## Wire tiles (16 × 16, 16 frames each)

Frame number = sum of connections: N = 1, E = 2, S = 4, W = 8 (frame 0 = stub, 5 = straight N–S, 10 = straight E–W, 15 = cross). Ends meet the tile-edge centre so tiles join seamlessly.

| # | File | Frames | Description |
| --- | --- | --- | --- |
| 21 | `wire-tiles.png` | 0–15 in number order | Thin copper wire with small staples, lying flat on the floor. |
| 22 | `wire-tiles-glow.png` | same 16 | Same wire with a soft teal glow, shown only in Network View. |
| 35 | `heavy-cable-tiles.png` | 0–15 in number order | Thick rounded cable ~6 px wide, light-grey rubber jacket, darker outline, top highlight, a small metal band per tile. Drawn in light greys so code can tint it any of Stardew's 20 chest colours; must look good tinted red, blue, green or yellow. No glow version needed. |

Covered wire is simply not drawn, so it needs no art.

## Attachment overlays (16 × 16)

Small, in a top corner, so the vanilla sprite stays recognisable.

| # | File | Item | Frames | Description |
| --- | --- | --- | --- | --- |
| 23 | `chest-link.png` | Chest Link | linked | Small copper clamp with a teal LED, bolted to the chest's upper corner. |
| 24 | `machine-link.png` | Machine Link | not set up · idle · working · missing input | Small box with one LED: amber = not configured, teal = idle, blinking teal = working, red = out of input. |
| 25 | `shipping-link.png` | Shipping Link | idle · shipping | Copper clamp with a tiny coin symbol; coin glows while shipping overnight. |
| 26 | `power-feed.png` | Power Feed | off · charging | Small junction box with a lightning-bolt symbol at the base of a Lightning Rod or Solar Panel. |

## Tools

| # | File | Size | Frames | Description |
| --- | --- | --- | --- | --- |
| 36 | `network-wrench.png` | 16 × 16 | 1 | Copper adjustable wrench with a small teal LED in the handle, so it reads as a network tool. |

## UI and promo art

Menus otherwise reuse Stardew's own frames and fonts.

| # | File | Size per frame (px) | Frames | Description |
| --- | --- | --- | --- | --- |
| 27 | `terminal-frame.png` | 48 × 48, 9-slice | 1 | Border like vanilla's wooden frame, copper trim, dark green screen inset for the item grid. |
| 28 | `tab-icons.png` | 16 × 16 | storage · craft · jobs | Wooden crate, hammer, turning gear. |
| 29 | `settings-icons.png` | 9 × 9 | up · down · add · remove · copy · paste · clock | Machine Link settings buttons. |
| 30 | `power-meter.png` | 48 × 8 | empty frame · fill | Horizontal battery bar; fill cut to length in code. Green, amber below 25%. |
| 31 | `storage-meter.png` | 48 × 8 | empty frame · fill | Same shape, teal fill, for kinds/items used on a disk. |
| 32 | `network-view-markers.png` | 9 × 9 | connected · no power · not set up · full · missing input | Teal tick, crossed plug, amber wrench, box with "!", empty basket. |
| 33 | `mod-icon.png` | 64 × 64 | 1 | Copper circuit-leaf emblem on a dark rounded square. |
| 34 | `nexus-banner.png` | 480 × 120, shown at 4× | 1 | Cozy shed interior at evening: wall of copper and gold racks with glowing screens, wire to a row of Kegs, title "Stardew Energistics" in a pixel font. No AE2 art or logos. |
