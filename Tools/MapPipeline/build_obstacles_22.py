"""Build the props chapter 22 adds -- the stone monument and the six crystal orbs.

Run once to produce, in Resources/Remastered/Obstacles/vox/:

    stone_shrine_2      5 x 2   the wide monument at the head of the plateau: the same
                                dark tablet with rows of dots and the same light base
                                slab as chapter 21's stone_shrine_1, but five tiles wide
                                (tiles 146..172 / 234..236 / 140..142 / 143..145). Its
                                body *and* base rows are Blocked, so it stands on two.
    orb_pillar_<colour> 1 x 1   the knight's pedestal (build_obstacles_08) with a glass
                                orb on it instead of a bust: yellow, orange, green,
                                purple, red, blue (tiles 252, 248, 254, 250, 256, 258
                                over pedestal 33). Six on the map, one of each.

**The monument is painted in elevation over four rows and stands on two.** Roof
(row 14), tablet face (15), tablet foot with the top of the base slab (16) and
the front of the base slab (17): rows 15 and 16 are the Blocked ones, so the
model is 5 cols x 2 rows, the slab covering the footprint and the tablet standing
on its back half; the obstacle list clears all four rows. At 48 voxels it is a
little taller than the 3-wide shrine (40), as the art draws it.

The monument borrows chapter 21's stone ramp (its own palette); the orbs stay on
the default palette like the statues they stand on, with the orb colours taken
off the tile art.

Model space is the obstacle convention (formats.md): x runs left to right along
the map, y = 0 is the map row nearest the camera (the front of the footprint)
and y = rows * 24 - 1 the back, z is up. The light comes from the front-left.

    python build_obstacles_22.py
    python build_obstacles_22.py --only orb_pillar_red --force
    python vox_preview.py ../../Resources/Remastered/Obstacles/vox/stone_shrine_2.vox --scale 4
"""

import argparse
import colorsys
import os

import numpy as np
from PIL import Image

import voxlib
from build_obstacles_08 import STATUE, pedestal
from build_obstacles_21 import (Model, PAL, BLACK, DARKEST, DARKER, DARK, MID_DARK, MID,
                                MID_LIGHT, LIGHT, LIGHTER, LIGHTEST)

TILE = voxlib.TILE


def P(rgb):
    return voxlib.palette_index(rgb)


# --------------------------------------------------------------------------
# the monument
# --------------------------------------------------------------------------

SHRINE_COLS, SHRINE_ROWS, SHRINE_H = 5, 2, 48


def stone_shrine_2():
    m = Model(SHRINE_COLS, SHRINE_ROWS, SHRINE_H)
    W, D = m.w, m.d
    # the base slab: the whole footprint, its front edge catching the light
    m.box(3, W - 4, 2, D - 2, 0, 9, top=MID_LIGHT, front=LIGHT, side=MID)
    for x in range(3, W - 3):
        m.set(x, 2, 9, LIGHTER)
        m.set(x, 2, 8, LIGHTER)
    for x in range(5, W - 5):
        m.set(x, 3, 9, LIGHTEST)
    # the tablet: dark stone standing on the back half of the slab
    ty0, ty1 = D // 2 - 2, D - 3
    m.box(8, W - 9, ty0, ty1, 10, 37, top=MID_DARK, front=DARKER, side=DARK, back=DARK)
    # its frame, and the rows of dots on the face
    for x in range(8, W - 8):
        m.set(x, ty0, 10, MID)
        m.set(x, ty0, 37, MID)
    for z in (11, 36):
        for x in range(10, W - 10):
            m.set(x, ty0, z, DARK)
    for row, z in enumerate(range(14, 34, 3)):
        for x in range(13 + (row % 2) * 2, W - 13, 4):
            m.set(x, ty0, z, BLACK)
            m.set(x + 1, ty0, z, DARKEST)
    # the roof slab: lighter, overhanging the tablet, its front edge lightest
    m.box(4, W - 5, ty0 - 2, D - 1, 38, SHRINE_H - 1, top=LIGHT, front=MID_LIGHT, side=MID, back=MID)
    for x in range(4, W - 4):
        m.set(x, ty0 - 2, SHRINE_H - 1, LIGHTER)
        m.set(x, ty0 - 2, SHRINE_H - 2, LIGHTER)
    for x in range(6, W - 6):
        m.set(x, ty0 - 1, SHRINE_H - 1, LIGHTEST)
    return m


# --------------------------------------------------------------------------
# the orbs
# --------------------------------------------------------------------------

ORB_H = 40
ORB_TILES = {
    'orb_pillar_yellow': 252,
    'orb_pillar_orange': 248,
    'orb_pillar_green': 254,
    'orb_pillar_purple': 250,
    'orb_pillar_red': 256,
    'orb_pillar_blue': 258,
}


def orb_colours(root, tile_id):
    """(lit, mid, shade, highlight) palette indices sampled off the orb tile:
    the saturated pixels are the glass, ranked by brightness."""
    path = os.path.join(voxlib.shape_panel_dir(root, 22), voxlib.tile_png_name(22, tile_id))
    px = list(Image.open(path).convert('RGB').getdata())
    glass = []
    for (r, g, b) in px:
        h, s, v = colorsys.rgb_to_hsv(r / 255.0, g / 255.0, b / 255.0)
        if s > 0.25 and v > 0.3:
            glass.append((v, (r, g, b)))
    if len(glass) < 20:
        raise SystemExit('tile %d does not look like an orb' % tile_id)
    glass.sort()
    n = len(glass)
    shade = glass[n // 8][1]
    mid = glass[n // 2][1]
    lit = glass[(n * 6) // 7][1]
    highlight = tuple(min(255, c + (255 - c) * 2 // 3) for c in lit)
    return P(lit), P(mid), P(shade), P(highlight)


def orb_pillar(root, tile_id):
    lit, mid, shade, highlight = orb_colours(root, tile_id)
    _, stone_mid, _, _ = STATUE
    g = pedestal(np.zeros((TILE, TILE, ORB_H), dtype=np.uint8))
    g[9:15, 9:15, 14:17] = stone_mid                     # a short neck the orb sits on
    xs = np.arange(TILE)[:, None, None]
    ys = np.arange(TILE)[None, :, None]
    zs = np.arange(ORB_H)[None, None, :]
    cx = cy = 11.5
    cz, r = 23.0, 6.5
    dx, dy, dz = xs - cx, ys - cy, zs - cz
    ball = dx * dx + dy * dy + dz * dz <= r * r
    # lit from the front-left and above, like the stone balls
    d = np.sqrt(dx * dx + dy * dy + dz * dz) + 1e-6
    light = (-0.4 * dx - 0.7 * dy + 0.6 * dz) / d
    g[ball] = mid
    g[ball & (light > 0.35)] = lit
    g[ball & (light < -0.3)] = shade
    g[ball & (light > 0.88)] = highlight
    return g


def write_grid(path, g):
    xs, ys, zs = np.nonzero(g)
    voxels = list(zip(xs.tolist(), ys.tolist(), zs.tolist(), g[xs, ys, zs].tolist()))
    voxlib.write_vox(path, g.shape, voxels)
    return len(voxels), (xs.min(), xs.max(), ys.min(), ys.max(), zs.min(), zs.max())


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

    keys = ['stone_shrine_2'] + sorted(ORB_TILES)
    for key in keys:
        if a.only and key not in a.only:
            continue
        path = os.path.join(out_dir, key + '.vox')
        if os.path.exists(path) and not a.force:
            print('exists, skipping (use --force):', path)
            continue
        if key == 'stone_shrine_2':
            m = stone_shrine_2()
            voxels = m.voxels()
            voxlib.write_vox(path, (m.w, m.d, m.h), voxels, palette=PAL)
            xs = [v[0] for v in voxels]
            ys = [v[1] for v in voxels]
            zs = [v[2] for v in voxels]
            n, ext = len(voxels), (min(xs), max(xs), min(ys), max(ys), min(zs), max(zs))
            size = (m.w, m.d, m.h)
        else:
            g = orb_pillar(root, ORB_TILES[key])
            n, ext = write_grid(path, g)
            size = g.shape
        print('wrote %s  SIZE %s  %d voxels  extent x %d..%d y %d..%d z %d..%d' % ((path, size, n) + tuple(ext)))


if __name__ == '__main__':
    main()
