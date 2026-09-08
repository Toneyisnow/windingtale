"""Build the two tents chapter 13 adds.

Run once to produce, in Resources/Remastered/Obstacles/vox/:

    tent_gray_1     3 x 3   the gray-green tent (tiles 240..254)
    tent_blue_1     3 x 3   the blue-and-white tent (tiles 224..238, 259/233/260)

They are one shape in two colourways, the way the art paints them: a round
dome roof in alternating dark / light stripes standing over a cloth skirt that
hangs in vertical pleats and flares out at the ground, with a dark door
opening in the front. The dome overhangs the skirt, and a dark band under its
rim is the shadow line the art draws there.

The art shows the tent in elevation over three rows; the model stands on all
three tiles (the footprint is its plan) and is 46 voxels tall -- a little
under two tiles, lower than the huts (84) as a tent should be. The colours
are the art's own, carried as the model's palette because the MagicaVoxel
default palette turns the gray-green cloth into plain gray.

Model space is the obstacle convention (formats.md): x runs left to right
along the map, y = 0 is the map row nearest the camera (the front of the
footprint) and y = 71 the back, z is up. The light comes from the front-left.

    python build_obstacles_13.py            # both tents
    python build_obstacles_13.py --only tent_blue_1 --force
    python vox_preview.py ../../Resources/Remastered/Obstacles/vox/tent_gray_1.vox --scale 4
"""

import argparse
import math
import os

import voxlib
from build_trees import make_palette

TILE = voxlib.TILE
COLS = ROWS = 3
W = D = COLS * TILE                 # 72
H = 46
CX = CY = (W - 1) / 2.0             # 35.5

SKIRT_TOP = 23                      # z of the band under the dome's rim
SKIRT_R_BOTTOM = 30.0               # the pleats flare out at the ground
SKIRT_R_TOP = 25.5
DOME_R = 31.5                       # the rim overhangs the skirt
PLEATS = 16
STRIPES = 12                        # alternating dark / light around the dome

DOOR_HALF_W = 7                     # the opening in the front, half its width
DOOR_H = 18
DOOR_RECESS = 3

# Sampled off the tile art (ShapePanel13). Each colourway names the same
# roles, so build() is written once.
COLOURWAYS = {
    'tent_gray_1': dict(
        stripe_dark=(72, 92, 92), stripe_dark_shade=(44, 64, 64), rim=(36, 52, 52),
        stripe_light=(128, 148, 128), stripe_light_hi=(148, 168, 152),
        stripe_light_shade=(104, 116, 96),
        cloth=(128, 148, 128), cloth_hi=(148, 168, 152), cloth_mid=(116, 132, 112),
        cloth_shade=(104, 116, 96), cloth_dark=(56, 80, 80),
        door=(0, 0, 0), door_edge=(24, 40, 40)),
    'tent_blue_1': dict(
        stripe_dark=(68, 96, 164), stripe_dark_shade=(56, 84, 152), rim=(40, 68, 136),
        stripe_light=(172, 192, 192), stripe_light_hi=(192, 208, 208),
        stripe_light_shade=(136, 156, 156),
        cloth=(172, 192, 192), cloth_hi=(192, 208, 208), cloth_mid=(152, 172, 172),
        cloth_shade=(136, 156, 156), cloth_dark=(116, 136, 136),
        door=(0, 0, 0), door_edge=(24, 40, 40)),
}

PLEAT_CYCLE = ('cloth', 'cloth_hi', 'cloth', 'cloth_mid')
SHADE = {'cloth': 'cloth_shade', 'cloth_hi': 'cloth', 'cloth_mid': 'cloth_dark',
         'stripe_dark': 'stripe_dark_shade', 'stripe_light': 'stripe_light_shade'}
HIGHLIGHT = {'cloth': 'cloth_hi', 'cloth_mid': 'cloth', 'cloth_hi': 'cloth_hi',
             'stripe_dark': 'stripe_dark', 'stripe_light': 'stripe_light_hi'}


def lighting(dx, dy, d):
    """-1..1: how much a point on the tent's side faces the front-left light."""
    if d < 0.5:
        return 1.0
    return (-0.45 * dx - 0.85 * dy) / d


def build(colours):
    pal, index = make_palette(list(colours.values()))
    C = {role: index[rgb] for role, rgb in colours.items()}

    def shaded(role, dx, dy, d):
        light = lighting(dx, dy, d)
        if light < -0.35:
            role = SHADE.get(role, role)
        elif light > 0.6:
            role = HIGHLIGHT.get(role, role)
        return C[role]

    vox = {}

    # the skirt: a solid disk per layer, flaring towards the ground, coloured
    # in pleats around it
    for z in range(0, SKIRT_TOP):
        t = z / float(SKIRT_TOP - 1)
        r = SKIRT_R_BOTTOM + (SKIRT_R_TOP - SKIRT_R_BOTTOM) * t
        for x in range(W):
            for y in range(D):
                dx, dy = x - CX, y - CY
                d = math.hypot(dx, dy)
                if d > r:
                    continue
                ang = math.atan2(dy, dx)
                k = int(((ang + math.pi) / (2 * math.pi)) * PLEATS + 0.5) % PLEATS
                in_door = dy < 0 and abs(dx) <= DOOR_HALF_W and z < DOOR_H
                if in_door:
                    if d > r - DOOR_RECESS:
                        continue                       # the opening is set back
                    edge = abs(dx) > DOOR_HALF_W - 2 or z >= DOOR_H - 2
                    vox[(x, y, z)] = C['door_edge'] if edge else C['door']
                    continue
                vox[(x, y, z)] = shaded(PLEAT_CYCLE[k % len(PLEAT_CYCLE)], dx, dy, d)

    # the band under the rim: the widest layer, and dark all round
    for x in range(W):
        for y in range(D):
            dx, dy = x - CX, y - CY
            if math.hypot(dx, dy) <= DOME_R + 0.5:
                vox[(x, y, SKIRT_TOP)] = C['rim']

    # the dome: a flattened cap in alternating stripes that meet at the pole
    dome_h = H - SKIRT_TOP - 1
    for z in range(SKIRT_TOP + 1, H):
        t = (z - SKIRT_TOP) / float(dome_h)
        r = DOME_R * math.sqrt(max(0.0, 1.0 - t ** 2.4))
        if z == H - 1:
            r = max(r, 2.5)
        for x in range(W):
            for y in range(D):
                dx, dy = x - CX, y - CY
                d = math.hypot(dx, dy)
                if d > r:
                    continue
                ang = math.atan2(dy, dx) + math.pi / STRIPES   # a stripe centred on the front
                s = int(((ang + math.pi) / (2 * math.pi)) * STRIPES) % STRIPES
                role = 'stripe_light' if s % 2 == 0 else 'stripe_dark'
                if t > 0.8 and role == 'stripe_light':
                    role = 'stripe_light_hi'                 # the crown catches the light
                vox[(x, y, z)] = shaded(role, dx, dy, d)

    voxels = [(x, y, z, c) for (x, y, z), c in sorted(vox.items())]
    return voxels, pal


def main():
    ap = argparse.ArgumentParser(description=__doc__.split('\n\n')[0])
    ap.add_argument('--root')
    ap.add_argument('--only', action='append', help='build just this key (repeatable)')
    ap.add_argument('--force', action='store_true')
    a = ap.parse_args()
    root = a.root or voxlib.workspace_root()
    out_dir = voxlib.obstacles_vox_dir(root)

    for key, colours in COLOURWAYS.items():
        if a.only and key not in a.only:
            continue
        path = os.path.join(out_dir, key + '.vox')
        if os.path.exists(path) and not a.force:
            print('exists, skipping (use --force):', path)
            continue
        voxels, pal = build(colours)
        xs = [v[0] for v in voxels]
        ys = [v[1] for v in voxels]
        zs = [v[2] for v in voxels]
        voxlib.write_vox(path, (W, D, H), voxels, palette=pal)
        print('wrote %s  SIZE (%d, %d, %d)  %d voxels  extent x %d..%d y %d..%d z %d..%d' % (
            path, W, D, H, len(voxels), min(xs), max(xs), min(ys), max(ys), min(zs), max(zs)))


if __name__ == '__main__':
    main()
