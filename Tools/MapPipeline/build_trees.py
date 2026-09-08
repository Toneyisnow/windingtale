"""Build the tree obstacles: one fixed model per crown colour and tree shape.

Every tree on every map is one of two shapes, and one of a dozen colours:

    normal   a wide cone crown, a short vertical band of foliage under it,
             then a trunk with a root mound -- every chapter but 17 and 20
    pine     a slimmer cone that reaches the ground, no trunk -- chapters
             17 and 20 only

so the models are ``tree_<colour>.vox`` and ``pine_<colour>.vox`` in
Resources/Remastered/Obstacles/vox/, each 1 x 1 tile, and a chapter places
one per tree (see tree_obstacles.py). There is deliberately one model per
colour, not a random mix: the same tree on every map is what the 2D art does.

Colours are the art's own crown ramps, read off the tile PNGs (the darkest
shade is the band under the crown), and the models carry them as their own
palette -- the MagicaVoxel default palette is a 6-step cube that folds the
seven shades of a crown into three.

Run once (``--force`` to rebuild):

    python build_trees.py
    python build_trees.py --only tree_dark_green
    python vox_preview.py ../../Resources/Remastered/Obstacles/vox/tree_dark_green.vox --scale 4

Model space is the obstacle convention (formats.md): x runs left to right
along the map, y = 0 is the map row nearest the camera (the front of the
footprint) and y = 23 the back, z is up. The light comes from the front-left.

Adding a colour: sample the crown's shades off its tile art (lightest first,
seven of them), add a ``COLOURS`` entry, and list the key in ``TREES``.
"""

import argparse
import math
import os

import numpy as np

import voxlib

TILE = voxlib.TILE
CX = CY = 11.5          # the tree stands in the middle of its tile

# Crown shades per colour, lightest -> darkest. Index 2 is the body colour the
# art paints most of the cone in; 0-1 are the lit bands, 3-4 the shadow bands,
# 5-6 the shade under the crown. Sampled from the tile PNGs:
#   dark_green    chapter 01 tiles 44 / 46, chapter 04 128 / 129
#   light_green   chapter 01 tiles 40 / 42, chapter 04 131 / 134
#   bright_green  chapter 04 tiles 138 / 139
COLOURS = {
    'dark_green': [(92, 124, 36), (84, 112, 32), (72, 100, 24), (64, 88, 20),
                   (56, 76, 16), (48, 64, 12), (32, 44, 8)],
    'light_green': [(152, 140, 80), (140, 140, 60), (120, 132, 48), (100, 120, 32),
                    (76, 112, 20), (52, 100, 12), (32, 92, 4)],
    'bright_green': [(120, 164, 16), (100, 148, 12), (84, 132, 8), (68, 116, 8),
                     (64, 88, 20), (56, 76, 16), (40, 52, 8)],
    #   blue          chapter 05 tiles 164 / 165 (also 07, 08, 09)
    #   dark_red      chapter 05 tiles 168 / 169
    #   light_red     chapter 05 tiles 171 / 173
    'blue': [(68, 96, 116), (52, 80, 104), (36, 68, 96), (24, 56, 84),
             (16, 48, 76), (8, 36, 64), (0, 28, 56)],
    'dark_red': [(144, 60, 24), (128, 44, 16), (116, 32, 12), (100, 24, 8),
                 (88, 12, 4), (72, 4, 0), (60, 0, 0)],
    'light_red': [(192, 104, 64), (180, 88, 48), (168, 72, 32), (144, 60, 24),
                  (128, 44, 16), (116, 32, 12), (88, 12, 4)],
    #   dark_gray      chapter 13 tiles 91 / 92 (also 14, 15, 21) -- the brown dead trees
    #   light_gray     chapter 13 tiles 88 / 89 -- the tan ones
    'dark_gray': [(148, 112, 76), (132, 96, 60), (116, 84, 48), (104, 72, 36),
                  (88, 60, 24), (76, 48, 16), (48, 28, 4)],
    'light_gray': [(200, 172, 136), (184, 156, 120), (168, 140, 100), (152, 124, 88),
                   (136, 108, 72), (120, 96, 60), (104, 80, 48)],
    #   snow           the snow-covered cone of chapter 16's trees (tile 144) and
    #                  chapter 17's pines (tile 164); the 'snow' shape paints the
    #                  cone in this and the foliage band under it in the tree's colour
    'snow': [(252, 252, 252), (216, 244, 220), (196, 220, 200), (176, 196, 180),
             (160, 180, 164), (148, 168, 152), (128, 148, 128)],
    #   snow_blue      chapter 16 tile 149 -- the blue-green showing under the snow
    #   snow_red       chapter 16 tile 145 -- the red-brown showing under the snow
    'snow_blue': [(116, 164, 136), (100, 148, 120), (84, 132, 104), (68, 112, 88),
                  (56, 96, 76), (56, 96, 76), (40, 52, 8)],
    'snow_red': [(184, 156, 120), (168, 140, 100), (152, 124, 88), (136, 108, 72),
                 (120, 96, 60), (104, 80, 48), (92, 68, 40)],
    #   pine_snow      chapter 17 tiles 164 / 166 / 170: snow with a dark skirt
    #   pine_blue      chapter 20 tiles 132 / 134 / 136 / 138: the night-blue pines
    'pine_snow': [(252, 252, 252), (216, 244, 220), (196, 220, 200), (176, 196, 180),
                  (160, 180, 164), (104, 116, 96), (80, 80, 64)],
    'pine_blue': [(116, 136, 136), (100, 120, 120), (84, 104, 104), (72, 92, 92),
                  (56, 80, 80), (44, 64, 64), (36, 52, 52)],
}

# Bark and the root mound, shared by every normal tree.
BARK = [(92, 68, 40), (76, 48, 16), (60, 36, 8), (48, 28, 4), (24, 20, 12)]
ROOT = [(120, 96, 60), (104, 80, 48), (88, 60, 24), (76, 48, 16), (60, 36, 8)]
# The drop shadow the art paints under the roots -- dark red under the green
# and red trees, black under the blue ones. Not modelled (the 3D tree casts
# its own) but tree_obstacles.py needs to know it is part of the tree.
SHADOW = [(48, 0, 0), (0, 0, 0)]

# key -> (shape, colour). 'snow' is the normal tree with its cone under snow.
TREES = {
    'tree_dark_green': ('normal', 'dark_green'),
    'tree_light_green': ('normal', 'light_green'),
    'tree_bright_green': ('normal', 'bright_green'),
    'tree_blue': ('normal', 'blue'),
    'tree_dark_red': ('normal', 'dark_red'),
    'tree_light_red': ('normal', 'light_red'),
    'tree_dark_gray': ('normal', 'dark_gray'),
    'tree_light_gray': ('normal', 'light_gray'),
    'tree_snow_blue': ('snow', 'snow_blue'),
    'tree_snow_red': ('snow', 'snow_red'),
    'pine_snow': ('pine', 'pine_snow'),
    'pine_blue': ('pine', 'pine_blue'),
}

# --------------------------------------------------------------------------
# geometry
# --------------------------------------------------------------------------

# The normal tree, in voxels. The art draws it over two tiles: the cone fills
# the upper one, the lower holds the foliage band, the trunk and the roots.
NORMAL_H = 46
CONE_Z0 = 21            # where the cone starts (its widest layer)
CONE_R = 10.3           # cone base radius -- the art's cone is ~21 px wide
BAND_Z0 = 12            # the foliage band under the cone starts here...
BAND_PROFILE = (6.5, 8.0, 9.0, 9.8, 10.3)   # ...rounding out to the cone radius
TRUNK_R = 2.6
TRUNK_TOP = 14          # the trunk disappears into the band
ROOT_PROFILE = (5.8, 5.0, 4.0, 2.8)         # the root mound, radius per layer

# The pine: a slimmer cone straight off the ground.
PINE_H = 46
PINE_R = 8.0

BAND_PERIOD = 4         # the horizontal shading bands of the cone repeat every 4 layers
CONE_BANDS = (2, 1, 2, 3)   # shade index per layer within the period (mid, lit, mid, dark)


def coords():
    xs = np.arange(TILE)[:, None]
    ys = np.arange(TILE)[None, :]
    return xs, ys


def disc_mask(r, cx=CX, cy=CY):
    xs, ys = coords()
    return (xs - cx) ** 2 + (ys - cy) ** 2 <= r * r


def side():
    """-1 on the lit (front-left) side of the tile, +1 on the shadow (back-right)
    side, 0 across the middle."""
    xs, ys = coords()
    d = (xs - CX) + (ys - CY)
    return np.where(d < -4.5, -1, np.where(d > 4.5, 1, 0))


def cone_radius(z, z0, z1, r0):
    """Radius of a cone layer: ``r0`` at ``z0`` narrowing to a point at ``z1``."""
    t = (z - z0) / float(z1 - z0)
    return max(0.6, r0 * (1.0 - t) ** 0.85)


def paint_layer(g, z, r, shade, ramp, index, sides):
    """One crown layer: ``shade`` in the middle, one lighter on the lit side and
    one darker on the shadow side."""
    m = disc_mask(r)
    lit = np.clip(shade + sides, 0, len(ramp) - 1)
    g[:, :, z][m] = np.vectorize(lambda s: index[ramp[s]])(lit)[m]


def crown_cone(g, z0, z1, r0, ramp, index):
    sides = side()
    for z in range(z0, z1):
        shade = CONE_BANDS[(z - z0) % BAND_PERIOD]
        paint_layer(g, z, cone_radius(z, z0, z1, r0), shade, ramp, index, sides)


def tree_normal(ramp, index, cone_ramp=None):
    """``cone_ramp`` paints the cone in other shades than the band under it --
    the snow trees: white cone, coloured foliage showing beneath."""
    g = np.zeros((TILE, TILE, NORMAL_H), dtype=np.uint8)
    sides = side()

    # root mound: light on top, dark at the ground
    for k, r in enumerate(ROOT_PROFILE):
        z = len(ROOT_PROFILE) - 1 - k
        g[:, :, z][disc_mask(r)] = index[ROOT[min(len(ROOT) - 1, 1 + k)]]
    # trunk, darker on the shadow side
    for z in range(0, TRUNK_TOP):
        m = disc_mask(TRUNK_R)
        g[:, :, z][m] = index[BARK[1]]
        g[:, :, z][m & (sides > 0)] = index[BARK[2]]
        g[:, :, z][m & (sides < 0)] = index[BARK[0]]
    # foliage band under the cone: in shadow, darkest at its underside
    for k, r in enumerate(BAND_PROFILE):
        z = BAND_Z0 + k
        shade = 6 if k == 0 else (5 if k == 1 else 4)
        paint_layer(g, z, r, shade, ramp, index, sides)
    for z in range(BAND_Z0 + len(BAND_PROFILE), CONE_Z0):
        paint_layer(g, z, CONE_R, 3, ramp, index, sides)
    # the cone
    crown_cone(g, CONE_Z0, NORMAL_H, CONE_R, cone_ramp or ramp, index)
    return g


def tree_snow(ramp, index):
    return tree_normal(ramp, index, cone_ramp=COLOURS['snow'])


def tree_pine(ramp, index):
    g = np.zeros((TILE, TILE, PINE_H), dtype=np.uint8)
    sides = side()
    # a dark skirt at the ground, then the cone
    paint_layer(g, 0, PINE_R - 0.8, 6, ramp, index, sides)
    paint_layer(g, 1, PINE_R - 0.3, 5, ramp, index, sides)
    crown_cone(g, 2, PINE_H, PINE_R, ramp, index)
    return g


SHAPES = {'normal': tree_normal, 'snow': tree_snow, 'pine': tree_pine}


# --------------------------------------------------------------------------
# palette
# --------------------------------------------------------------------------

def make_palette(colours):
    """A .vox palette carrying ``colours`` at indices 1.., the MagicaVoxel default
    behind them. Returns (palette, {rgb: index})."""
    index = {}
    pal = []
    for rgb in colours:
        if rgb not in index:
            index[rgb] = len(pal) + 1
            pal.append(tuple(rgb) + (255,))
    for entry in voxlib.PALETTE:
        if len(pal) >= 256:
            break
        pal.append(entry)
    return pal[:256], index


def build(key):
    shape, colour = TREES[key]
    ramp = COLOURS[colour]
    pal, index = make_palette(list(ramp) + COLOURS['snow'] + BARK + ROOT)
    return SHAPES[shape](ramp, index), pal


def main():
    p = argparse.ArgumentParser(description=__doc__,
                                formatter_class=argparse.RawDescriptionHelpFormatter)
    p.add_argument('--only', action='append', default=[], help='build just these keys')
    p.add_argument('--force', action='store_true', help='overwrite existing VOX files')
    p.add_argument('-o', '--out', help='output dir (default Resources/Remastered/Obstacles/vox)')
    args = p.parse_args()

    root = voxlib.workspace_root()
    out_dir = args.out or voxlib.obstacles_vox_dir(root)
    if not os.path.isdir(out_dir):
        os.makedirs(out_dir)

    keys = args.only or sorted(TREES)
    for key in keys:
        if key not in TREES:
            raise SystemExit('unknown tree %r, expected one of %s' % (key, ', '.join(sorted(TREES))))
        path = os.path.join(out_dir, key + '.vox')
        if os.path.isfile(path) and not args.force:
            print('  skip  %-20s exists (use --force)' % key)
            continue
        g, pal = build(key)
        xs, ys, zs = np.nonzero(g)
        voxels = list(zip(xs.tolist(), ys.tolist(), zs.tolist(), g[xs, ys, zs].tolist()))
        voxlib.write_vox(path, g.shape, voxels, palette=pal)
        print('%-20s SIZE %2dx%2dx%-3d  x %2d..%2d y %2d..%2d z ..%2d  %5d voxels  -> %s'
              % (key, g.shape[0], g.shape[1], g.shape[2],
                 xs.min(), xs.max(), ys.min(), ys.max(), zs.max(), len(voxels), path))


if __name__ == '__main__':
    main()
