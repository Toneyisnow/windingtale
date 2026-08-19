# Formats and coordinate conventions

Everything here was verified against the finished chapter 01 assets. Do not
guess at any of it.

## Where things live

| What | Path |
|---|---|
| chapter data | `WindingTale2/Assets/Resources/Data/Chapters/Chapter_NN.json` |
| original map art | `Resources/Original/Maps/NN/Chapter-NN.png` (+ `-grid.png`, `-Cover.png`) |
| original tile art | `Resources/Original/Shapes/ShapePanelNN/Shape_<NN-1>_<id>.png` |
| remastered tiles | `Resources/Remastered/Shapes/Shapes_NN/{vox,obj}/Shape_<NN>_<id>.*` |
| remastered obstacles | `Resources/Remastered/Obstacles/{vox,obj}/<definition_key>.*` |
| what the game loads | `WindingTale2/Assets/Resources/Shapes/Shapes_NN/`, `.../Resources/Obstacles/` |

Obstacle models are **shared across chapters**, so they sit in one flat folder
keyed by `DefinitionKey` — not per chapter. Tiles are per chapter.

### File naming — the prefixes do not match the folder

| chapter | source tiles | remastered tiles |
|---|---|---|
| 01 | `ShapePanel01/Shape_0_<id>.png` | `Shapes_01/vox/Shape_1_<id>.vox` |
| 02 | `ShapePanel02/Shape_1_<id>.png` | `Shapes_02/vox/Shape_2_<id>.vox` |
| NN | `ShapePanelNN/Shape_<NN-1>_<id>.png` | `Shapes_NN/vox/Shape_<NN>_<id>.vox` |

The source prefix is the panel's **0-based** index and the remastered prefix is
the chapter's **1-based** index, so they are always one apart. `voxlib`'s
`tile_png_name()` / `shape_vox_name()` are the single source of truth.

## Chapter_NN.json

```jsonc
{
  "Index": 0,
  "Width": 24, "Height": 24,          // in tiles
  "Treasures": [...],
  "BackgroundMusic": {...},
  "DefaultTais": [...],
  "Obstacles": [                       // inserted by step 2, before ShapeMatrix
    { "Id": 1, "DefinitionKey": "dwelling_house_1", "Position": { "X": 10, "Y": 9 } }
  ],
  "ShapeMatrix":  [ [ ... ], ... ],    // the painted map    -- ShapeMatrix[x][y]
  "RenderMatrix": [ [ ... ], ... ],    // the cleaned map    -- inserted by step 5
  "Shapes": { "49": { "Type": 0, "bg": 6 }, ... }   // per-tile-id properties
}
```

`Shapes` keys the tile ids used by `ShapeMatrix`; `Type` is the movement class
and `bg` the background group. Step 2 must not drop entries for tiles that
survive the clean.

### The two maps

`ShapeMatrix` is the map as painted and is the **only** one the game logic
reads: `FDField.GetShapeAt` resolves it to a `ShapeDefinition`, whose
`ShapeType` decides move cost, the AP/DP terrain bonus, and whether a tile can
be entered at all. Lifting a house out into an obstacle must not change it —
the house blocks movement because its tiles are `Blocked` here.

`RenderMatrix` is the same map with every obstacle footprint cleared, and is
read only by `ShapesLayer` via `FDField.GetRenderShapeIdAt`, to choose the tile
model. Without it the ground would draw the painted building a second time,
underneath the obstacle model standing on it.

It is optional: `FDField` falls back to `ShapeMatrix` for chapters that have
not been converted, which is why those still render.

## Coordinates

`FDField.GetShapeAt` reads `shapes[X-1, Y-1]`, so:

```
ShapeMatrix[x][y]  ->  map X = x + 1   column, left -> right
                       map Y = y + 1   row,    top  -> bottom
```

The map PNG is `Width*24 × Height*24` and tile `(X, Y)` occupies pixels
`((X-1)*24, (Y-1)*24)`. Rebuilding the PNG from `ShapeMatrix` this way
reproduces `Chapter-NN.png` exactly, except where objects were painted on top.

### Tile PNG → shape VOX

```
tile PNG pixel (px, py)   py counted from the TOP
shape VOX voxel (x, y, z) x = px
                          y = 23 - py        <- VOX +Y is toward the top of the map
                          z = terrain level
```

Shape VOXs are authored on a **40×40×40** canvas with the tile in the
`0..23 × 0..23` corner. Terrain levels:

| terrain | z | colour |
|---|---|---|
| land | 23 | everything else |
| sand | 22 | the light-tan family, mainly `(204,153,102)` |
| water | 21 | `(51,102,153)` |
| deep water | 20 | `(51,51,153)` |

`(153,102,51)` is dirt, **not** sand — it stays at 23.

Dark green `(51,102,0)` is grass and gets 2 more voxels of the same colour
stacked above it.

### Obstacle VOX

```
SIZE = (cols * 24, rows * 24, height)
```

Same X/Y axes as a shape VOX. `Position` in the chapter JSON is the
**top-left tile** of the footprint (smallest map X and Y), 1-based, and may be
`<= 0` or run off the far edge when the object is only partly on screen — see
`barrel_group_1` at `X: -1` in chapter 01.

Note the reading order: in the obstacle prompts, "4 x 6 tiles" means
**4 rows deep × 6 columns wide**, i.e. `SIZE (144, 96, h)`.

Height is chosen from how tall the object looks in the art, in tile units —
a barrel stack that reads as 1.5 tiles high is 36 voxels.

## Colours

Both shape and obstacle VOXs use the MagicaVoxel default 256-colour palette.
Art colours map to the **nearest** palette entry over indices 1..255, ties
going to the lowest index. Index 256 (pure black) is never used, which is why
black art pixels land on 225. `voxlib.palette_index()` implements this.

## OBJ export

`Tools/Vox_to_Obj/vox_to_obj_exporter.py` with the defaults — `scale 0.1`,
centred, grounded, and **Z-up (no `--y-up`)**. `ShapesLayer` and
`ObstaclesLayer` stand the models up themselves with a parent `Euler(90)` plus
an inner `Euler(180)`, so a Y-up export would land the map on its side.
