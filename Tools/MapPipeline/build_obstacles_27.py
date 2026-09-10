"""Build the props chapters 27-30 add -- the blue light pillars, the stone stele
and the two-row stone column.

Run once to produce, in Resources/Remastered/Obstacles/vox/:

    light_pillar_1   1 x 1   the tall pillar of blue light: a white-blue column
                             painted over two rows (chapter 27's 32 over 40, 36
                             over 41; 28's 196 over 201; 29's 148 over 156, 152
                             over 157; 30's 152 over 157), tongues of dark blue
                             licking off its top. Two frames.
    light_pillar_2   1 x 1   the short one, painted in one row (27's 42, 28's
                             202, 29's 158, 30's 158). Two frames.
    stone_stele_1    1 x 1   the standing stone tablet: an olive-grey slab with
                             rows of dots and a blue-lit slot on its face under a
                             pale cap (27's 228 over 230 in the pool, 28's 244
                             over 189, 29's 104 over 109).
    stone_column_3   1 x 1   chapter 21's tall column (stone_column_1) painted
                             over two rows instead of three (27's 180 over 184):
                             the same plinth, fluted shaft, capital and ball,
                             a tile shorter.

**Every one of them stands on the Blocked tile and the rows around it are
cleaned.** The art draws each prop in elevation over its stack of tiles and
then paints a reflection of it on the floor in the row below -- pale streaks
under the columns and statues, a lit ellipse under the light pillars, a
ghost of the tablet under the steles. The user asked for those reflections to
be ignored, so no model carries one: the rows below go back to plain floor
through ``_erase`` entries (map_clean.py).

**The light pillars are chapter 10's fire pillars in blue.** The column is
solid white-blue at the foot and shades to sky blue at the top; the flame on
top is build_obstacles_10's flame() -- the same six tongues, tall in one frame
and short in the other -- run on a ramp that goes the other way, pale at the
base and near-black navy at the tips, as the art draws them. No dish: the art
has none, the reflection on the floor being the only thing at the foot. Colours
are the art's own blue ramp, carried as the model's palette, because the
MagicaVoxel default palette has only three blues between white and navy.

Model space is the obstacle convention (formats.md): x runs left to right along
the map, y = 0 is the map row nearest the camera and y = 23 the back, z is up.
The light comes from the front-left.

    python build_obstacles_27.py
    python build_obstacles_27.py --only light_pillar_1 --force
    python vox_preview.py ../../Resources/Remastered/Obstacles/vox/light_pillar_1.vox --scale 4
"""

import argparse
import math
import os

import numpy as np

import voxlib
import build_obstacles_10 as b10
from build_obstacles_21 import (Model, PAL as STONE_PAL, MID_DARK, MID, MID_LIGHT, LIGHT,
                                LIGHTER, DARK)
from build_trees import make_palette

TILE = voxlib.TILE
CX = CY = 11.5

# -------------------------------------------------------------------------
# the light pillars
# -------------------------------------------------------------------------

# the art's blue ramp (ShapePanel28 tiles 196 / 201), brightest first
BLUES = [
    (200, 224, 252),
    (172, 200, 240),
    (148, 184, 228),
    (128, 164, 220),
    (116, 152, 208),
    (104, 136, 200),
    (92, 124, 188),
    (68, 96, 164),
    (56, 84, 152),
    (40, 68, 136),
    (28, 52, 116),
    (0, 8, 48),
]
LIGHT_PAL, LIGHT_INDEX = make_palette(BLUES)
(WHITE_BLUE, PALE, PALER_SKY, SKY, MID_SKY, DEEP_SKY, BLUE, MID_BLUE, DEEP_BLUE,
 DARK_BLUE, NAVY, INK) = [LIGHT_INDEX[c] for c in BLUES]

COLUMN_R = 7.5          # 16 voxels across, as the art draws it
TALL_H, SHORT_H = 48, 28
TALL_COLUMN_TOP = 32    # where the tongues start; the tallest reaches z 48
SHORT_COLUMN_TOP = 12

# the tongues: pale where they leave the column, ink at the tips
FLAME_RAMP = ((0.14, MID_SKY), (0.28, DEEP_SKY), (0.42, BLUE), (0.56, MID_BLUE),
              (0.70, DEEP_BLUE), (0.82, DARK_BLUE), (0.92, NAVY), (1.01, INK))


def column_colour(z, top):
    """The column shades up from white-blue at the foot to sky blue where the
    tongues begin."""
    t = z / float(top)
    if t < 0.36:
        return WHITE_BLUE, PALE
    if t < 0.56:
        return PALE, PALER_SKY
    if t < 0.74:
        return PALER_SKY, SKY
    if t < 0.88:
        return SKY, MID_SKY
    return MID_SKY, DEEP_SKY


def light_column(g, top):
    xs = np.arange(TILE)[:, None]
    ys = np.arange(TILE)[None, :]
    d2 = (xs - CX) ** 2 + (ys - CY) ** 2
    inside = d2 <= COLUMN_R ** 2
    rim = inside & (d2 > (COLUMN_R - 1.3) ** 2)
    for z in range(top):
        core, edge = column_colour(z, top)
        g[:, :, z][inside] = core
        g[:, :, z][rim] = edge


def light_pillar(height, column_top, frame):
    g = np.zeros((TILE, TILE, height), dtype=np.uint8)
    light_column(g, column_top)
    b10.flame(g, column_top, frame, FLAME_RAMP)
    return g


def light_pillar_1(frame):
    return light_pillar(TALL_H, TALL_COLUMN_TOP, frame)


def light_pillar_2(frame):
    return light_pillar(SHORT_H, SHORT_COLUMN_TOP, frame)


# -------------------------------------------------------------------------
# the stele
# -------------------------------------------------------------------------

# the art's colours (ShapePanel27 tiles 226 / 228)
STELE = [
    (36, 32, 24),       # deepest
    (48, 44, 36),       # dots
    (64, 60, 48),       # dark side
    (80, 80, 64),       # face
    (92, 96, 80),       # face edge
    (104, 116, 96),     # lit edge
    (116, 132, 112),    # cap
    (128, 148, 128),    # cap, lit
    (148, 168, 152),    # cap, lightest
    (28, 52, 116),      # slot, rim
    (52, 80, 148),      # slot
]
STELE_PAL, STELE_INDEX = make_palette(STELE)
(S_DEEPEST, S_DOT, S_SIDE, S_FACE, S_EDGE, S_LIT, S_CAP, S_CAP_LIT, S_CAP_LIGHTEST,
 S_SLOT_RIM, S_SLOT) = [STELE_INDEX[c] for c in STELE]

STELE_H = 40


def stone_stele_1():
    m = Model(1, 1, STELE_H)
    # the foot slab, a little wider than the tablet
    m.box(1, 22, 5, 18, 0, 2, top=S_EDGE, front=S_LIT, side=S_SIDE)
    # the tablet: twenty wide, ten deep, its front face the inscribed one
    m.box(2, 21, 7, 16, 3, 33, top=S_EDGE, front=S_FACE, side=S_SIDE, back=S_SIDE)
    # the face's raised border and the rows of dots (the art's lines of script)
    for x in range(2, 22):
        m.set(x, 7, 3, S_EDGE)
        m.set(x, 7, 33, S_EDGE)
    for z in range(4, 33):
        m.set(2, 7, z, S_EDGE)
        m.set(21, 7, z, S_EDGE)
    for row, z in enumerate(range(12, 31, 3)):
        for x in range(5 + (row % 2) * 2, 19, 3):
            m.set(x, 7, z, S_DOT)
            m.set(x + 1, 7, z, S_DEEPEST)
    # the blue-lit slot low on the face
    for x in range(7, 17):
        for z in range(5, 9):
            m.set(x, 7, z, S_SLOT_RIM)
    for x in range(8, 16):
        for z in range(6, 8):
            m.set(x, 7, z, S_SLOT)
    # the cap: a pale slab overhanging the tablet, its front edge catching the light
    m.box(1, 22, 6, 17, 34, 39, top=S_CAP_LIT, front=S_CAP, side=S_CAP, back=S_CAP)
    for x in range(1, 23):
        m.set(x, 6, 39, S_CAP_LIGHTEST)
        m.set(x, 6, 38, S_CAP_LIT)
    for x in range(3, 21):
        m.set(x, 7, 39, S_CAP_LIGHTEST)
    return m


# -------------------------------------------------------------------------
# the two-row column
# -------------------------------------------------------------------------

COLUMN_3_H = 40


def stone_column_3():
    """stone_column_1 with the shaft a tile shorter."""
    m = Model(1, 1, COLUMN_3_H)
    cx = cy = 11.5
    m.box(4, 19, 4, 19, 0, 3, top=MID_LIGHT, front=MID, side=MID_DARK)
    m.box(6, 17, 6, 17, 4, 6, top=MID, front=MID_DARK, side=DARK)
    m.cylinder(cx, cy, 4.6, 7, 25, lit=LIGHT, mid=MID, shade=DARK, flute=6)
    m.box(6, 17, 6, 17, 26, 28, top=MID_LIGHT, front=MID, side=MID_DARK)
    m.box(8, 15, 8, 15, 29, 31, top=MID, front=MID_DARK, side=DARK)
    m.sphere(cx, cy, 35.0, 4.5, lit=LIGHTER, mid=MID_LIGHT, shade=DARK)
    return m


# -------------------------------------------------------------------------

FRAMES = 2


def grid_voxels(g):
    xs, ys, zs = np.nonzero(g)
    return list(zip(xs.tolist(), ys.tolist(), zs.tolist(), g[xs, ys, zs].tolist()))


def extent(voxels):
    xs = [v[0] for v in voxels]
    ys = [v[1] for v in voxels]
    zs = [v[2] for v in voxels]
    return min(xs), max(xs), min(ys), max(ys), min(zs), max(zs)


def build_all():
    """(file stem, SIZE, voxels, palette) for every model and frame."""
    out = []
    for key, build in (('light_pillar_1', light_pillar_1), ('light_pillar_2', light_pillar_2)):
        for frame in range(FRAMES):
            g = build(frame)
            out.append((b10.frame_name(key, frame), g.shape, grid_voxels(g), LIGHT_PAL))
    m = stone_stele_1()
    out.append(('stone_stele_1', (m.w, m.d, m.h), m.voxels(), STELE_PAL))
    m = stone_column_3()
    out.append(('stone_column_3', (m.w, m.d, m.h), m.voxels(), STONE_PAL))
    return out


def main():
    ap = argparse.ArgumentParser(description=__doc__.split('\n\n')[0])
    ap.add_argument('--root')
    ap.add_argument('--only', action='append', help='build just this key (repeatable)')
    ap.add_argument('--force', action='store_true')
    a = ap.parse_args()
    root = a.root or voxlib.workspace_root()
    out_dir = voxlib.obstacles_vox_dir(root)
    if not os.path.isdir(out_dir):
        os.makedirs(out_dir)

    for stem, size, voxels, pal in build_all():
        key = stem.split('_f')[0] if '_f' in stem[-4:] else stem
        if a.only and key not in a.only:
            continue
        path = os.path.join(out_dir, stem + '.vox')
        if os.path.exists(path) and not a.force:
            print('exists, skipping (use --force):', path)
            continue
        voxlib.write_vox(path, size, voxels, palette=pal)
        print('wrote %-18s SIZE %3dx%3dx%-3d  %6d voxels  extent x %d..%d y %d..%d z %d..%d'
              % ((stem,) + tuple(size) + (len(voxels),) + extent(voxels)))


if __name__ == '__main__':
    main()
