"""Build the stone props chapter 21 adds -- the ruined shrine on the plateau.

Run once to produce, in Resources/Remastered/Obstacles/vox/:

    stone_shrine_1   3 x 1   the dark stone shrine box (tiles 146..163 / 172..174):
                             a foot step, a body whose front is a dark tablet with
                             rows of dots, and a lighter roof slab. Six on the map.
    stone_column_1   1 x 1   the tall column: plinth, fluted shaft, capital and a
                             ball on top (tiles 85 over 87 over 89). Twelve.
    stone_column_2   1 x 1   the short column: a wide plinth, a squat block and the
                             ball (tiles 85 / 90 over 89 / 94). Twenty.

**Every one of them is painted in elevation and stands on one row.** A shrine
is drawn over four rows -- roof, body, base, foot -- and only its base row is
Blocked, so the model is three tiles wide and one deep and stands there; the
obstacle list clears all four rows. A tall column is drawn over three rows and
stands on the bottom one, a short column over two. The models are cut down
from the painted heights the way the churches were (build_obstacles_02.py):
a shrine painted 96 px tall is 40 voxels, a tall column painted 72 px is 58.

Colours are the art's own blue-grey stone ramp, carried as each model's own
palette because the MagicaVoxel default palette folds the nine shades into
three or four.

Model space is the obstacle convention (formats.md): x runs left to right
along the map, y = 0 is the map row nearest the camera (the front of the
footprint) and y = 23 the back, z is up. The light comes from the front-left.

    python build_obstacles_21.py            # all three
    python build_obstacles_21.py --only stone_column_1 --force
    python vox_preview.py ../../Resources/Remastered/Obstacles/vox/stone_shrine_1.vox --scale 4
"""

import argparse
import math
import os

import voxlib
from build_trees import make_palette

TILE = voxlib.TILE

# the art's stone ramp, darkest first (ShapePanel21 tiles 85..94, 146..163)
STONE = [
    (0, 0, 0),
    (16, 28, 28),
    (24, 40, 40),
    (36, 52, 52),
    (44, 64, 64),
    (56, 80, 80),
    (72, 92, 92),
    (84, 104, 104),
    (100, 120, 120),
    (116, 136, 136),
]
PAL, INDEX = make_palette(STONE)


def C(rgb):
    return INDEX[rgb]


BLACK, DARKEST, DARKER, DARK, MID_DARK, MID, MID_LIGHT, LIGHT, LIGHTER, LIGHTEST = [C(c) for c in STONE]


class Model(object):
    def __init__(self, cols, rows, height):
        self.w, self.d, self.h = cols * TILE, rows * TILE, height
        self.v = {}

    def set(self, x, y, z, c):
        if 0 <= x < self.w and 0 <= y < self.d and 0 <= z < self.h:
            self.v[(x, y, z)] = c

    def box(self, x0, x1, y0, y1, z0, z1, top, front, side, back=None):
        """A solid block, shaded per face: ``top`` on its upper surface, ``front``
        on the face towards the camera, ``side`` on the flanks and ``back``."""
        back = back if back is not None else side
        for x in range(x0, x1 + 1):
            for y in range(y0, y1 + 1):
                for z in range(z0, z1 + 1):
                    if z == z1:
                        c = top
                    elif y == y0:
                        c = front
                    elif y == y1:
                        c = back
                    else:
                        c = side
                    self.set(x, y, z, c)

    def cylinder(self, cx, cy, r, z0, z1, lit, mid, shade, flute=0):
        """A vertical column, shaded around by how much it faces the front-left
        light; ``flute`` > 0 carves that many shallow grooves into it."""
        for z in range(z0, z1 + 1):
            for x in range(int(cx - r - 1), int(cx + r + 2)):
                for y in range(int(cy - r - 1), int(cy + r + 2)):
                    dx, dy = x - cx, y - cy
                    d = math.hypot(dx, dy)
                    if d > r:
                        continue
                    ang = math.atan2(dy, dx)
                    if flute and d > r - 1.2 and int(((ang + math.pi) / (2 * math.pi)) * flute * 2) % 2 == 1:
                        continue
                    light = (-0.45 * dx - 0.85 * dy) / d if d > 0.5 else 1.0
                    c = lit if light > 0.45 else (shade if light < -0.35 else mid)
                    self.set(x, y, z, c)

    def sphere(self, cx, cy, cz, r, lit, mid, shade):
        for x in range(int(cx - r - 1), int(cx + r + 2)):
            for y in range(int(cy - r - 1), int(cy + r + 2)):
                for z in range(int(cz - r - 1), int(cz + r + 2)):
                    dx, dy, dz = x - cx, y - cy, z - cz
                    d = math.sqrt(dx * dx + dy * dy + dz * dz)
                    if d > r:
                        continue
                    light = (-0.4 * dx - 0.7 * dy + 0.6 * dz) / d if d > 0.5 else 1.0
                    c = lit if light > 0.5 else (shade if light < -0.3 else mid)
                    self.set(x, y, z, c)

    def voxels(self):
        return [(x, y, z, c) for (x, y, z), c in sorted(self.v.items())]


def stone_shrine_1():
    m = Model(3, 1, 40)
    # the foot: a step the width of the body, its front edge catching the light
    m.box(4, 67, 2, 23, 0, 7, top=MID_LIGHT, front=LIGHT, side=MID)
    for x in range(4, 68):
        m.set(x, 2, 7, LIGHTER)
    # the body: dark stone, its front a tablet
    m.box(7, 64, 5, 21, 8, 31, top=MID_DARK, front=DARKER, side=DARK, back=DARK)
    # the tablet's frame and its rows of dots
    for x in range(9, 63):
        for z in (10, 29):
            m.set(x, 5, z, DARK)
    for z in (10, 29):
        pass
    for x in range(7, 65):
        m.set(x, 5, 8, MID)
        m.set(x, 5, 31, MID)
    for row, z in enumerate(range(12, 28, 3)):
        for x in range(11 + (row % 2) * 2, 61, 4):
            m.set(x, 5, z, BLACK)
            m.set(x + 1, 5, z, DARKEST)
    # the roof slab: lighter, overhanging the body, its front edge lightest
    m.box(3, 68, 1, 23, 32, 39, top=LIGHT, front=MID_LIGHT, side=MID, back=MID)
    for x in range(3, 69):
        m.set(x, 1, 39, LIGHTER)
        m.set(x, 1, 38, LIGHTER)
    for x in range(5, 67):
        m.set(x, 2, 39, LIGHTEST)
    return m


def stone_column_1():
    m = Model(1, 1, 58)
    cx = cy = 11.5
    # plinth and its flare
    m.box(4, 19, 4, 19, 0, 3, top=MID_LIGHT, front=MID, side=MID_DARK)
    m.box(6, 17, 6, 17, 4, 6, top=MID, front=MID_DARK, side=DARK)
    # the fluted shaft
    m.cylinder(cx, cy, 4.6, 7, 43, lit=LIGHT, mid=MID, shade=DARK, flute=6)
    # capital and the block the ball sits on
    m.box(6, 17, 6, 17, 44, 46, top=MID_LIGHT, front=MID, side=MID_DARK)
    m.box(8, 15, 8, 15, 47, 49, top=MID, front=MID_DARK, side=DARK)
    m.sphere(cx, cy, 53.0, 4.6, lit=LIGHTER, mid=MID_LIGHT, shade=DARK)
    return m


def stone_column_2():
    m = Model(1, 1, 28)
    cx = cy = 11.5
    # the wide plinth, stepped
    m.box(3, 20, 3, 20, 0, 3, top=MID_LIGHT, front=MID, side=MID_DARK)
    m.box(5, 18, 5, 18, 4, 6, top=MID, front=MID_DARK, side=DARK)
    # the squat block
    m.box(7, 16, 7, 16, 7, 14, top=MID_LIGHT, front=MID, side=MID_DARK)
    m.box(8, 15, 8, 15, 15, 17, top=MID, front=MID_DARK, side=DARK)
    m.sphere(cx, cy, 22.5, 5.0, lit=LIGHTER, mid=MID_LIGHT, shade=DARK)
    return m


BUILDERS = {
    'stone_shrine_1': stone_shrine_1,
    'stone_column_1': stone_column_1,
    'stone_column_2': stone_column_2,
}


def main():
    ap = argparse.ArgumentParser(description=__doc__.split('\n\n')[0])
    ap.add_argument('--root')
    ap.add_argument('--only', action='append', help='build just this key (repeatable)')
    ap.add_argument('--force', action='store_true')
    a = ap.parse_args()
    root = a.root or voxlib.workspace_root()
    out_dir = voxlib.obstacles_vox_dir(root)

    for key, build in BUILDERS.items():
        if a.only and key not in a.only:
            continue
        path = os.path.join(out_dir, key + '.vox')
        if os.path.exists(path) and not a.force:
            print('exists, skipping (use --force):', path)
            continue
        m = build()
        voxels = m.voxels()
        xs = [v[0] for v in voxels]
        ys = [v[1] for v in voxels]
        zs = [v[2] for v in voxels]
        voxlib.write_vox(path, (m.w, m.d, m.h), voxels, palette=PAL)
        print('wrote %s  SIZE (%d, %d, %d)  %d voxels  extent x %d..%d y %d..%d z %d..%d' % (
            path, m.w, m.d, m.h, len(voxels), min(xs), max(xs), min(ys), max(ys), min(zs), max(zs)))


if __name__ == '__main__':
    main()
