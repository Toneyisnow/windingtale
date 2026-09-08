---
name: chapter-shape-vox
description: Step 3 of the 2D-to-3D map workflow — generate 40³ shape VOX models from a chapter's 24×24 tile PNGs, with sunken sand/water, raised grass and 3D tree crowns. Use when asked to convert ShapePanel tile PNGs to VOX, generate Shapes_NN models, or add trees/grass/water depth to a chapter's tiles.
---

# Step 3: generate the shape VOXs

Every tile id the cleaned chapter still uses becomes a `Shape_<NN>_<id>.vox` on
a 40³ canvas. The generation rules were reverse-engineered from chapter 01 and
reproduce all 96 of its reference models byte-for-byte, so the tool is the
authority — do not hand-write these.

## Inputs

- `Chapter_NN_UsedTiles.json` from step 2
- `Resources/Original/Shapes/ShapePanelNN/Shape_<NN-1>_*.png`

## Output

`Resources/Remastered/Shapes/Shapes_NN/vox/Shape_<NN>_<id>.vox`

The prefixes are one apart — the source PNG's is the panel's 0-based index, the
VOX's is the chapter's 1-based one (`ShapePanel02/Shape_1_153.png` →
`Shapes_02/vox/Shape_2_153.vox`). `voxlib.tile_png_name()` /
`shape_vox_name()` own this; never build the names by hand.

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

Rules 3 and 4 are colour tables read off chapter 01, and a chapter with no
shore can paint those colours for something else: chapter 10's cave has no
sand, but the orange of its fire rings resolves to the sand family and would
sink one voxel. Pass `--flat` on such a chapter and every pixel stays at
ground level.

Rule 5 needs `--grass-tile <id>`: the plain grass tile differs per panel (52 in
chapter 01, 153 in chapter 02). Pick it off the used-tile contact sheet before
you generate — an untextured, featureless green tile.

## Deciding the trees

**Trees are obstacles now** (`chapter-obstacles`, step 2b): `tree_obstacles.py`
lifts every tree tile out of the matrix before this step, so a chapter done
that way has no tree tiles left in its `UsedTiles` and needs no `--tree` at
all -- every converted chapter (01-09) is like this now, and the tree tiles of
11-21 are already classified in `obstacles_prompt.md`. Everything below is
kept for the case of a crown that should stay part of a tile; it is not the
normal path any more.

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

### Coloured forests

All six references are green, because chapter 01's forest is. Where a chapter
paints its trees in more than one colour, `--tree ID:REF#RRGGBB` retints the
crown's foliage to that colour and leaves the trunk brown; the shading survives,
only the hue changes. Sample the colour off the tile art rather than guessing,
and give every tile of one species the same tint so the forest reads as a
forest — chapter 05 uses three: green (untinted), `#2c4c6c` and `#a04824`.

Add `--dry-run` first to see the voxel counts, and `--force` to overwrite.

### Taller trees

The six references top out at z 38, which reads as roughly 0.6 of a tile. A
chapter that wants a taller forest passes `--tree-stretch F`: every crown's
layers are repeated so it ends up F times taller, keeping the conifer's steps
and the round tree's taper. The VOX canvas grows in Z to fit (40×40×45 at
1.4×), which the OBJ export does not care about — it centres on X/Y and grounds
on the lowest voxel, so nothing else shifts.

```bash
python shapes_to_vox.py 04 --used-tiles .../Chapter_04_UsedTiles.json     --grass-tile 20 --tree-stretch 1.4 --tree 131:42
```

Chapter 04 uses 1.4×. Past about 1.6× a conifer stops reading as a tree and
starts reading as a tower, so preview before committing to a bigger number.

### Varied forests

A forest painted from one tile id is one crown stamped a hundred times. Where
that reads badly -- chapter 09's bottom forest is 200 tiles of six ids --
give those tiles alternative crowns:

```bash
python shapes_to_vox.py 09 --used-tiles .../Chapter_09_UsedTiles.json --grass-tile 74     --tree-stretch 1.4 --tree "80:42#7a2410" ...     --variant 1.15,0.8 --variant 1.7,0.9 --variant 1.4,1.25 --variant-tiles 80,81,84,85,92,93
```

Each `--variant STRETCH,WIDTH` writes `Shape_NN_<id>_v<k>.vox` for every
`--variant-tiles` tile, with the crown stretched to STRETCH times its authored
height (replacing `--tree-stretch` for that model) and resampled to WIDTH times
its authored width, clipped to the tile. `ShapesLayer` finds the `_v1`, `_v2`,
... models beside the base one and picks one of base + variants per map
position (a hash of the position, so the board is the same every build). Three
variants on top of the base is enough to break the repetition; keep WIDTH
within about 0.8..1.3 so a conifer stays a conifer, and preview every variant
the way the base tiles are previewed below.

## Verify

Regression-check the rules against chapter 01 — this must stay clean:

```bash
python validate_shapes.py
# chapter 01: 96 tiles identical, 0 differ
```

Then eyeball a handful of the new tiles, especially every tree tile and any
tile with a shoreline:

```bash
python vox_preview.py ../../Resources/Remastered/Shapes/Shapes_NN/vox/Shape_<NN>_<id>.vox \
    -o out/ --views front,iso --scale 5
```

On a shoreline tile the `front` view should show a visible step down from land
to sand to water, not a flat slab.
