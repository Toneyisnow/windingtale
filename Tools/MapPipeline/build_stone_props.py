"""Build the two stone props of the chapter 08 / 09 roads, remade bigger and in the art's own stone.

Run to produce, in Resources/Remastered/Obstacles/vox/:

    stone_pillar_1    1 col x 1 row   the short square pillar with a stone ball on top
    stone_statue_1    1 col x 1 row   the same pillar with a great-helm knight standing on it

They replace the first versions (build_obstacles_09.stone_pillar_1 and
build_obstacles_08.stone_statue_1, which are no longer built): those filled 18 of the 24
voxels of a tile, stood 30 (the pillar) to 40 (the knight) tall, and were all one flat teal grey, so on the map the ball
read as a small marble on a stump.

**Bigger.** The pillar is now 45 voxels tall (was 30 -- the 40 of its SIZE was empty headroom, +50%) on a plinth that fills the whole
tile (24 wide, was 18 -- the tile is as wide as a model may go), with a ball of radius 8.

**More stone colours.** Both carry their own palette: the art's teal-grey ramp with white and
light-grey steps added at the top and a warm grey between, so edges and lit faces catch
white, the plinth and shadow sides fall to dark grey, and the ball is shaded round its
surface by where the light (front-left, above) meets it.

**The knight is the art's knight, made solid.** Looking at the original painting
(Chapter-09.png, the pillar at map 9,8): a bucket-shaped great helm with a T-shaped visor -- a
wide slit across and a narrow one running down -- and a small pointed horn at each side of the
crown; a heater shield with a T on its face held out at the viewer's left; broad shoulders and
a chest of plates outlined in black. Here: a blocky helm with a domed crown, the T cut into
its front and the two horns, the shield standing off the chest at x-low with its T raised in
dark, pauldrons and a plate belt on the torso.

Model space is the obstacle convention (formats.md): x runs left to right along the map, y = 0
is the map row nearest the camera (the front of the footprint) and y = 23 the back, z is up.
The light comes from the front-left.

    python build_stone_props.py
    python vox_preview.py ../../Resources/Remastered/Obstacles/vox/stone_statue_1.vox --scale 6
    python vox_batch_to_obj.py --obstacles --force     # or --in with a folder holding just these two
"""

import argparse
import os

import numpy as np

import voxlib
from build_trees import make_palette

TILE = voxlib.TILE
H = 56                      # the knight's height; the ball pillar is 45

# the art's teal-grey ramp with a white and a light-grey step on top, a warm grey between
STONE = {
    'white': (238, 242, 242),
    'pale': (204, 212, 212),
    'warm': (168, 172, 168),
    'light': (140, 154, 154),
    'mid': (112, 130, 130),
    'shade': (84, 104, 104),
    'dark': (56, 76, 76),
    'deep': (30, 44, 44),
    'black': (10, 14, 14),
}
NAMES = ['white', 'pale', 'warm', 'light', 'mid', 'shade', 'dark', 'deep', 'black']
PAL, INDEX = make_palette([STONE[n] for n in NAMES])
C = dict((n, INDEX[STONE[n]]) for n in NAMES)

# darkest first, for shading a round surface by light
RAMP = [C['dark'], C['shade'], C['mid'], C['light'], C['warm'], C['pale'], C['white']]


def box(g, x0, x1, y0, y1, z0, z1, colour):
    g[x0:x1, y0:y1, z0:z1] = colour


def pedestal(g, top):
    """The square pillar under both props: a stepped plinth filling the tile, a shaft with a
    carved panel on each side (a chequer, like the art's), and a cap, ending at z = top.

    The shaft's corners are white, its faces light grey, its back side (y high, x high) a
    shade darker -- the light comes from the front-left.
    """
    plinth_h, step_h = 5, 3
    cap_h = 4
    shaft_z0 = plinth_h + step_h
    shaft_z1 = top - cap_h

    box(g, 0, 24, 0, 24, 0, plinth_h, C['dark'])
    box(g, 0, 24, 0, 2, 0, plinth_h, C['shade'])                 # the lit front lip
    box(g, 0, 2, 0, 24, 0, plinth_h, C['shade'])                 # and the lit left lip
    box(g, 2, 22, 2, 22, plinth_h, plinth_h + step_h, C['mid'])

    box(g, 3, 21, 3, 21, shaft_z0, shaft_z1, C['mid'])
    box(g, 3, 21, 3, 4, shaft_z0, shaft_z1, C['light'])          # the front face, lit
    box(g, 3, 4, 3, 21, shaft_z0, shaft_z1, C['warm'])           # the left face, lit most
    box(g, 20, 21, 3, 21, shaft_z0, shaft_z1, C['shade'])        # the right face, in shade
    box(g, 3, 21, 20, 21, shaft_z0, shaft_z1, C['shade'])        # the back, in shade
    box(g, 3, 4, 3, 4, shaft_z0, shaft_z1, C['white'])           # the corner arrises catch the light
    box(g, 20, 21, 3, 4, shaft_z0, shaft_z1, C['pale'])
    box(g, 3, 4, 20, 21, shaft_z0, shaft_z1, C['pale'])

    # a carved panel on the front and on each side: a recessed frame round a chequer
    pz0 = shaft_z0 + 3
    pz1 = shaft_z1 - 3
    px0, px1 = 7, 17
    for z in range(pz0, pz1):
        for x in range(px0, px1):
            edge = x in (px0, px1 - 1) or z in (pz0, pz1 - 1)
            tone = C['dark'] if edge else (C['shade'] if (x + z) % 2 == 0 else C['deep'])
            g[x, 3, z] = tone                                    # front (y = 3), cut in one
    for z in range(pz0, pz1):
        for y in range(px0, px1):
            edge = y in (px0, px1 - 1) or z in (pz0, pz1 - 1)
            tone = C['dark'] if edge else (C['shade'] if (y + z) % 2 == 0 else C['deep'])
            g[3, y, z] = tone                                    # left side
            g[20, y, z] = tone                                   # right side

    # the cap: a slab wider than the shaft, the top pale, the sides stepping down in shade
    box(g, 1, 23, 1, 23, shaft_z1, top - 1, C['light'])
    box(g, 1, 23, 1, 2, shaft_z1, top - 1, C['pale'])
    box(g, 1, 2, 1, 23, shaft_z1, top - 1, C['warm'])
    box(g, 22, 23, 1, 23, shaft_z1, top - 1, C['mid'])
    box(g, 1, 23, 22, 23, shaft_z1, top - 1, C['mid'])
    box(g, 1, 23, 1, 23, top - 1, top, C['pale'])
    box(g, 3, 21, 3, 21, top - 1, top, C['white'])
    return g


def shade_by_light(nx, ny, nz):
    """A ramp index for a surface facing (nx, ny, nz) with the light coming from the
    front-left-above."""
    light = np.array([-0.55, -0.55, 0.63])
    n = np.array([nx, ny, nz], dtype=float)
    n = n / max(np.linalg.norm(n), 1e-6)
    lit = float(np.dot(n, light))                  # -1 .. 1
    t = min(1.0, max(0.0, (lit + 0.5) / 1.4))
    return RAMP[int(round(t * (len(RAMP) - 1)))]


def stone_pillar_1():
    """The short square pillar, 45 tall (was 30), with a large stone ball on top."""
    g = np.zeros((TILE, TILE, 45), dtype=np.uint8)
    top = 30
    pedestal(g, top)

    # the ball, resting on the cap and shaded round
    cx = cy = 11.5
    cz = top + 6.5
    r = 7.5
    for x in range(TILE):
        for y in range(TILE):
            for z in range(top, 45):
                dx, dy, dz = x - cx, y - cy, z - cz
                if dx * dx + dy * dy + dz * dz <= r * r:
                    g[x, y, z] = shade_by_light(dx, dy, dz)
    return g


def stone_statue_1():
    """The knight on the pillar: torso, pauldrons, shield, and a T-visored great helm."""
    g = np.zeros((TILE, TILE, H), dtype=np.uint8)
    top = 27
    pedestal(g, top)

    # torso: a block of plate with a dark belt and seams, resting on the cap
    box(g, 6, 18, 8, 17, top, top + 12, C['mid'])
    box(g, 6, 8, 8, 17, top, top + 12, C['warm'])                # lit left side
    box(g, 16, 18, 8, 17, top, top + 12, C['shade'])             # shaded right side
    box(g, 6, 18, 8, 9, top, top + 12, C['light'])               # breastplate front
    box(g, 6, 18, 8, 17, top + 3, top + 4, C['deep'])            # the belt
    box(g, 11, 13, 8, 9, top + 4, top + 11, C['dark'])           # the seam down the chest
    box(g, 8, 16, 8, 9, top + 8, top + 9, C['dark'])             # the plate line across it

    # pauldrons: broad, on the top corners of the torso
    box(g, 3, 8, 7, 18, top + 8, top + 13, C['warm'])
    box(g, 16, 21, 7, 18, top + 8, top + 13, C['mid'])
    box(g, 3, 8, 7, 8, top + 8, top + 13, C['pale'])
    box(g, 3, 21, 7, 18, top + 12, top + 13, C['pale'])           # the tops catch the light

    # the right arm (viewer's right) hangs down the side, a gauntlet at its foot
    box(g, 18, 21, 9, 15, top, top + 8, C['shade'])
    box(g, 18, 21, 9, 15, top, top + 2, C['dark'])

    # the shield, held out at the viewer's left: a heater shape standing off the chest.
    # Wide at the top, tapering to a point; a pale rim and the art's dark T on its face.
    sz0, sz1 = top + 1, top + 15
    for z in range(sz0, sz1):
        t = (z - sz0) / float(sz1 - sz0)
        half = 5 if t > 0.4 else int(round(2 + 3 * (t / 0.4)))
        cxs = 5
        for x in range(cxs - half, cxs + half):
            for y in range(4, 7):
                rim = x in (cxs - half, cxs + half - 1) or z in (sz1 - 1,)
                g[x, y, z] = C['pale'] if rim else C['light']
    box(g, 2, 9, 4, 5, sz1 - 5, sz1 - 3, C['dark'])              # the T's bar
    box(g, 4, 6, 4, 5, sz0 + 3, sz1 - 5, C['dark'])              # the T's stem

    # the great helm: a squat bucket with a domed crown
    hz0 = top + 11
    box(g, 7, 17, 8, 17, hz0, hz0 + 12, C['light'])
    box(g, 7, 9, 8, 17, hz0, hz0 + 12, C['pale'])                # lit left edge
    box(g, 15, 17, 8, 17, hz0, hz0 + 12, C['shade'])             # shaded right edge
    box(g, 6, 18, 7, 18, hz0, hz0 + 2, C['warm'])                # the rim at the neck
    box(g, 8, 16, 9, 16, hz0 + 12, hz0 + 14, C['warm'])          # crown, stepped in twice
    box(g, 9, 15, 10, 15, hz0 + 14, hz0 + 16, C['pale'])
    box(g, 10, 14, 11, 14, hz0 + 16, hz0 + 17, C['white'])

    # the T-shaped visor cut into the front: a wide slit across, a narrow one running down
    box(g, 8, 16, 8, 9, hz0 + 6, hz0 + 8, C['black'])            # the bar
    box(g, 11, 13, 8, 9, hz0 + 1, hz0 + 6, C['black'])           # the stem
    box(g, 8, 16, 8, 9, hz0 + 8, hz0 + 9, C['deep'])            # the brow shadow over the bar

    # a small pointed horn at each side of the crown, angled out
    for side, x0 in ((0, 5), (1, 17)):
        for k in range(5):
            xx = x0 - k // 2 if side == 0 else x0 + k // 2
            box(g, xx, xx + 2, 11, 14, hz0 + 7 + k, hz0 + 8 + k, C['pale'] if side == 0 else C['mid'])
    return g


BUILDERS = {
    'stone_pillar_1': stone_pillar_1,
    'stone_statue_1': stone_statue_1,
}


def main():
    ap = argparse.ArgumentParser(description=__doc__,
                                 formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument('--root')
    ap.add_argument('--only', nargs='*')
    a = ap.parse_args()
    root = a.root or voxlib.workspace_root()
    out_dir = voxlib.obstacles_vox_dir(root)
    if not os.path.isdir(out_dir):
        os.makedirs(out_dir)
    for name, build in sorted(BUILDERS.items()):
        if a.only and name not in a.only:
            continue
        g = build()
        xs, ys, zs = np.nonzero(g)
        voxels = list(zip(xs.tolist(), ys.tolist(), zs.tolist(), g[xs, ys, zs].tolist()))
        path = os.path.join(out_dir, name + '.vox')
        voxlib.write_vox(path, g.shape, voxels, palette=PAL)
        print('%-16s SIZE %3dx%3dx%-4d  %d cols x %d rows  %8d voxels  -> %s'
              % (name, g.shape[0], g.shape[1], g.shape[2], g.shape[0] // TILE,
                 g.shape[1] // TILE, len(voxels), path))


if __name__ == '__main__':
    main()
