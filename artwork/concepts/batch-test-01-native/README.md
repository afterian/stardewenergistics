# Batch test 01: native-size sprite cuts
Top row left to right: copper washer idle, copper washer working, stone press, charge station.
Bottom row: ore, copper ingot, teal crystal, blue charged cell.

Eight individual native PNGs, a 64 x 32 machine strip, a 64 x 16 resource strip, metrics, and an 8x nearest-neighbor review image. These are placeholder art tests, not installed or game-validated assets. The raw generator output remains in the parent folder.

Re-run with tools/Slice-SpriteTest.ps1 and pass an image path and a new output folder. This prototype uses a user-set horizontal split (default 0.59), four equal columns, an alpha cutoff (default 200), shared row scale and a shared first-pair anchor. It checks for empty or clipped cells. For further iteration, add palette cleanup and inspect repeat-frame stability. Native resolution doesn't imply a tight palette: the generated output retained many subtly different colors.
