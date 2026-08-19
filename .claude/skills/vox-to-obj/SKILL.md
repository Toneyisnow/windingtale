---
name: vox-to-obj
description: Batch-convert a folder of MagicaVoxel .vox files into .obj + .mtl + palette .png in the sibling obj/ folder, using the export settings WindingTale2 expects. Use when asked to export VOX models to OBJ, refresh the obj folder for obstacles or a chapter's shapes, or import remastered models into Unity.
---

# Convert VOX models to OBJ

Wraps `Tools/Vox_to_Obj/vox_to_obj_exporter.py`, which writes its three output
files next to the source `.vox`. This project keeps them apart:

```
<something>/vox/foo.vox   ->   <something>/obj/foo.obj + foo.mtl + foo.png
```

so the wrapper exports and moves.

## Run it

```bash
cd Tools/MapPipeline

python vox_batch_to_obj.py --obstacles      # Resources/Remastered/Obstacles/vox -> ../obj
python vox_batch_to_obj.py --shapes NN      # Resources/Remastered/Shapes/Shapes_NN/vox -> ../obj
python vox_batch_to_obj.py --in <dir>/vox --out <dir>/obj
```

Models that already have an `.obj` are skipped; pass `--force` to re-export,
`--dry-run` to just list.

## Export settings — do not change these

`scale 0.1`, centred, grounded, and **Z-up (no `--y-up`)**.

`ShapesLayer` and `ObstaclesLayer` stand each model up themselves with a parent
`Euler(90)` plus an inner `Euler(180)`, and `ShapesLayer` relies on the mesh
origin already sitting at the tile centre. Exporting Y-up lays the whole map on
its side; exporting un-centred shifts every tile by half a map.

The three files travel together — the `.mtl` references the `.png` by bare
filename, so never move just the `.obj`.

## Getting them into the game

Unity loads from `WindingTale2/Assets/Resources/`:

| source | destination |
|---|---|
| `Resources/Remastered/Obstacles/obj/*` | `WindingTale2/Assets/Resources/Obstacles/` |
| `Resources/Remastered/Shapes/Shapes_NN/obj/*` | `WindingTale2/Assets/Resources/Shapes/Shapes_NN/` |

Copy all three files per model. Unity regenerates the `.meta` files on next
focus; leave existing ones alone so GUIDs survive.
