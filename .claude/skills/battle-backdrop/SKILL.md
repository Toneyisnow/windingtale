---
name: battle-backdrop
description: Build a chapter's battle-scene backdrop BG_NN — a 256×180×90 VOX composed from that chapter's own tiles and obstacle models, previewed from the battle camera, exported to OBJ and installed under WindingTale2/Assets/Resources/BG/NN. Use when asked to generate/make/redo the 战斗背景 BG_xx for a chapter, a backdrop for chapter NN, or a range of chapters' battle backgrounds.
---

# Battle backdrop BG_NN

The fight scene shows the two fighters in front of a voxel landscape. Each
chapter gets its own, built from the chapter's remastered assets so the battle
happens in front of scenery the player just walked through. Read the memory
notes `battle-backdrop-geometry` and `vox-file-format` first if you have them.

Everything lives in `Tools/Vox_Generator/chapter_bg_to_vox.py`: one
`recipe_NN(bd)` per chapter, listed in `RECIPES`. **Read the recipes nearest
to what you are building before writing a new one** — the helpers and their
docstrings are the real manual.

## Ground rules

- Model space: +X right in the file but **screen LEFT** (the frame is mirrored),
  +Y away from the camera, +Z up. Camera at (109, 0, 62), 23° down.
- Keep `FIGHTER_ZONE` (x 95..170, y 20..150) to grass-height things only:
  pass it to `scatter_trees(avoid=...)`. Centre axial things (roads, stairs,
  causeways) on x ≈ 116, not 128.
- Flat things (water, paving) must come **forward** (y < ~120) to be seen.
- A narrow way (causeway, bridge, paved road) laid straight up the axis leaves
  one fighter over the water/drop beside it. Lay such a recipe out in a
  `Turn` frame (45°, pivot = the fighters' ground point): the way then crosses
  the frame from the near screen-right corner to the far screen-left and both
  fighters stand on it. Chapters 17, 18, 22, 23, 26 and 29 do this.
- A backdrop is a few of the chapter's things placed where they read, not the
  map rebuilt. One landmark behind the fight, framing at the edges.
- Only tiles with a `Shapes_NN` VOX can be laid (tiles under trees were never
  generated). Pick ids with the helper:

  ```bash
  python .claude/skills/battle-backdrop/scripts/tile_ids.py NN              # usable ids + colours
  python .claude/skills/battle-backdrop/scripts/tile_ids.py NN 0 0 40 30    # id grid of a window
  ```

  Look at `Resources/Original/Maps/NN/Chapter-NN.png` (downscale it) to see
  what the chapter is, then read ids off the grid where you saw each thing.
  High `sd` = patterned tile; mixing several flat-but-different tiles at
  random gives a checkerboard, so prefer one or two similar ids per surface.
- Obstacle keys for the chapter: `Tools/MapPipeline/obstacles/obstacles_NN*.json`
  (`DefinitionKey`); the models are in `Resources/Remastered/Obstacles/vox`.
  `obstacle(key, 2)` is half again as big as the default scale 3.
- Indoors / underground / void chapters: the battle sky is always daylight, so
  wall the picture in with `enclose(bd, VOID or rock tones, back=..., sides=...)`.

## Toolbox (all in chapter_bg_to_vox.py)

| need | helper |
|---|---|
| rolling ground, far rise | `bd.undulate`, `bd.rise` |
| flat level / step / terrace | `bd.terrace`, `raise_where`, `step_up` (terrace + stair lane) |
| floor tiles | `bd.lay_ground(tiles_fn, NN)`, `_pick(ids, seed)` |
| curved patches (ponds, paths, beds) | `_blob`, `_patchy`, `lay_where(bd, fn, NN, mask)` |
| cliff / wall faces | `rock_faces(bd, NN, tile, min_rise)` |
| water / pits one colour all the way down | `sink_colour(bd, mask)` |
| close the frame (no sky) | `enclose` |
| path on the slant (both fighters on it) | `turn = Turn(bd)`; masks from `turn.u/v`, `turn.stamp`, `scatter_turned`, `railing_turned` |
| trees / props | `scatter_trees(..., where=fn)`, `bd.stamp`, `building`, `avenue`, `pillar_rows` |
| built geometry | `railing`, `fence`, `crates`, `log_pile`, `well`, `stump`, `cave_mouth`, `light_beam`, lava `_lava_paint` / `lava_pool` |

Numpy gotcha: `_grid(bd)` returns broadcastable (256,1)/(1,180) arrays. A
mask built from only one of them must be broadcast before boolean indexing —
use `xs, ys = np.broadcast_arrays(*_grid(bd))` when in doubt. Ground heights
must stay below 90 (`ENCLOSE_TOP - 4` for hills).

## Procedure

1. Write `recipe_NN`, a header comment saying what the map is and what the
   backdrop picks, and add it to `RECIPES` and to the module docstring list.
2. Build and look at it from the battle camera — the only reliable judge:

   ```bash
   cd Tools/Vox_Generator
   python chapter_bg_to_vox.py NN
   python bg_preview.py ../../Resources/Remastered/BG/BG_NN.vox -o <scratch>/prev
   ```

   Read the PNG. Thin light-blue lines in it are splat pinholes in the
   preview, not holes in the model. Iterate until the fighters' boxes sit on
   clear ground with the landmark behind them and no sky where there should
   be none.
3. Export (bare defaults — scale 0.1, centred, grounded, Z-up) and install:

   ```bash
   cd Resources/Remastered/BG
   python ../../../Tools/Vox_to_Obj/vox_to_obj_exporter.py BG_NN.vox
   mkdir -p ../../../WindingTale2/Assets/Resources/BG/NN
   cp BG_NN.obj BG_NN.mtl BG_NN.png ../../../WindingTale2/Assets/Resources/BG/NN/
   ```

   `BattleLoader.LoadBackdrop` picks up `BG/NN/BG_NN` by chapter number; no
   C# change. `.meta` files are gitignored and regenerated by Unity.
4. Say plainly that it is unverified in the editor until someone runs a
   battle in that chapter.
