# Pixel tools

Stardew Energistics uses a native grid, not a downscaled concept render. The approved Maru purple-and-oak board is an art reference. The Computer Core and Network Terminal were drawn directly as editable native pixel masters. Each stays on the same grid across power states.

## Recommended flow

1. Sample a small region of the supplied reference (cabinet, wood, or other relevant material), not the whole screenshot. Select a few deliberate shadow, midtone, and highlight swatches. Near-black appears in the source game art; this set deliberately avoids it for the hardware outlines. The palette uses a plum outline at HSV value 31%, muted violet casing, honey oak, and CRT green.
2. Draw at 1x when possible. For an image-generation pass, request equally spaced cells, fixed framing, no shadows outside the silhouette, a solid matte or alpha, no text and no filtering. Reload the accepted image in an editor and register the grid or redraw at native size. For clean integer-enlarged art, **snap** samples the center of each pixel block, so it does not average edge colors. It does not infer an unknown grid or repair inconsistent AI pixels. Use **grade** for alpha cleanup and HSV matching; a truly opaque matte must be removed before this step.
3. Slice uniform cells and pack them in the order in the project art list. In a state set, edit the screen or lights on one master; do not regenerate the whole machine. No jitter between states. Export only native PNGs; use nearest-neighbor scaling for review.

## Commands

Run from the project root. Requires Python with Pillow 12.3 or a compatible version with get_flattened_data.

~~~powershell
python -B tools/pixel/pixel_pipeline.py sample artwork/references/maru-bedroom.png --box 386 132 460 178
python -B tools/pixel/pixel_pipeline.py snap artwork/source/maru-machines/maru-machines-master-6x.png artwork/review/maru-machines/registered.png 48 64 --step 6
python -B tools/pixel/pixel_pipeline.py slice artwork/review/maru-machines/registered.png artwork/review/maru-machines/staged-cells 16 32 3 2
python -B tools/pixel/pixel_pipeline.py grade artwork/review/maru-machines/staged-cells/01.png artwork/review/maru-machines/core-on-graded.png artwork/source/maru-machines/maru-palette.json --floor .30 --sat 1 --val 1
python -B tools/pixel/pixel_pipeline.py check artwork/exports/computer-core.png 64 32 --floor .30
python -B tools/pixel/pixel_pipeline.py check artwork/exports/network-terminal.png 32 32 --floor .30
~~~

Use pack DEST FRAME0 FRAME1 ... for a horizontal strip (or --cols to make a grid). Slice accepts --origin X Y and --gap N. Snap accepts --origin X Y in enlarged pixels. Grade uses circular hue matching; --sat and --val are multipliers and --floor excludes palette entries below that value. It performs intentional palette mapping, not automatic style transfer.

Regenerate this set: python -B artwork/source/maru-machines/build_maru_machines.py --replace. The --replace flag allows overwriting the two named sheets, so use it only after reviewing any artist edits. The farm composite is a preview over the supplied image, not a test inside the game.
