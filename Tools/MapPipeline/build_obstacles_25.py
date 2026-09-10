"""Build what chapter 25 adds to Resources/Remastered/Obstacles/vox/:

    fire_pillar_4        1 x 1   the tall bright pillar painted over THREE rows
                                 (tiles 56 over 64 over 61), two frames
    lava_25_<tile>       1 x 1   a one-voxel sheet of molten lava over the lava
                                 pixels of tile <tile> (4..15 and 50), two frames

**fire_pillar_4** is chapter 10's ``fire_pillar_2`` -- the same white-hot
column painted over two rows (56 over 69, which is chapter 25's 61) -- with the
column one tile taller, so the model is 72 voxels high instead of 48. The dish,
the column radius and the two-frame flame are reused from build_obstacles_10.

**The lava sheets** are ground covers (ObstaclesLayer.IsGroundCover): the tile
under them stays painted -- and glows, ShapeDefinition.Glow -- and the sheet
lies on it, one voxel thick, at full tile size, self-lit, and flickering at the
fire pillars' rate. One model per lava tile shape, its footprint the white
pixels of that tile's art, so the sheets of neighbouring tiles meet at the tile
edges the way the painted lava does. Colour is a slow ripple of yellow and gold
veins and a few orange embers on the white-hot lava, and the second frame is the
same ripple a little further along, so the two frames flicker like the fire
rather than blink between two unrelated pictures. Both frames share the same
footprint (the mask), which is what keeps their bounds identical.

Model space is the obstacle convention (formats.md): x runs left to right along
the map, y = 0 is the map row nearest the camera and y = 23 the back, z is up --
a tile PNG's row py lands on y = 23 - py, as in shapes_to_vox.
"""

import math
import os

import numpy as np
from PIL import Image

import voxlib
import build_obstacles_10 as b10

TILE = voxlib.TILE

CHAPTER = 25

# -------------------------------------------------------------------------
# fire_pillar_4
# -------------------------------------------------------------------------

TALL_COLUMN_H = b10.COLUMN_H + TILE      # one painted tile taller than fire_pillar_2
TALL_PILLAR_H = b10.PILLAR_H + TILE      # 72


def fire_pillar_4(frame):
    """The tall bright pillar: fire_pillar_2's column, one tile higher."""
    g = np.zeros((TILE, TILE, TALL_PILLAR_H), dtype=np.uint8)
    b10.dish(g)
    b10.column(g, b10.DISH_TOP, b10.DISH_TOP + TALL_COLUMN_H, b10.WHITE, b10.WHITE, streak=b10.LIGHT_YELLOW)
    b10.disc(g, b10.DISH_TOP, b10.DISH_TOP + 2, b10.COLUMN_R, b10.YELLOW)
    b10.flame(g, b10.DISH_TOP + TALL_COLUMN_H, frame, b10.BRIGHT_RAMP)
    return g


# -------------------------------------------------------------------------
# lava sheets
# -------------------------------------------------------------------------

LAVA_TILES = list(range(4, 16)) + [50]      # the tiles with white lava on them
LAVA_RGB = (252, 252, 252)                  # the art's lava, pure white

# The sheet's colours: the art's own fire ramp. Mostly white-hot, with veins of
# the yellows and the odd deep-orange ember.
LAVA_WHITE = b10.WHITE
LAVA_YELLOW = b10.YELLOW
LAVA_GOLD = b10.GOLD
LAVA_ORANGE = b10.ORANGE2

# How far the ripple moves between the frames (radians); the frames are the
# same pattern at two phases, so the veins seem to drift rather than jump.
FRAME_PHASE = 1.9


def lava_mask(tile_id):
    """(x, y) of every lava pixel of the tile, in model space."""
    path = os.path.join(voxlib.shape_panel_dir(voxlib.workspace_root(), '%02d' % CHAPTER),
                        voxlib.tile_png_name('%02d' % CHAPTER, tile_id))
    px = Image.open(path).convert('RGB').load()
    cells = []
    for py in range(TILE):
        for pxi in range(TILE):
            if tuple(px[pxi, py][:3]) == LAVA_RGB:
                cells.append((pxi, TILE - 1 - py))
    return cells


def ripple(x, y, seed, phase):
    """A smooth, deterministic swirl in about -1.5..1.5: three sine waves at
    different angles and wavelengths, offset per tile so no two sheets repeat."""
    s = seed * 0.37
    return (math.sin(x * 0.55 + y * 0.35 + s + phase)
            + 0.7 * math.cos(x * 0.25 - y * 0.6 + 2.0 * s - 0.6 * phase)
            + 0.5 * math.sin((x + y) * 0.9 + 3.0 * s + 1.3 * phase))


def lava_colour(x, y, tile_id, frame):
    v = ripple(x, y, tile_id, frame * FRAME_PHASE)
    if v > 1.25:
        return LAVA_ORANGE        # an ember
    if v > 0.75:
        return LAVA_GOLD          # the heart of a vein
    if v > 0.35:
        return LAVA_YELLOW        # the vein's edge
    return LAVA_WHITE


def lava_sheet(tile_id, frame):
    g = np.zeros((TILE, TILE, 1), dtype=np.uint8)
    for x, y in lava_mask(tile_id):
        g[x, y, 0] = lava_colour(x, y, tile_id, frame)
    return g


def lava_key(tile_id):
    return 'lava_%d_%d' % (CHAPTER, tile_id)


# -------------------------------------------------------------------------

def builders():
    out = {'fire_pillar_4': fire_pillar_4}
    for t in LAVA_TILES:
        out[lava_key(t)] = (lambda frame, t=t: lava_sheet(t, frame))
    return out


def main():
    root = voxlib.workspace_root()
    out_dir = voxlib.obstacles_vox_dir(root)
    if not os.path.isdir(out_dir):
        os.makedirs(out_dir)
    for name, build in sorted(builders().items()):
        extents = []
        for frame in range(b10.FRAMES):
            g = build(frame)
            xs, ys, zs = np.nonzero(g)
            voxels = list(zip(xs.tolist(), ys.tolist(), zs.tolist(), g[xs, ys, zs].tolist()))
            path = os.path.join(out_dir, b10.frame_name(name, frame) + '.vox')
            voxlib.write_vox(path, g.shape, voxels)
            extents.append((xs.min(), xs.max(), ys.min(), ys.max()))
            print('%-18s SIZE %3dx%3dx%-4d  x %d..%d y %d..%d z ..%d  %6d voxels  -> %s'
                  % (b10.frame_name(name, frame), g.shape[0], g.shape[1], g.shape[2],
                     xs.min(), xs.max(), ys.min(), ys.max(), zs.max(), len(voxels), path))
        if len(set(extents)) != 1:
            raise SystemExit('%s: the frames differ in X/Y extent %r' % (name, extents))


if __name__ == '__main__':
    main()
