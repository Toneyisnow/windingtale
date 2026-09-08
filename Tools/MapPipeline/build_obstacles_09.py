"""Build the two models chapter 09 adds.

Run once to produce, in Resources/Remastered/Obstacles/vox/:

    stone_pillar_1    1 col  x 1 row    the plain pillar: a stone ball on the pedestal
    notice_board_1    5 cols x 1 row    the royal notice board in the middle of the map

The helmed knights along the roads are chapter 08's ``stone_statue_1`` -- the
art is pixel-identical (chapter 09 tiles 76 + 78 are chapter 08 tiles 8 + 10),
so that model is reused and not built here.

**The plain pillar is the statue's pedestal with a ball on it.** The art draws
the same two-tile pedestal under both, so ``stone_pillar_1`` imports the
pedestal from ``build_obstacles_08`` and only adds the sphere, at the same
height as the knight's shoulders so the two kinds line up along a road.

**The board is a vertical slab standing on one row.** The art paints it over
four rows -- a light frame around a dark, text-covered face on rows 13-14, the
plinth on row 15 (the only Blocked row) and a thin foot on row 16 -- because a
notice board is drawn in elevation. The model is 5 cols x 1 row and stands on
row 15: a plinth the full width of the footprint, two posts, and a slab whose
front face is the dark board with lighter dotted lines standing in for the
text. Height 60 keeps it at the scale the statues were cut down to (a 40-voxel
statue for a two-row painting).

Model space is the obstacle convention (formats.md): x runs left to right along
the map, y = 0 is the map row nearest the camera (the front of the footprint)
and y = 23 the back, z is up.
"""

import os

import numpy as np

import voxlib
from build_obstacles_08 import STATUE, pedestal

TILE = voxlib.TILE


def P(rgb):
    return voxlib.palette_index(rgb)


# sampled from the chapter 09 tiles; the same grey-teal stone as the statues
BOARD_FRAME = (P((116, 136, 136)), P((84, 104, 104)), P((44, 64, 64)))
BOARD_FACE = P((24, 40, 40))
BOARD_TEXT = P((56, 80, 80))
BOARD_SHADOW = P((16, 28, 28))


# --------------------------------------------------------------------------
# the plain pillar
# --------------------------------------------------------------------------

PILLAR_H = 40


def stone_pillar_1():
    """The knight's pedestal with a stone ball on top instead of a bust."""
    light, mid, dark, deep = STATUE
    g = pedestal(np.zeros((TILE, TILE, PILLAR_H), dtype=np.uint8))
    g[9:15, 9:15, 14:17] = mid            # a short neck the ball sits on
    xs = np.arange(TILE)[:, None, None]
    ys = np.arange(TILE)[None, :, None]
    zs = np.arange(PILLAR_H)[None, None, :]
    ball = (xs - 11.5) ** 2 + (ys - 11.5) ** 2 + (zs - 23) ** 2 <= 7.0 ** 2
    g[ball] = light
    g[ball & (zs < 21) & (ys >= 12)] = mid          # the shaded underside, toward the back
    g[ball & (zs < 19) & (ys >= 14)] = dark
    return g


# --------------------------------------------------------------------------
# the notice board
# --------------------------------------------------------------------------

BOARD_COLS, BOARD_H = 5, 60
PLINTH_H = 10
POST_W = 6
SLAB_Y0, SLAB_Y1 = 8, 16          # slab thickness, front to back
SLAB_Z0, SLAB_Z1 = PLINTH_H, 56
FRAME = 4


def notice_board_1():
    light, mid, dark = BOARD_FRAME
    W = BOARD_COLS * TILE
    g = np.zeros((W, TILE, BOARD_H), dtype=np.uint8)

    # plinth: the full footprint, stepped once
    g[2:W - 2, 2:22, 0:PLINTH_H - 3] = mid
    g[4:W - 4, 4:20, PLINTH_H - 3:PLINTH_H] = light
    g[2:W - 2, 2:3, 0:PLINTH_H - 3] = dark          # the front lip in shadow

    # two posts, either end
    for x0 in (6, W - 6 - POST_W):
        g[x0:x0 + POST_W, SLAB_Y0 - 1:SLAB_Y1 + 1, PLINTH_H:SLAB_Z1 + 2] = mid
        g[x0:x0 + POST_W, SLAB_Y0 - 1:SLAB_Y0, PLINTH_H:SLAB_Z1 + 2] = light   # lit front edge

    # the slab, framed
    g[6:W - 6, SLAB_Y0:SLAB_Y1, SLAB_Z0:SLAB_Z1] = mid
    g[6:W - 6, SLAB_Y0:SLAB_Y0 + 1, SLAB_Z0:SLAB_Z1] = light           # frame front face
    g[6:W - 6, SLAB_Y0:SLAB_Y1, SLAB_Z1 - 2:SLAB_Z1] = light           # top rail
    g[6:W - 6, SLAB_Y1 - 1:SLAB_Y1, SLAB_Z0:SLAB_Z1] = dark            # the back is in shadow

    # the dark face, recessed one voxel into the frame, with the text lines
    fx0, fx1 = 6 + POST_W + FRAME, W - 6 - POST_W - FRAME
    fz0, fz1 = SLAB_Z0 + FRAME + 2, SLAB_Z1 - FRAME - 2
    g[fx0:fx1, SLAB_Y0:SLAB_Y0 + 1, fz0:fz1] = 0
    g[fx0:fx1, SLAB_Y0 + 1:SLAB_Y0 + 2, fz0:fz1] = BOARD_FACE
    g[fx0:fx1, SLAB_Y0 + 1:SLAB_Y0 + 2, fz1 - 1:fz1] = BOARD_SHADOW    # the frame's shadow on the face
    for z in range(fz1 - 6, fz0 + 2, -5):                              # rows of "writing"
        for x in range(fx0 + 3, fx1 - 3, 3):
            if (x // 3 + z) % 4 != 0:                                   # word gaps
                g[x:x + 2, SLAB_Y0 + 1:SLAB_Y0 + 2, z:z + 2] = BOARD_TEXT

    # a cap along the top
    g[4:W - 4, SLAB_Y0 - 2:SLAB_Y1 + 2, SLAB_Z1 + 2:SLAB_Z1 + 4] = light
    return g


BUILDERS = {
    'stone_pillar_1': stone_pillar_1,
    'notice_board_1': notice_board_1,
}


def main():
    root = voxlib.workspace_root()
    out_dir = voxlib.obstacles_vox_dir(root)
    if not os.path.isdir(out_dir):
        os.makedirs(out_dir)
    for name, build in sorted(BUILDERS.items()):
        g = build()
        xs, ys, zs = np.nonzero(g)
        voxels = list(zip(xs.tolist(), ys.tolist(), zs.tolist(), g[xs, ys, zs].tolist()))
        path = os.path.join(out_dir, name + '.vox')
        voxlib.write_vox(path, g.shape, voxels)
        print('%-16s SIZE %3dx%3dx%-4d  %d cols x %d rows  %8d voxels  -> %s'
              % (name, g.shape[0], g.shape[1], g.shape[2], g.shape[0] // TILE,
                 g.shape[1] // TILE, len(voxels), path))


if __name__ == '__main__':
    main()
