"""
chapter_bg_to_vox.py
--------------------

Build a battle-scene backdrop (``Resources/Remastered/BG/BG_<nn>.vox``) out of
the chapter's own remastered 3D map assets, so the fight happens in front of
scenery the player just walked through.

``BG_01.vox`` -- chapter 1's backdrop, the one the battle scene was framed around
-- is a 256 x 180 x 90 landscape of grass, water and hills. This builds models the
same size, but instead of inventing terrain it re-uses what the chapter map is
already made of:

* ground is tiled from that chapter's ``Shapes_<nn>`` tile models (grass,
  cobbled road, dirt), sampled down to ``TILE_OUT`` voxels per map tile;
* the buildings and trees are the chapter's own obstacle models
  (``blue_house_1``, ``tree_dark_green``, ...), downsampled by ``SCALE``.

Everything is authored in the same coordinate system as the rest of the
pipeline, which is also how the battle scene places the model:

  +X = right      +Y = away from the camera (model front is at Y = 0)  +Z = up

The battle camera sits just off the front edge near X = 109 and looks into the
scene, tilted down about 23 degrees, so only a wedge of the model is ever on
screen: roughly Y >= 35, nearer ground being below the frame. ``FIGHTER_ZONE`` is
the part of that wedge the two fighting sprites are drawn over -- every recipe
keeps it clear so the creatures always read against open ground.

A backdrop is a few of the chapter's things placed where they read, not its map
rebuilt: chapter 02 is grass and a cobbled road underfoot, one blue-roofed house
behind, a barrel beside it, a few pines. Crowding the frame with everything the
map holds only gives the fighters a wall to disappear against.

Usage
~~~~~

    python chapter_bg_to_vox.py 02       # -> Resources/Remastered/BG/BG_02.vox

Add a chapter by writing one ``recipe_<nn>()`` and listing it in ``RECIPES``.
"""

import argparse
import math
import os
import random
import sys

import numpy as np

_HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, os.path.join(_HERE, '..', 'MapPipeline'))

import voxlib  # noqa: E402  (needs the path above)


OUT_SIZE = (256, 180, 90)         # same envelope as chapter 1's BG_01
SCALE = 3                         # map voxels per backdrop voxel
TILE_OUT = voxlib.TILE // SCALE   # 8 backdrop voxels per map tile
GROUND_Z = 12                     # backdrop z of the ground surface
SOIL = (102, 68, 34)              # what the ground is packed with underneath
TILE_CONTRAST = 0.45              # how much of a tile's own grain survives

ROOT = voxlib.workspace_root(_HERE)

# The corridor the fighter sprites are drawn over, as (x0, x1, y0, y1) in
# backdrop voxels. The sprites stand around X 112..118 / Y 38, and the camera's
# sight line past them meets the ground near X 126 / Y 107 and leaves the model
# around X 145 / Y 180, so this band is what the creatures are seen against.
# Nothing taller than grass goes in it; props stand beside it, not in it.
#
# Note the screen is mirrored against the model: the battle camera looks up +Y
# with +Z up, and Unity is left-handed, so the model's +X runs to the LEFT of
# the frame. Large X is screen left, small X is screen right.
FIGHTER_ZONE = (95, 170, 20, 150)


# --------------------------------------------------------------------------- #
# palette                                                                     #
# --------------------------------------------------------------------------- #

class PaletteBook(object):
    """One 255-colour palette for the whole backdrop.

    The map models do not agree on a palette: the houses were built through
    ``voxlib`` so they carry the MagicaVoxel default, while the trees, the hut
    and the barrels were traced from the 2D art and carry their own. Colour
    index 5 is a green in one file and a brown in the next, so everything is
    remapped to this book on the way in -- and only then do the indices mean the
    same thing.
    """

    def __init__(self):
        self.colours = []          # index i (1-based) -> (r, g, b, a)
        self._by_rgb = {}

    def index(self, rgb):
        key = tuple(int(v) for v in rgb[:3])
        hit = self._by_rgb.get(key)
        if hit is not None:
            return hit
        if len(self.colours) < 255:
            self.colours.append(key + (255,))
            self._by_rgb[key] = len(self.colours)
            return len(self.colours)
        best, best_d = 1, None
        for i, c in enumerate(self.colours):
            d = sum((c[k] - key[k]) ** 2 for k in range(3))
            if best_d is None or d < best_d:
                best, best_d = i + 1, d
        self._by_rgb[key] = best
        return best

    def lut(self, palette):
        """model colour index -> book colour index, as a 256-entry array."""
        out = np.zeros(256, np.uint8)
        for i in range(1, 256):
            out[i] = self.index(palette[i - 1])
        return out

    def palette(self):
        pal = list(self.colours)
        while len(pal) < 256:
            pal.append((0, 0, 0, 255))
        return pal


BOOK = PaletteBook()


# --------------------------------------------------------------------------- #
# model loading / downsampling                                                #
# --------------------------------------------------------------------------- #

_model_cache = {}


def _dense(model):
    """A model as a dense (sx, sy, sz) uint8 array of palette indices."""
    a = np.zeros(model.size, np.uint8)
    v = np.asarray(model.voxels, np.int32)
    a[v[:, 0], v[:, 1], v[:, 2]] = v[:, 3].astype(np.uint8)
    return a


def _centre_first(scale):
    """Block offsets ordered from the middle of the block outwards."""
    mid = (scale - 1) / 2.0
    offs = [(dx, dy, dz)
            for dx in range(scale) for dy in range(scale) for dz in range(scale)]
    offs.sort(key=lambda o: (o[0] - mid) ** 2 + (o[1] - mid) ** 2 + (o[2] - mid) ** 2)
    return offs


def downsample(model, scale=SCALE):
    """Shrink a map model by ``scale``: every block takes the colour of its
    innermost filled voxel.

    Voting on the commonest colour of a block eats everything thin -- the cross
    on a church roof, window frames, tree trunks -- and voting on the rarest is
    worse: one stray highlight repaints a whole wall and a pine comes out a
    white pillar. Sampling from the middle of the block outwards leaves flat
    areas exactly as they were, and a block is filled whenever anything in it
    was, so thin work survives in its own colour instead of someone else's.
    """
    a = _dense(model)
    pad = [(-s) % scale for s in a.shape]
    if any(pad):
        a = np.pad(a, [(0, p) for p in pad])
    out = np.zeros(tuple(s // scale for s in a.shape), np.uint8)
    for dx, dy, dz in _centre_first(scale):
        sub = a[dx::scale, dy::scale, dz::scale]
        take = (out == 0) & (sub > 0)
        out[take] = sub[take]
    return out


def obstacle(key, scale=SCALE):
    """One of the chapter map's obstacle models, at backdrop scale.

    Pass a smaller ``scale`` to bring a model up in the world: 2 instead of the
    usual 3 makes it half again as big, and does it by throwing away less of the
    original rather than by stretching what is left, so the model keeps its
    detail at the larger size.
    """
    hit = _model_cache.get((key, scale))
    if hit is None:
        path = os.path.join(voxlib.obstacles_vox_dir(ROOT), key + '.vox')
        model = voxlib.read_vox(path)
        hit = BOOK.lut(model.palette)[downsample(model, scale)]
        _model_cache[(key, scale)] = hit
    return hit


def tile_surface(nn, tile_id):
    """(colour, lift) for one map tile, as TILE_OUT x TILE_OUT arrays.

    The ground does not go through ``downsample``. A map tile is a fine speckle
    of a dozen greens, browns and greys, drawn to be read at 24 px; keeping any
    one voxel out of each 3x3 block turns that speckle into coarse noise that
    fights with the sprites in front of it. Averaging the block and snapping the
    result back onto the art's own palette keeps the tile's colour and loses
    only the grain. ``lift`` carries the voxel the tile models raise grass
    blades by (``voxlib.GRASS_LIFT``), squashed to 0 or 1, which is what is left
    of the texture.
    """
    path = os.path.join(voxlib.shapes_vox_dir(ROOT, nn),
                        voxlib.shape_vox_name(nn, tile_id))
    m = voxlib.read_vox(path)
    top = {}
    for x, y, z, c in m.voxels:
        if top.get((x, y), (-1, 0))[0] < z:
            top[(x, y)] = (z, c)
    whole = [m.palette[c - 1][:3] for _z, c in top.values()]
    tile_mean = [sum(ch) / float(len(whole)) for ch in zip(*whole)]

    colour = np.zeros((TILE_OUT, TILE_OUT), np.uint8)
    lift = np.zeros((TILE_OUT, TILE_OUT), np.uint8)
    for i in range(TILE_OUT):
        for j in range(TILE_OUT):
            # Average a window a little wider than the block: the 24px art
            # was drawn to be dithered, and a 3x3 mean still jitters enough to
            # read as static from the battle camera.
            block = [top.get((i * SCALE + u, j * SCALE + v))
                     for u in range(-1, SCALE + 1) for v in range(-1, SCALE + 1)]
            block = [b for b in block if b]
            if not block:
                continue
            rgb = [m.palette[c - 1][:3] for _z, c in block]
            mean = [sum(ch) / float(len(rgb)) for ch in zip(*rgb)]
            # ...and pull that mean most of the way back to the tile's overall
            # colour. Cobblestones are drawn a few pixels across, which is below
            # what one backdrop voxel can hold; left alone they alias into a
            # checkerboard that crawls across the whole square.
            mean = [TILE_CONTRAST * mn + (1 - TILE_CONTRAST) * tm
                    for mn, tm in zip(mean, tile_mean)]
            colour[i, j] = BOOK.index(voxlib.palette_rgb(mean))
            lift[i, j] = 1 if any(z > voxlib.GROUND_Z for z, _c in block) else 0
    return colour, lift


# --------------------------------------------------------------------------- #
# the canvas                                                                  #
# --------------------------------------------------------------------------- #

class Backdrop(object):
    def __init__(self, size=OUT_SIZE):
        self.size = size
        self.a = np.zeros(size, np.uint8)
        self.ground = np.full(size[:2], GROUND_Z, np.int16)

    # -- terrain ---------------------------------------------------------- #

    def undulate(self, amplitude=2.0, wavelength=90.0, seed=7):
        """Gentle rolling ground, so the field is not a drawing board."""
        rnd = random.Random(seed)
        phase = [rnd.uniform(0, 2 * math.pi) for _ in range(4)]
        xs = np.arange(self.size[0])[:, None]
        ys = np.arange(self.size[1])[None, :]
        h = (np.sin(xs / wavelength + phase[0])
             * np.cos(ys / (wavelength * 1.3) + phase[1])
             + 0.5 * np.sin((xs + ys) / (wavelength * 0.7) + phase[2]))
        self.ground = np.round(GROUND_Z + amplitude * h).astype(np.int16)

    def rise(self, y0, height):
        """Lift everything behind ``y0``, to close the horizon off."""
        ys = np.arange(self.size[1])[None, :]
        t = np.clip((ys - y0) / float(max(1, self.size[1] - y0)), 0, 1)
        self.ground = self.ground + (t ** 2 * height).astype(np.int16)

    def flatten(self, x0, x1, y0, y1, margin=6):
        """Level a footprint (plus a margin) to its mean, for building on."""
        x0, x1 = max(0, x0 - margin), min(self.size[0], x1 + margin)
        y0, y1 = max(0, y0 - margin), min(self.size[1], y1 + margin)
        if x0 >= x1 or y0 >= y1:
            return GROUND_Z
        z = int(round(self.ground[x0:x1, y0:y1].mean()))
        self.ground[x0:x1, y0:y1] = z
        return z

    # -- painting --------------------------------------------------------- #

    def lay_ground(self, tiles, nn):
        """Tile the whole floor. ``tiles(tx, ty) -> tile id``."""
        cache = {}
        soil = BOOK.index(SOIL)
        nx = (self.size[0] + TILE_OUT - 1) // TILE_OUT
        ny = (self.size[1] + TILE_OUT - 1) // TILE_OUT
        for tx in range(nx):
            for ty in range(ny):
                tid = tiles(tx, ty)
                if tid not in cache:
                    cache[tid] = tile_surface(nn, tid)
                colour, lift = cache[tid]
                # Eight voxels of a 24px tile is a small pattern, and laying the
                # same eight down over and over reads as a checkerboard from the
                # battle camera. Flipping each copy breaks the repeat up.
                flip = random.Random(tx * 31 + ty * 17).randrange(4)
                if flip & 1:
                    colour, lift = colour[::-1], lift[::-1]
                if flip & 2:
                    colour, lift = colour[:, ::-1], lift[:, ::-1]
                for i in range(TILE_OUT):
                    x = tx * TILE_OUT + i
                    if x >= self.size[0]:
                        break
                    for j in range(TILE_OUT):
                        y = ty * TILE_OUT + j
                        if y >= self.size[1]:
                            break
                        c = colour[i, j]
                        if c == 0:
                            continue
                        top = int(self.ground[x, y]) + int(lift[i, j])
                        top = max(1, min(self.size[2] - 1, top))
                        self.a[x, y, :top] = soil
                        self.a[x, y, top] = c

    def stamp(self, dense, x, y, z=None):
        """Drop a downsampled model with its base on the ground at (x, y)."""
        sx, sy, sz = dense.shape
        if z is None:
            z = int(self.flatten(x, x + sx, y, y + sy)) + 1
        x0, y0, z0 = max(x, 0), max(y, 0), max(z, 0)
        x1 = min(x + sx, self.size[0])
        y1 = min(y + sy, self.size[1])
        z1 = min(z + sz, self.size[2])
        if x0 >= x1 or y0 >= y1 or z0 >= z1:
            return
        src = dense[x0 - x:x1 - x, y0 - y:y1 - y, z0 - z:z1 - z]
        dst = self.a[x0:x1, y0:y1, z0:z1]
        np.copyto(dst, src, where=src > 0)

    def write(self, path):
        xs, ys, zs = np.nonzero(self.a)
        cs = self.a[xs, ys, zs]
        voxels = list(zip(xs.tolist(), ys.tolist(), zs.tolist(), cs.tolist()))
        voxlib.write_vox(path, self.size, voxels, palette=BOOK.palette())
        return len(voxels)


def scatter_trees(bd, keys, boxes, spacing=11, jitter=4, seed=3, avoid=()):
    """Sprinkle tree models through ``boxes``, skipping the ``avoid`` rects."""
    rnd = random.Random(seed)
    models = [obstacle(k) for k in keys]
    placed = 0
    for (bx0, bx1, by0, by1, density) in boxes:
        for gx in range(bx0, bx1, spacing):
            for gy in range(by0, by1, spacing):
                if rnd.random() > density:
                    continue
                x = gx + rnd.randint(-jitter, jitter)
                y = gy + rnd.randint(-jitter, jitter)
                m = models[rnd.randrange(len(models))]
                cx, cy = x + m.shape[0] // 2, y + m.shape[1] // 2
                if any(ax0 <= cx <= ax1 and ay0 <= cy <= ay1
                       for (ax0, ax1, ay0, ay1) in avoid):
                    continue
                bd.stamp(m, x, y)
                placed += 1
    return placed


# --------------------------------------------------------------------------- #
# chapter 02 -- the village                                                   #
# --------------------------------------------------------------------------- #

# A backdrop is not the map. Chapter 02's map is a whole town of blue-roofed
# churches and cobbled streets, and rebuilding it behind the fighters only gives
# them a wall of roofs to disappear against. What it needs is a few of that
# chapter's things, placed where they read: the village's grass and cobbled road
# underfoot, one blue-roofed house standing behind, a barrel beside it, and a
# couple of pines to keep the horizon from being a bare line.

GRASS_02 = (99, 96, 100)       # plain grass tiles
ROAD_02 = 73                   # full cobblestone
EDGE_N_02 = 109                # grass with the cobble band on its far side
EDGE_S_02 = 110                # grass with the cobble band on its near side

# The house is built at SCALE 2 rather than 3, which makes it half again as big
# as everything else -- at the backdrop's own scale it read as a shed at the far
# end of a field rather than the village behind the fight.
HOUSE_02 = (128, 112)          # near-left corner of the blue-roofed house
HOUSE_SCALE_02 = 2
# The road runs across the scene rather than towards it, and sits where the
# camera's sight line past the fighters meets the ground -- put it any nearer and
# it lands under the frame, with the creatures left standing on grass.
ROAD_BAND_02 = (11, 14)        # ty0, ty1


def tiles_02(tx, ty):
    rnd = random.Random(tx * 977 + ty * 131 + 5)
    by0, by1 = ROAD_BAND_02
    if by0 <= ty < by1:
        return ROAD_02
    if ty == by0 - 1:
        return EDGE_N_02
    if ty == by1:
        return EDGE_S_02
    return GRASS_02[rnd.randrange(len(GRASS_02))]


def recipe_02(bd):
    bd.undulate(amplitude=2.0)
    bd.rise(y0=120, height=6)
    bd.lay_ground(tiles_02, '02')

    # X/Y is the near-left corner of the footprint in backdrop voxels, and every
    # model's facade is on its own low-Y side, which is the side the battle camera
    # is on. Remember large X is screen left.
    bd.stamp(obstacle('blue_house_4', HOUSE_SCALE_02), *HOUSE_02)
    bd.stamp(obstacle('barrel_group_1'), 206, 118)  # beside it, clear of the fighters

    scatter_trees(
        bd, ('tree_dark_green', 'tree_light_green'),
        boxes=[
            (218, 252, 136, 176, 0.5),   # past the house, on its far side
            (24, 120, 148, 176, 0.5),    # a few across the far side
            (208, 248, 40, 88, 0.5),     # one or two near the frame edges, for depth
            (4, 44, 40, 88, 0.5),
        ],
        avoid=(FIGHTER_ZONE,))


RECIPES = {'02': recipe_02}


# --------------------------------------------------------------------------- #
# CLI                                                                         #
# --------------------------------------------------------------------------- #

def build(chapter, out_dir=None):
    nn = voxlib.nn(chapter)
    if nn not in RECIPES:
        raise SystemExit('no backdrop recipe for chapter %s (have: %s)'
                         % (nn, ', '.join(sorted(RECIPES))))
    bd = Backdrop()
    RECIPES[nn](bd)
    out_dir = out_dir or os.path.join(ROOT, 'Resources', 'Remastered', 'BG')
    if not os.path.isdir(out_dir):
        os.makedirs(out_dir)
    path = os.path.join(out_dir, 'BG_%s.vox' % nn)
    n = bd.write(path)
    print('wrote %s: %d voxels, dims %dx%dx%d' % ((path, n) + OUT_SIZE))
    return path


def main(argv=None):
    p = argparse.ArgumentParser(
        prog='chapter_bg_to_vox.py',
        description='Build a chapter battle backdrop from that chapter map assets.')
    p.add_argument('chapter', help='chapter number, e.g. 02')
    p.add_argument('-o', '--out-dir', default=None)
    args = p.parse_args(argv if argv is not None else sys.argv[1:])
    build(args.chapter, args.out_dir)


if __name__ == '__main__':
    main()
