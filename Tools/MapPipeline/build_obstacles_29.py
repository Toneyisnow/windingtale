"""Build chapter 29's three dragons -- the heads of the fortress' defence centre at
the top of the map -- as one obstacle, and write the obstacle entry that clears
their tiles.

    dragon_heads_29   7 x 13   the red, grey and blue dragons lying side by side
                               down the top corridor, their bodies coming in over
                               the top edge of the map (tiles 13..19 x 1..12, the
                               heads on 14..18 x 13 and the chins on 15..17 x 14)

**The model is lifted out of the art, not drawn.** The map PNG is compared,
pixel by pixel, with the ground the dragons lie on (FILLS below); what differs
-- and is not the head's drop shadow on the floor -- is dragon. Each pixel is
given to the dragon whose hue it carries (red, grey, blue; the black outlines,
the brown spines and horns go to the nearest of them), and each dragon becomes a
rounded relief over its own outline: a column of voxels per pixel, as tall as
the pixel lies deep inside that dragon (a quarter-ellipse profile), coloured
with the pixel. The bodies stand BODY_HEIGHT voxels at the spine and the heads
rise to HEAD_HEIGHT -- a creature's height: one tile, 24 voxels (the tents of
chapter 13 were raised one tile "the height of a creature"). The spines and
horns stand SPIKE_LIFT above the surface they grow from.

**The faces are shaped, the same way on all three** (HEADS). Below the eyes the
nose and mouth drop to SNOUT_TOP and slope on down to SNOUT_CHIN at the chin, so
the brow stands over them as a dark step facing the camera, and the eyes are
carved on that step at the size the art paints them -- "\\ /" from the front,
outer ends high, inner ends low, fading from a pale core to darker ends in the
art's own colours -- in a black socket rim (made bigger to match the art on the
user's word, 2026-11-01). The eyes painted on top
are darkened so the carved ones are the only eyes. (The user's call, 2026-11-01:
the snouts were too high, the eyes not fierce enough, and the red dragon's face
wrong: its skin highlights passed for horn brown, so a fifth of its face stood up
as spikes; is_spike now wants green a good half of red.)

**The footprint is the whole 7 x 13 block.** ObstaclesLayer measures a model's
footprint off its bounding box and centres it there, so the box has to span the
seven columns even though the horns reach only 13.7..19.3: two anchor voxels at
z = 0 on the outer columns (inside the floor tile, which the model's base is
seated in) hold it open. The chins run one or two pixels into row 14; they are
cut, so the box is exactly 13 rows and its centre the art's.

**The ground under them** (FILLS) is the corridor the art hides: the blue channel
(163) down to row 6, the wall line closing it on row 7 (189), the two wall lines
coming in from both sides on rows 8 and 11 joined straight across (78), grey
floor (145) everywhere else. On the edge columns 13 and 19, where the floor shows
beside the bodies, the fill is the tile matching those pixels best
(prop_obstacles.ground_fill, 88..97%). The ShapeMatrix keeps the dragon tiles,
so the battle still reads them as Gap.

Model space is the obstacle convention (formats.md): x runs left to right along
the map, y = 0 is the map row nearest the camera, z is up -- map pixel row py
lands on y = H - 1 - py.

    python build_obstacles_29.py                # model + obstacles/obstacles_29_dragons.json
    python build_obstacles_29.py --mask out/dragons_mask.png
    python vox_preview.py ../../Resources/Remastered/Obstacles/vox/dragon_heads_29.vox --scale 4
"""

import argparse
import json
import os
from collections import Counter, OrderedDict

import numpy as np
from PIL import Image
from scipy import ndimage

import voxlib
from build_trees import make_palette

TILE = voxlib.TILE
CHAPTER = '29'
KEY = 'dragon_heads_29'

# The block the model stands on (1-based tiles) and the rows cleaned with it.
X0, COLS = 13, 7
Y0, ROWS = 1, 13
CLEAR_ROWS = 14

BODY_HEIGHT = 16
HEAD_HEIGHT = 24
SPIKE_LIFT = 4
# Where the bodies give way to the heads, in map pixels from the top of the block:
# the horns start on row 10, the brows on row 11.
HEAD_FROM_PY = 9 * TILE
HEAD_FULL_PY = 10 * TILE + 12
# The quarter-ellipse profile reaches full height this many pixels inside a dragon.
ROUND_PX = 14.0

# (x, y) -> tile drawn under the dragons. Absent tiles inside the clear block keep
# their own id (they are ordinary floor already).
EDGE_FILLS = {
    13: {1: 91, 2: 91, 3: 91, 4: 91, 5: 91, 6: 95, 7: 189, 8: 69, 9: 71, 10: 79, 11: 69},
    19: {1: 88, 2: 88, 3: 88, 4: 88, 5: 88, 6: 92, 7: 189, 8: 70, 9: 68, 10: 76, 11: 70},
}


def interior_fill(y):
    if y <= 6:
        return 163
    if y == 7:
        return 189
    if y in (8, 11):
        return 78
    return 145


def fills(matrix):
    """{(x, y): fill tile} for every dragon tile of the clear block."""
    out = OrderedDict()
    for y in range(Y0, Y0 + CLEAR_ROWS):
        for x in range(X0, X0 + COLS):
            painted = matrix[x - 1][y - 1]
            if not is_dragon_tile(painted):
                continue
            if x in EDGE_FILLS and y in EDGE_FILLS[x]:
                out[(x, y)] = EDGE_FILLS[x][y]
            else:
                out[(x, y)] = interior_fill(y)
    return out


def is_dragon_tile(tile):
    return tile == 228 or (272 <= tile <= 382 and tile % 2 == 0)


def tile_image(root, tile, cache):
    if tile not in cache:
        path = os.path.join(voxlib.shape_panel_dir(root, CHAPTER), voxlib.tile_png_name(CHAPTER, tile))
        cache[tile] = np.asarray(Image.open(path).convert('RGB'), dtype=np.int32)
    return cache[tile]


def load_block(root, matrix, fill):
    """(art, ground) as H x W x 3 arrays over the model's block plus the chin row."""
    art = np.asarray(Image.open(voxlib.map_png_path(root, CHAPTER)).convert('RGB'), dtype=np.int32)
    px0, py0 = (X0 - 1) * TILE, (Y0 - 1) * TILE
    art = art[py0:py0 + CLEAR_ROWS * TILE, px0:px0 + COLS * TILE]
    ground = np.zeros_like(art)
    cache = {}
    for y in range(Y0, Y0 + CLEAR_ROWS):
        for x in range(X0, X0 + COLS):
            tile = fill.get((x, y), matrix[x - 1][y - 1])
            ty, tx = (y - Y0) * TILE, (x - X0) * TILE
            ground[ty:ty + TILE, tx:tx + TILE] = tile_image(root, tile, cache)
    return art, ground


# The blue channel's own colours, which the art shades beside the bodies.
CHANNEL_COLOURS = [(104, 136, 200), (84, 112, 180)]


def mask_shape(art):
    return art.shape[:2]


def dragon_mask(art, ground):
    """Pixels that are dragon: unlike the ground, and not the heads' shadow on it."""
    differs = np.abs(art - ground).sum(axis=2) > 24
    # A shadow is the floor darkened evenly: every channel the same fraction of the
    # ground's, that fraction well short of 1. A dragon pixel is not (outlines are
    # near black, the grey dragon is bluish-dark with highlights).
    g = np.maximum(ground, 1).astype(np.float64)
    ratio = art / g
    even = (ratio.max(axis=2) - ratio.min(axis=2)) < 0.08
    dim = (ratio.mean(axis=2) > 0.45) & (ratio.mean(axis=2) < 0.9)
    shadow = even & dim
    # Only open floor carries a shadow: the edge columns beside the bodies and the
    # floor below the heads. Inside, the ground is hidden and the grey dragon's own
    # scales would pass for shadow on it.
    open_floor = np.zeros(shadow.shape, dtype=bool)
    open_floor[:, :TILE] = True
    open_floor[:, -TILE:] = True
    open_floor[HEAD_FROM_PY + TILE:] = True
    shadow &= open_floor
    # The blue channel's floor and its shading beside the bodies.
    channel = np.zeros(mask_shape(art), dtype=bool)
    for rgb in CHANNEL_COLOURS:
        channel |= (art == rgb).all(axis=2)
    # On the edge columns the floor beside the bodies is not always the fill tile's
    # own art (228 is 88 with the blue body over it, but only 88% of it): a pixel
    # there in one of the floor's colours is floor.
    floor_rgb = set(map(tuple, ground[:, :TILE].reshape(-1, 3))) | set(map(tuple, ground[:, -TILE:].reshape(-1, 3)))
    floor = np.zeros(channel.shape, dtype=bool)
    for rgb in floor_rgb:
        floor |= (art == rgb).all(axis=2)
    floor[:, TILE:-TILE] = False
    mask = differs & ~shadow & ~channel & ~floor
    # Keep the dragons, drop specks.
    labels, n = ndimage.label(mask)
    sizes = ndimage.sum(mask, labels, range(1, n + 1))
    keep = np.zeros(n + 1, dtype=bool)
    keep[1:] = sizes >= 200
    mask = keep[labels]
    mask = ndimage.binary_fill_holes(mask)
    # A pale grey rim round the outline is floor showing through where the fill
    # tile is only a guess at the hidden ground: peel it off from the outside in.
    bright = art.max(axis=2)
    pale = (bright >= 110) & (bright - art.min(axis=2) < 30)
    for _ in range(4):
        rim = mask & ~ndimage.binary_erosion(mask)
        mask &= ~(rim & pale)
    return mask


def classify(art, mask):
    """0 red, 1 grey, 2 blue for every mask pixel; -1 off the mask."""
    r, g, b = art[..., 0], art[..., 1], art[..., 2]
    bright = art.max(axis=2)
    spread = bright - art.min(axis=2)
    seed = np.full(mask.shape, -1)
    dark_or_brown = (bright < 50) | ((r > b + 30) & (g > b + 10) & (r < g + 90) & (g > 60))
    seed[(r > g + 40) & (r > b + 40) & ~dark_or_brown] = 0
    # Grey: the mid greys of its scales -- not the near-black outlines round the
    # other two, nor the white of the eyes.
    seed[(spread < 30) & (bright >= 70) & (bright < 180)] = 1
    seed[(b > r + 60) & (b > g + 30) & (bright < 230)] = 2
    seed[~mask] = -1
    owner = nearest_seed(seed, mask)
    # Islands -- the blue-white eyes of the grey dragon, the white eyes of the blue
    # one -- belong to the dragon round them.
    for d in range(3):
        labels, n = ndimage.label(owner == d)
        sizes = ndimage.sum(owner == d, labels, range(1, n + 1))
        for i, size in enumerate(sizes):
            if size < 400:
                owner[labels == i + 1] = -1
    return nearest_seed(owner, mask)


def nearest_seed(seed, mask):
    """Every mask pixel without a seed (outlines, spines, horns) takes the nearest one."""
    _, (iy, ix) = ndimage.distance_transform_edt(seed < 0, return_indices=True)
    owner = seed[iy, ix]
    owner[~mask] = -1
    return owner


def is_spike(art):
    """The brown spines down the backs and the horns round the heads: (104, 72, 36)
    down to (48, 28, 4), green a good half of red. Not the red dragon's own skin,
    whose highlights (144, 60, 24) keep green under half of red."""
    r, g, b = art[..., 0], art[..., 1], art[..., 2]
    return (r >= 40) & (g * 100 >= r * 55) & (g * 100 <= r * 85) & (b * 2 <= g)


# The faces, in block pixels (px from the left of column 13, py from the top of row 1).
# eye_py is the last row of the painted eyes: the brow ends there, and below it the
# nose and mouth run down to the chin at chin_py. eyes are the centres of the two
# painted eyes. dark is the shade the brow step is painted; glow the eyes' colours
# from the art, (ends, body, core) -- the painted slits fade from a pale core to
# darker ends.
HEADS = {
    0: dict(eye_py=262, chin_py=301, eyes=(43.5, 57.5), dark=(72, 4, 0),
            glow=((164, 0, 0), (212, 0, 0), (255, 64, 40))),
    1: dict(eye_py=273, chin_py=312, eyes=(79.0, 93.0), dark=(36, 52, 52),
            glow=((92, 124, 188), (148, 184, 228), (200, 224, 252))),
    2: dict(eye_py=259, chin_py=298, eyes=(112.5, 126.5), dark=(16, 36, 100),
            glow=((172, 192, 192), (200, 220, 220), (232, 248, 248))),
}
# The snout: SNOUT_TOP just under the eyes, falling to SNOUT_CHIN at the chin, so the
# brow stands HEAD_HEIGHT - SNOUT_TOP voxels over it as a step facing the camera.
SNOUT_TOP = 15
SNOUT_CHIN = 9
# The eyes on that step, seen from the front as "\ /" and about the size the art
# paints them (8..9 pixels across, slanting the same way): each EYE_HALF pixels
# either side of its centre, the bottom of its outer end EYE_OUTER_Z high and of its
# inner end EYE_INNER_Z, EYE_THICK voxels tall, in a black socket rim -- two voxels
# of brow line over it, one under it and one past each end.
EYE_HALF = 5
EYE_OUTER_Z = 20
EYE_INNER_Z = 16
EYE_THICK = 3


def heights(art, owner):
    h = np.zeros(owner.shape)
    py = np.arange(owner.shape[0])[:, None]
    t = np.clip((py - HEAD_FROM_PY) / float(HEAD_FULL_PY - HEAD_FROM_PY), 0.0, 1.0)
    top = BODY_HEIGHT + (HEAD_HEIGHT - BODY_HEIGHT) * t
    for d in range(3):
        inside = owner == d
        depth = ndimage.distance_transform_edt(inside)
        u = np.clip(depth / ROUND_PX, 0.0, 1.0)
        profile = np.sqrt(1.0 - (1.0 - u) ** 2)
        face = HEADS[d]
        down = np.clip((py - face['eye_py']) / float(face['chin_py'] - face['eye_py']), 0.0, 1.0)
        snout = SNOUT_TOP + (SNOUT_CHIN - SNOUT_TOP) * down
        own_top = np.where(py > face['eye_py'], snout, top)
        h = np.where(inside, np.maximum(2.0, own_top * profile), h)
    spike = is_spike(art) & (owner >= 0)
    # A spine/horn pixel stands above the surface around it, not above its own
    # (lower, outline-side) depth.
    around = ndimage.grey_dilation(np.where(spike, 0, h), size=(5, 5))
    h = np.where(spike, np.minimum(HEAD_HEIGHT + SPIKE_LIFT, np.maximum(h, around) + SPIKE_LIFT), h)
    return np.rint(h).astype(int)


def face_paint(art, h, owner):
    """{(px, py, z): rgb} over the plain art colours: the brow step painted dark, the
    two slanted eyes glowing on it, and the painted eyes on top darkened so the
    carved ones are the only eyes."""
    paint = {}
    for d, face in HEADS.items():
        ey = face['eye_py']
        # The step is one dark shade: the art's own columns there are mostly the black
        # outlines of the eye sockets, which come out as stripes down it.
        for row in (ey - 1, ey):
            for px in np.nonzero(owner[row] == d)[0]:
                below = h[ey + 1, px] if owner[ey + 1, px] == d else 0
                for z in range(int(below), int(h[row, px])):
                    paint[(int(px), row, z)] = face['dark']
        for side, cx in enumerate(face['eyes']):
            ends, body, core = face['glow']
            for px in range(int(round(cx - EYE_HALF)) - 1, int(round(cx + EYE_HALF)) + 2):
                # 0 at the outer end, 1 at the inner one: the left eye runs right, the right one left.
                t = min(1.0, max(0.0, (px - (cx - EYE_HALF)) / (2.0 * EYE_HALF)))
                if side == 1:
                    t = 1.0 - t
                z0 = int(round(EYE_OUTER_Z + (EYE_INNER_Z - EYE_OUTER_Z) * t))
                rim = abs(px - cx) > EYE_HALF + 0.5
                along = abs(px - cx) / EYE_HALF
                for z in range(z0 - 1, z0 + EYE_THICK + 2):
                    if z >= h[ey, px]:
                        continue
                    if rim or z < z0 or z >= z0 + EYE_THICK:
                        rgb = (0, 0, 0)
                    elif along > 0.75:
                        rgb = ends
                    elif along < 0.5 and z == z0 + EYE_THICK // 2:
                        rgb = core
                    else:
                        rgb = body
                    paint[(px, ey, z)] = rgb
        # The painted eyes: the bright pixels on the brow's last rows, now its top.
        for row in range(ey - 2, ey + 2):
            for px in np.nonzero((owner[row] == d) & (art[row].max(axis=1) > 150))[0]:
                paint[(int(px), row, int(h[row, px]) - 1)] = face['dark']
    return paint


def build(root, matrix):
    fill = fills(matrix)
    art, ground = load_block(root, matrix, fill)
    mask = dragon_mask(art, ground)
    owner = classify(art, mask)
    h = heights(art, owner)

    W, H = COLS * TILE, ROWS * TILE
    # Quantise the dragon's colours into the palette.
    pts = art[mask]
    img = Image.fromarray(pts.reshape(-1, 1, 3).astype(np.uint8), 'RGB')
    q = img.quantize(colors=200, method=Image.Quantize.MEDIANCUT)
    qpal = q.getpalette()[:3 * 200]
    colours = [tuple(qpal[i:i + 3]) for i in range(0, len(qpal), 3)]
    paint = face_paint(art, h, owner)
    pal, index = make_palette(colours + [(150, 166, 164)] + sorted(set(paint.values())))
    qidx = np.asarray(q).reshape(-1)
    cidx = np.zeros(mask.shape, dtype=int)
    cidx[mask] = [index[colours[i]] for i in qidx]

    voxels = []
    ys, xs = np.nonzero(mask[:H])
    for py, px in zip(ys, xs):
        c = int(cidx[py, px])
        for z in range(int(h[py, px])):
            rgb = paint.get((int(px), int(py), z))
            voxels.append((int(px), H - 1 - int(py), z, index[rgb] if rgb else c))
    # The anchors holding the box open to the full seven columns, sunk in the floor.
    floor = index[(150, 166, 164)]
    anchor_y = H - 1 - (9 * TILE + 12)
    voxels.append((0, anchor_y, 0, floor))
    voxels.append((W - 1, anchor_y, 0, floor))
    zmax = max(v[2] for v in voxels) + 1
    return (W, H, zmax), voxels, pal, fill, mask, owner


def obstacle_entry(fill):
    clear = [OrderedDict([('X', x), ('Y', y), ('Cols', 1), ('Rows', 1), ('Fill', f)])
             for (x, y), f in fill.items()]
    return OrderedDict([
        ('_comment', ['Chapter 29\'s three dragons, merged into obstacles_29.json by prop_obstacles.py --with.',
                      'Generated by build_obstacles_29.py: one 7 x 13 model over the dragon tiles, every',
                      'dragon tile cleared to the ground the art hides (see FILLS there).']),
        ('Obstacles', [OrderedDict([
            ('Id', 0),
            ('DefinitionKey', KEY),
            ('Position', OrderedDict([('X', X0), ('Y', Y0)])),
            ('Clear', clear),
        ])]),
    ])


def main():
    ap = argparse.ArgumentParser(description=__doc__.split('\n\n')[0])
    ap.add_argument('--root')
    ap.add_argument('--mask', help='also write the dragon mask (coloured by dragon) to this PNG')
    args = ap.parse_args()
    root = args.root or voxlib.workspace_root()

    with open(voxlib.chapter_json_path(root, CHAPTER), encoding='utf-8-sig') as f:
        matrix = json.load(f)['ShapeMatrix']

    size, voxels, pal, fill, mask, owner = build(root, matrix)
    out = os.path.join(voxlib.obstacles_vox_dir(root), KEY + '.vox')
    voxlib.write_vox(out, size, voxels, pal)
    print('wrote %s  SIZE %r  %d voxels' % (out, size, len(voxels)))

    entry = os.path.join(os.path.dirname(os.path.abspath(__file__)), 'obstacles', 'obstacles_29_dragons.json')
    with open(entry, 'w', encoding='utf-8') as f:
        json.dump(obstacle_entry(fill), f, indent=2, ensure_ascii=False)
        f.write('\n')
    print('wrote %s  (%d tiles cleared)' % (entry, len(fill)))
    print('fills:', dict(Counter(fill.values())))

    if args.mask:
        tint = np.array([[200, 60, 40], [120, 120, 120], [40, 60, 200]])
        img = np.zeros(mask.shape + (3,), dtype=np.uint8)
        img[owner >= 0] = tint[owner[owner >= 0]]
        Image.fromarray(img).resize((mask.shape[1] * 4, mask.shape[0] * 4), Image.NEAREST).save(args.mask)
        print('wrote', args.mask)


if __name__ == '__main__':
    main()
