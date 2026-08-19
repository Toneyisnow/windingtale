---
name: map-2d-to-3d
description: Convert one chapter's 2D tile map into 3D voxel assets for WindingTale2 — read the chapter schema, lift the painted obstacles out into standalone VOX models, clean the tile matrix, generate shape VOXs, and export OBJs. Use when asked to "把第NN关的地图从2D转成3D", "convert chapter NN map to 3D", remaster a chapter's map, or rebuild a chapter's shapes/obstacles.
---

# Workflow: convert chapter NN's map from 2D to 3D

The original game draws each battlefield as a grid of 24×24-pixel tiles, with
buildings, huts and props **painted into those tiles**. The remaster stands
everything up in voxels: tiles become 40³ shape models, and each painted
building becomes a separate obstacle model placed on top of clean ground.

Chapter 01 is already finished and is the reference for everything below. Do
not change it — regenerate against it to check yourself.

## Before starting

Read `references/formats.md` once. It has the coordinate conventions, and
getting those wrong silently produces a mirrored map.

Tools live in `Tools/MapPipeline/` (see its README). Run them from that folder:

```bash
cd Tools/MapPipeline
```

`references/obstacles_prompt.md` holds the per-chapter description of what
objects are on each map, and the shared obstacle ids. **The same object across
chapters must reuse the same `DefinitionKey`** — that is the whole point of the
id table there.

## Steps

Run these in order. Each has its own skill with the detail; invoke it rather
than improvising.

| # | Skill | Produces |
|---|---|---|
| 1 | `chapter-schema` | understanding only — dimensions, tile ids, how the map PNG is assembled |
| 2 | `chapter-obstacles` | `Chapter_NN_Cleaned.json`, obstacle VOX models, the obstacle list, used tile ids |
| 3 | `chapter-shape-vox` | `Resources/Remastered/Shapes/Shapes_NN/vox/Shape_<NN>_*.vox` |
| 4 | `vox-to-obj` | `obj/` next to each `vox/` folder |
| 5 | — (below) | `Chapter_NN.json` carrying both the painted and the cleaned map |

### Step 1 — understand the chapter schema

Invoke `chapter-schema` with the chapter number. Output is knowledge, not
files: map size, which tile ids are used, which tile is ordinary ground, and a
confirmed match between the rebuilt tile grid and the original artwork.

### Step 2 — build the cleaned map and the obstacles

Invoke `chapter-obstacles`. This is the step with real judgement in it: you
look at the artwork, find the objects listed for this chapter in
`references/obstacles_prompt.md`, measure their footprints in tiles, model them
as VOX, and punch them out of the tile matrix.

### Step 3 — generate the shape VOXs

Invoke `chapter-shape-vox`. Mostly mechanical, except for deciding which tiles
carry trees and which reference crown fits.

### Step 4 — export OBJ

Invoke `vox-to-obj` for both `Resources/Remastered/Obstacles/vox` and
`Resources/Remastered/Shapes/Shapes_NN/vox`, then copy the results into
`WindingTale2/Assets/Resources/`.

### Step 5 — install the cleaned map

**The conversion is not finished until this happens.** Steps 2–4 only ever
write `Chapter_NN_Cleaned.json`; the game reads `Chapter_NN.json`.

The finished chapter carries **both** maps, and they are not interchangeable:

| key | which map | who reads it |
|---|---|---|
| `ShapeMatrix` | the **painted** one, unchanged | the battle — `ShapeType` gives move cost, AP/DP, whether a tile blocks |
| `RenderMatrix` | the **cleaned** one from step 2 | `ShapesLayer` only, to pick which tile model to draw |

Do not overwrite `ShapeMatrix` with the cleaned map. Clearing a footprint turns
the tiles a house stood on into plain grass, and if that reaches `ShapeMatrix`
the house becomes walkable — chapter 02's cleaned map has **zero** `Blocked`
tiles. The building blocks movement because the painted tile under it still
says so; the obstacle model is scenery.

```bash
cd Tools/MapPipeline
python install_chapter.py NN --dry-run
python install_chapter.py NN
```

It takes `ShapeMatrix` from the current `Chapter_NN.json` (still the painted
map at that point), everything else — including `Obstacles` — from
`Chapter_NN_Cleaned.json`, and writes the cleaned map in as `RenderMatrix`.
Re-running is safe.

It also refuses to install unless every `RenderMatrix` tile id has an OBJ,
every `ShapeMatrix` id has a `Shapes` entry, and every obstacle has a model —
worth having, because `ShapesLayer` silently skips a tile whose model is
missing, leaving a hole in the board rather than an error.

Chapter 01 predates the pipeline: its `Chapter_01.json` was already the cleaned
map and the painted original survives as `ChapterLegacy_01.txt`, so it was
installed with both sides named explicitly:

```bash
python install_chapter.py 01 \
    --original ../../WindingTale2/Assets/Resources/Data/Chapters/ChapterLegacy_01.txt \
    --cleaned  ../../WindingTale2/Assets/Resources/Data/Chapters/Chapter_01.json
```

## Finishing

After step 4, report:

- map size, obstacle count, tile count
- where every file landed
- anything you were unsure about (a footprint you had to guess, a tree you were
  not certain about) — say so plainly rather than presenting it as settled

## How the game picks the tile set

`ShapesLayer.cs` builds the path from `FDField.ChapterId`
(`Shapes/Shapes_NN/Shape_N_{id}`), so a chapter picks up its own models with no
code change. A chapter with no `Shapes_NN` folder falls back to chapter 01's
tiles and logs a warning once — that fallback is what makes an unconverted
chapter still render, and seeing chapter 01's art on chapter NN means either
step 5 was skipped or the OBJs never reached `Assets/Resources/Shapes/Shapes_NN/`.
