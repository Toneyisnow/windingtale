---
name: chapter-schema
description: Step 1 of the 2D-to-3D map workflow — read a WindingTale chapter's map data and confirm you can rebuild its 2D map image from the tile PNGs. Use when asked to understand a chapter's schema, ShapeMatrix, tile ids, or how a chapter map PNG is assembled from ShapePanel tiles.
---

# Step 1: understand the chapter schema

Produces no files. The point is to know, before touching anything, exactly how
this chapter's map is put together — and to *prove* it by repainting the map
from its tiles and diffing against the original artwork.

## Inputs

- `WindingTale2/Assets/Resources/Data/Chapters/Chapter_NN.json`
- `Resources/Original/Maps/NN/Chapter-NN.png` and `Chapter-NN-grid.png`
- `Resources/Original/Shapes/ShapePanelNN/Shape_0_*.png`

Coordinate conventions are in `map-2d-to-3d/references/formats.md`. Read it
first if you have not.

## Procedure

```bash
cd Tools/MapPipeline
python chapter_map.py info NN
```

That reports width/height in tiles, the tile-id histogram, which tile is the
ordinary background one, whether any tile id used by `ShapeMatrix` is missing
from `Shapes` or from the ShapePanel folder, and any existing obstacles.

Then prove the assembly rule:

```bash
python chapter_map.py verify NN
```

`verify` repaints the map from `ShapeMatrix` + the tile PNGs and prints a
per-tile diff grid against `Chapter-NN.png`.

**Read the result carefully — it is the most informative artefact in the whole
workflow:**

- clusters of `#` → those tiles were overpainted, i.e. that is where the
  obstacles are. Step 2 starts from exactly this picture. This is chapter 01.
- **all `.`** → the tile grid fully explains the artwork, which means the
  buildings are *drawn as ordinary tiles* rather than painted over them. This
  is chapter 02, and it is common. The diff cannot locate the obstacles for
  you; step 2 has to find them from the art. Say so explicitly when you hand
  over, so step 2 does not go looking for a signal that is not there.
- a `#` everywhere → something is wrong. Most likely the ShapePanel folder is
  not the one this chapter uses, or you are diffing against a `-grid.png`.

Finally, look at the map:

```bash
python chapter_map.py render NN --grid --labels -o out/chNN_grid.png
```

and open both that and `Chapter-NN-grid.png` with the Read tool. You need the
actual visual, not just numbers — step 2 depends on recognising objects.

## What you should be able to state when done

1. map size in tiles, and in pixels
2. the background tile id (what cleared ground should become)
3. how many distinct tiles the chapter uses, and whether all their PNGs exist
4. the tile rectangles where the artwork disagrees with the tile grid — the
   obstacle candidates, listed as `X1..X2, Y1..Y2`

Hand those four things to `chapter-obstacles`.
