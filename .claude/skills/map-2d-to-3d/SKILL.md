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
| 3 | `chapter-shape-vox` | `Resources/Remastered/Shapes/Shapes_NN/vox/Shape_1_*.vox` |
| 4 | `vox-to-obj` | `obj/` next to each `vox/` folder |

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

## Finishing

After step 4, report:

- map size, obstacle count, tile count
- where every file landed
- anything you were unsure about (a footprint you had to guess, a tree you were
  not certain about) — say so plainly rather than presenting it as settled

## Known gap in the game side

`ShapesLayer.cs` still hard-codes `Shapes/Shapes_01/Shape_1_{id}`, so a chapter
whose shapes live in `Shapes_02` will not load them until that path is made
per-chapter. Point this out when you finish a chapter other than 01; do not
silently change it as part of an asset conversion.
