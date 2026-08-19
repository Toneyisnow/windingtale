---
name: chapter-obstacles
description: Step 2 of the 2D-to-3D map workflow — find the buildings and props painted into a WindingTale chapter's tile map, build a VOX model for each, and punch their footprints out of the ShapeMatrix into Chapter_NN_Cleaned.json. Use when asked to extract obstacles from a chapter map, build obstacle VOX models, or clean a chapter's tile matrix.
---

# Step 2: build the cleaned map and the obstacle models

The houses, huts and barrels are painted into the 24×24 tiles. This step lifts
them out: each becomes a standalone VOX model placed by tile coordinate, and
the tiles they covered go back to plain ground.

This is the judgement-heavy step. The tools verify and apply; **you** decide
what is on the map.

## Inputs

- the four facts from `chapter-schema` (map size, background tile, tile ids,
  overpainted rectangles)
- `map-2d-to-3d/references/obstacles_prompt.md` — what objects this chapter has
  and which `DefinitionKey` each must use
- `Resources/Original/Maps/NN/Chapter-NN.png`

## Outputs

- `Chapter_NN_Cleaned.json` — chapter with footprints cleared and an
  `Obstacles` array inserted immediately before `ShapeMatrix`. This file is a
  **rendering** map, not a replacement chapter: step 5 installs its matrix as
  `RenderMatrix` and leaves the painted `ShapeMatrix` alone, because that is
  what the battle reads terrain from. Never copy it over `Chapter_NN.json`.
- `Chapter_NN_UsedTiles.json` — the tile ids that survive, for step 3
- `Resources/Remastered/Obstacles/vox/<definition_key>.vox` for each **new**
  object

## 1. Identify the objects

The chapter's prompt entry says what to look for and how many. Cross-check it
against the `#` clusters from `chapter_map.py verify` — the prompt says *what*,
the diff says *where*.

When `verify` came back all `.` the buildings are made of ordinary tiles and
the diff tells you nothing. Two techniques replace it, and both are worth
running because they give exact tile boundaries instead of eyeballed ones:

- **classify the tiles.** Score each distinct tile id for roof / wall / wood
  colours and print the map as one character per tile. Building blocks fall
  straight out as solid rectangles, and afterwards you can assert that every
  building-coloured tile lands inside some footprint — a miss means an object
  you have not accounted for.
- **template-match against a finished chapter.** Crop a known object out of
  `Chapter-01.png` and slide it over this chapter's map at 24-pixel steps. A
  100% hit means the same art, so the same `DefinitionKey`, and it hands you
  the exact `Position`. This is how chapter 02's barrels and hut were
  identified rather than guessed.

Repeated buildings are worth finding the same way: align a candidate rectangle
of tile ids against the rest of the map and look for a near-perfect offset. In
chapter 02 that proved the top-right and bottom-left churches are one model at
offset (−20, +12), which also revealed that the top-right one is cut off by a
row and so sits at `Y: 0`.

Look at each candidate close up:

```bash
cd Tools/MapPipeline
python chapter_map.py crop NN --tiles X1,Y1,X2,Y2 --scale 8 --grid -o out/cand.png
```

then open `out/cand.png` with the Read tool. Do this for every object. Do not
place an obstacle you have not looked at.

Rules for the footprint:

- measured in whole tiles, **including the object's drop shadow**
- `Position` is the top-left tile, 1-based
- an object half off the edge keeps its true footprint and gets a `Position`
  that runs off the map (chapter 01 has a barrel group at `X: -1`)
- footprints must not overlap; `map_clean.py` warns if they do

Reuse an existing `DefinitionKey` whenever the object is the same shape as one
already in `Resources/Remastered/Obstacles/vox/`. Only genuinely new shapes get
a new key. Names are lower-case letters, digits and underscores.

## 2. Model each new object

The VOX must be exactly `SIZE (cols*24, rows*24, height)` — the footprint in
tiles times 24. The model does **not** have to fill that box; empty margin is
normal and is how the object gets positioned inside its footprint.

Height comes from how tall the object reads in the art, in tile units: a barrel
stack that looks 1.5 tiles high is `1.5 * 24 = 36` voxels.

That reading is a starting point, not the answer, for anything tall. The 2D art
draws a church in elevation, so taking its height literally gives a model that
stands up on the board as a tower — chapter 02's churches were built that way
and then cut down to roughly a third. `build_obstacles_02.py` keeps the
art-derived numbers and squashes them in one place (`BODY_SCALE` for everything
below the eaves, `ROOF_SCALE` for the pitch), which is worth copying: the ratio
is the thing you will want to retune after seeing it in the game, and doing it
this way leaves the footprints — and so the cleaned map — untouched.

Existing models to match in style and scale:

| key | SIZE | footprint |
|---|---|---|
| `dwelling_house_1` | 144 × 96 × 120 | 4 rows × 6 cols |
| `thatched_hut_1` | 72 × 96 × 84 | 4 rows × 3 cols |
| `barrel_group_1` | 72 × 48 × 36 | 2 rows × 3 cols |

Write a small generator script that builds the voxels and calls
`voxlib.write_vox()` — `build_obstacles_02.py` is a worked example, building
its churches out of gabled wings. **Pack the interiors solid** before writing:
the OBJ exporter emits a face wherever a voxel meets empty space, so a hollow
shell pays for its inside surface too and comes out several times larger than
it should (see `Model.solidify`).

Then look at what you made:

```bash
python vox_preview.py ../../Resources/Remastered/Obstacles/vox/<key>.vox --scale 4
```

and open the preview PNG with the Read tool. A model that is the right size and
the wrong shape passes every automated check — the preview is the only thing
that catches it.

## 3. Clean the matrix

Write the obstacle list:

```json
{
  "Obstacles": [
    { "Id": 1, "DefinitionKey": "blue_house_1", "Position": { "X": 3, "Y": 3 } }
  ]
}
```

`Size` is optional — it is read from the VOX when omitted, which is the better
path because it cross-checks the model against the footprint you measured.
Supply `"Size": {"Cols": C, "Rows": R}` only when modelling the object later.

```bash
python map_clean.py NN --obstacles out/obstacles_NN.json --dry-run   # check first
python map_clean.py NN --obstacles out/obstacles_NN.json
```

By default each cleared tile takes its nearest surviving neighbour, so a church
standing on a cobbled plaza leaves cobbles and a hut standing on grass leaves
grass. Pass `--fill <id>` to paint one tile everywhere instead — right for a
chapter with a single kind of ground, wrong for chapter 02.

Overlap warnings are not always errors. The 2D art draws objects over each
other, so chapter 02's left barrel group genuinely starts one tile inside the
church next to it. Read each warning and decide; both tiles clear to ground
either way.

But an overlap the art does not show means a footprint is in the wrong place.
Chapter 02's `thatched_hut_1` sat at `Y: 17` for a while and overlapped the
barrels stacked above it in three tiles; the art has a clear gap there. It was
one row too high — the roof apex is at Y 18 and the wall base at Y 21 — which
also left the hut's two painted wall tiles (184, 185) lying on the ground south
of the 3D model. Moving it to `Y: 18` removed both the phantom overlap and the
stray tiles. Treat a warning you cannot see in the crop as a placement bug, not
as noise.

Keep an eye on the "no longer used" tile list: a chapter that is mostly
buildings loses most of its tile ids here, which is expected, but a tile that
disappears from a chapter you thought was mostly open ground means a footprint
is too big.

## 4. Verify

```bash
python chapter_map.py verify NN --chapter-json <path>/Chapter_NN_Cleaned.json
```

The `#` clusters should now be exactly the obstacle footprints and nothing
else. Anything left over is an object you missed.

`map_clean.py` is deterministic — same map plus same obstacle list gives the
same output, and the order the obstacles are listed in does not matter. So the
cleaned map can be re-derived from the chapter at any time, and that round trip
is the check that the two have not drifted apart:

```bash
# obstacle list back out of the installed chapter, then clean again
python map_clean.py NN --obstacles <that list> -o /tmp/repro.json
# /tmp/repro.json's ShapeMatrix must equal Chapter_NN_Cleaned.json's
```

A mismatch means the obstacle list was edited after the cleaned map was
generated and the cleaned map was never regenerated — chapter 02 drifted by 5
tiles exactly that way.

Report which tile ids stopped being used and which appeared — a large jump
usually means the fill tile was a poor choice.
