---
name: chapter-shape-vox
description: Step 3 of the 2D-to-3D map workflow — generate 40³ shape VOX models from a chapter's 24×24 tile PNGs, with sunken sand/water, raised grass and 3D tree crowns. Use when asked to convert ShapePanel tile PNGs to VOX, generate Shapes_NN models, or add trees/grass/water depth to a chapter's tiles.
---

# Step 3: generate the shape VOXs

Every tile id the cleaned chapter still uses becomes a `Shape_1_<id>.vox` on a
40³ canvas. The generation rules were reverse-engineered from chapter 01 and
reproduce all 96 of its reference models byte-for-byte, so the tool is the
authority — do not hand-write these.

## Inputs

- `Chapter_NN_UsedTiles.json` from step 2
- `Resources/Original/Shapes/ShapePanelNN/Shape_0_*.png`

## Output

`Resources/Remastered/Shapes/Shapes_NN/vox/Shape_1_<id>.vox`

## The rules the tool applies

1. each pixel takes the nearest MagicaVoxel default palette colour (indices
   1..255)
2. one voxel per pixel at `(px, 23 - py, level)`
3. `level`: land 23, **sand 22, water 21, deep water 20** — the shore steps
   down away from the land
4. dark green `(51,102,0)` is grass: 2 more voxels of the same colour stack
   above it, giving 3-voxel blades
5. a tile with a tree gets its ground **replaced wholesale** by the chapter's
   plain grass tile — so the painted 2D tree disappears — and a 3D crown is
   stamped on top. Tree tiles grow no grass blades.

Rule 5 needs `--grass-tile <id>`: the plain grass tile differs per panel (52 in
chapter 01, 153 in chapter 02). Pick it off the used-tile contact sheet before
you generate — an untextured, featureless green tile.

## Deciding the trees

This is the only judgement in the step. Build a labelled contact sheet of the
tiles the cleaned chapter still uses — every tile id, drawn at 5x, with its id
printed above it — and open it with the Read tool. That one image settles both
which tiles carry trees and which tile is the plain grass one, far faster than
cropping the map tile by tile.

Expect partial trees. Chapter 01's tree tiles each hold one whole tree, but
later chapters draw forests as clusters that straddle tile edges, so a tile may
show half a conifer. Stamp one crown per tile anyway — the alternative is a
forest that flattens to nothing in 3D — and match the crown to the species:
conifer art gets 47 or 42, round-tree art gets 43 or 44.

Pick a crown from chapter 01's six hand-built reference trees:

| ref tile | shape | crown top z |
|---|---|---|
| 46 | flat wide bush | 29 |
| 40 | rounded small bush | 30 |
| 43 | round tree (default) | 32 |
| 44 | taller round tree | 35 |
| 47 | small stepped conifer | 37 |
| 42 | tall stepped conifer | 38 |

Match the reference to what the 2D art draws — a low shrub gets 46 or 40, a
tall conifer gets 47 or 42. Preview any of them with:

```bash
python vox_preview.py ../../Resources/Remastered/Shapes/Shapes_01/vox/Shape_1_44.vox \
    --views front,iso --scale 5
```

## Generate

```bash
python shapes_to_vox.py NN \
    --used-tiles <path>/Chapter_NN_UsedTiles.json \
    --tree 71 --tree 72:44 --tree 80:42@12,11
```

`--tree ID` uses the default crown (43). `--tree ID:REF` picks the reference
tile. `--tree ID:REF@X,Y` also re-centres the crown inside the 24×24 tile —
only needed when the 2D tree is visibly off-centre.

Add `--dry-run` first to see the voxel counts, and `--force` to overwrite.

## Verify

Regression-check the rules against chapter 01 — this must stay clean:

```bash
python validate_shapes.py
# chapter 01: 96 tiles identical, 0 differ
```

Then eyeball a handful of the new tiles, especially every tree tile and any
tile with a shoreline:

```bash
python vox_preview.py ../../Resources/Remastered/Shapes/Shapes_NN/vox/Shape_1_<id>.vox \
    -o out/ --views front,iso --scale 5
```

On a shoreline tile the `front` view should show a visible step down from land
to sand to water, not a flat slab.
