# Stardew Energistics: farm-reference prompt

Working style experiment, not approved final art. Use the approved asset list for item names, constraints and states once production starts.

Use the supplied farm screenshot as the visual target for [ASSET NAME], a [MACHINE OR RESOURCE ITEM]. Match its apparent pixel size, camera, shading and palette. Do not redraw the farm or copy an existing sprite.

- Shape: straight-on front with a little of the top visible. Use horizontal front edges and vertical uprights, not a diamond/isometric view. Read cap, body and base before surface detail. For resources use one compact silhouette with one distinctive feature.
- Color and light: honey highlights, muted ochre/rust midtones and deep brown/plum shadows for wood and copper; warm gray/charcoal and subdued cool accents for stone and iron. Dark material-colored outlines, not a uniform black border. Upper-left light, darker undersides and lower-right recesses. A small lit indicator, no halo.
- Pixels: deliberate clusters, crisp stairs, a few value steps per material. Use sparse seams, chips and rivets. No antialiasing, gradients, bloom, or high-resolution surface noise.
- Output: isolated art with transparent background, no text or scenery. 16 x 32 source cells for one-tile machines; 16 x 16 for resources. Review enlargement uses whole square pixels; runtime PNGs remain native-size. Keep the same base, silhouette and registration for machine states; use one base and edit only the state detail.

For a batch, specify row and column counts, asset order, even gutters and separate machine and item rows. Verify the source sheet and save it before slicing. Review native cuts at farm scale before refining art.
