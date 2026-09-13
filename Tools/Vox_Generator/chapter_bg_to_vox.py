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

What each chapter picked, and why:

* 02, the village -- grass and a cobbled street, one blue-roofed house behind;
* 03, the crossing -- the timber bridge seen down its length, red railings
  converging on the far shore, open water either side;
* 04, the wood -- pines thick down both edges and along the horizon, bare earth
  trodden through the grass where the fight is;
* 05, the town -- the red cathedral to screen left, a blue hall to screen right,
  a cobbled street across, and the trees turning;
* 06, the harbour -- the cobbled quay behind a rail fence, cargo crates stacked
  on it, the basin's water to screen right, one red church standing to screen
  left;
* 07, the wild wood -- blue spruce down both sides and a broad mud track worn
  away from the camera into the trees;
* 08, the castle gate -- the wall across the whole horizon, the moat in front
  of it and the timber bridge crossing on the camera axis;
* 09, the statue avenue -- the paved way running away between two converging
  rows of knights and pillars, the monument at the end of it;
* 10, the fire cavern -- rock floor and burning pillars, closed in by cave
  walls instead of sky;
* 11, the terraced wood -- a ledge stepping up across the back with the red
  grove on top of it, spruce in clumps, a round pond to screen right;
* 12, the ravine -- the valley floor between stepped rock walls, red trees
  lining the track, a cave mouth black in the far wall;
* 13, the enemy camp -- trodden earth among pale pines, tents either side,
  sharpened log barricades, a timbered well and stumps;
* 14, the dry riverbed -- a bed of bare earth crossing the frame on the slant,
  pale pines along its banks, green groves and a terrace beyond.

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
TURF = 2                          # voxels of surface colour above the soil
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
    """One palette for the whole backdrop, squeezed to 255 colours at the end.

    The map models do not agree on a palette: the houses were built through
    ``voxlib`` so they carry the MagicaVoxel default, while the trees, the hut
    and the barrels were traced from the 2D art and carry their own. Colour
    index 5 is a green in one file and a brown in the next, so everything is
    remapped to this book on the way in -- and only then do the indices mean the
    same thing.

    The book takes every colour it is handed and only reckons with the 255 a VOX
    file can hold when the model is written. Capping it as it filled made the
    order things were painted in decide who got a real colour: the ground goes
    down before the buildings, so a few thousand tile means would take every
    slot and chapter 05's cathedral came out with grass-green walls.
    """

    def __init__(self):
        self.colours = []          # index i (1-based) -> (r, g, b, a)
        self._by_rgb = {}

    def index(self, rgb):
        key = tuple(int(v) for v in rgb[:3])
        hit = self._by_rgb.get(key)
        if hit is not None:
            return hit
        self.colours.append(key + (255,))
        self._by_rgb[key] = len(self.colours)
        return len(self.colours)

    def lut(self, palette):
        """model colour index -> book colour index, as a 256-entry array."""
        out = np.zeros(256, np.uint16)
        for i in range(1, 256):
            out[i] = self.index(palette[i - 1])
        return out

    def squeeze(self, counts, limit=255):
        """Reduce to ``limit`` colours: (lut, palette).

        Repeatedly folds the least-used colour into its nearest neighbour, so
        what is lost is always the colour fewest voxels are wearing -- a stray
        highlight on one roof tile goes before a field of grass does. ``counts``
        is how many voxels carry each colour, indexed the way the voxels are.
        """
        n = len(self.colours)
        rgb = np.array([c[:3] for c in self.colours], np.float64)
        w = np.asarray(counts, np.float64)[1:n + 1].copy()
        alive = w > 0
        target = np.arange(n)
        while alive.sum() > limit:
            live = np.nonzero(alive)[0]
            j = live[np.argmin(w[live])]
            others = live[live != j]
            k = others[np.argmin(((rgb[others] - rgb[j]) ** 2).sum(1))]
            w[k] += w[j]
            alive[j] = False
            target[target == j] = k
        keep = np.nonzero(alive)[0]
        slot = np.zeros(n, np.uint16)
        slot[keep] = np.arange(1, len(keep) + 1)
        lut = np.zeros(n + 1, np.uint16)
        lut[1:] = slot[target]
        pal = [self.colours[i] for i in keep]
        while len(pal) < 256:
            pal.append((0, 0, 0, 255))
        return lut, pal


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


def _mean_rgb(colours):
    """Average colours in linear light, not in the bytes they are stored as.

    The art is dithered: a grass tile is olive pixels mixed with bright green
    ones, and the eye adds those up as light. sRGB bytes are not light -- they
    are light raised to about 1/2.2 -- so averaging the bytes puts the result
    well below what the dither actually looks like, and a green field comes out
    the colour of dead grass. Squaring into light, averaging there and coming
    back gives the colour the tile reads as.
    """
    n = float(len(colours))
    lin = [sum((c[k] / 255.0) ** 2.2 for c in colours) / n for k in range(3)]
    return [255.0 * v ** (1 / 2.2) for v in lin]


def tile_surface(nn, tile_id, contrast=TILE_CONTRAST):
    """(colour, lift) for one map tile, as TILE_OUT x TILE_OUT arrays.

    The ground does not go through ``downsample``. A map tile is a fine speckle
    of a dozen greens, browns and greys, drawn to be read at 24 px; keeping any
    one voxel out of each 3x3 block turns that speckle into coarse noise that
    fights with the sprites in front of it. Averaging the block keeps the
    tile's colour and loses only the grain.

    ``lift`` is what survives of the tile's relief: each block's mean surface
    height against the tile's own, clamped to one voxel either way. Grass blades
    (``voxlib.GRASS_LIFT``) come out +1 as they always did, and the water
    running in the gaps between chapter 03's bridge planks comes out -1, so the
    deck is planked rather than merely painted with planks.
    """
    path = os.path.join(voxlib.shapes_vox_dir(ROOT, nn),
                        voxlib.shape_vox_name(nn, tile_id))
    m = voxlib.read_vox(path)
    top = {}
    for x, y, z, c in m.voxels:
        if top.get((x, y), (-1, 0))[0] < z:
            top[(x, y)] = (z, c)
    whole = [m.palette[c - 1][:3] for _z, c in top.values()]
    tile_mean = _mean_rgb(whole)
    zs = [z for z, _c in top.values()]
    base = max(set(zs), key=zs.count)      # the tile's own ground level

    colour = np.zeros((TILE_OUT, TILE_OUT), np.uint16)
    lift = np.zeros((TILE_OUT, TILE_OUT), np.int8)
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
            mean = _mean_rgb(rgb)
            # ...and pull that mean most of the way back to the tile's overall
            # colour. Cobblestones are drawn a few pixels across, which is below
            # what one backdrop voxel can hold; left alone they alias into a
            # checkerboard that crawls across the whole square.
            mean = [contrast * mn + (1 - contrast) * tm
                    for mn, tm in zip(mean, tile_mean)]
            # The mean of a dithered green is a green the art never uses, and
            # that is the point of averaging -- so it is kept, not snapped onto
            # some fixed ramp. Snapping it to the MagicaVoxel default palette
            # sent grass to (102, 102, 0) and turned every field olive, because
            # the nearest of six levels per channel to a mid green is the olive
            # the dither was mixing with. Rounding to a step of eight keeps the
            # backdrop inside its 255-colour book without that.
            # Clamped, because the rounding overshoots: a near-white mean --
            # chapter 10's embers -- rounds up to 256, which is not a byte.
            colour[i, j] = BOOK.index([min(255, int(round(v / 8.0)) * 8)
                                       for v in mean])
            dz = sum(z for z, _c in block) / float(len(block)) - base
            lift[i, j] = max(-1, min(1, int(round(dz))))
    return colour, lift


# --------------------------------------------------------------------------- #
# the canvas                                                                  #
# --------------------------------------------------------------------------- #

class Backdrop(object):
    def __init__(self, size=OUT_SIZE):
        self.size = size
        self.a = np.zeros(size, np.uint16)
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

    def terrace(self, x0, x1, y0, y1, z):
        """Set a rectangle of ground to an explicit height.

        ``flatten`` levels a patch to whatever it already averages, which is all
        a house needs. A bridge deck and the water under it are not variations on
        one field, though: they are two surfaces several voxels apart, and the
        recipe knows how far.
        """
        x0, x1 = max(0, x0), min(self.size[0], x1)
        y0, y1 = max(0, y0), min(self.size[1], y1)
        if x0 < x1 and y0 < y1:
            self.ground[x0:x1, y0:y1] = z

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

    def lay_ground(self, tiles, nn, contrast=TILE_CONTRAST, jumble=True):
        """Tile the whole floor. ``tiles(tx, ty) -> tile id``.

        ``contrast`` is how much of each tile's own grain survives; the default
        throws most of it away because cobbles and grass are drawn a couple of
        pixels across, below what one backdrop voxel can hold. Art drawn in
        wider bands -- chapter 03's bridge planking -- keeps its pattern at a
        higher setting, and needs to: the planks are what says "bridge". Where
        only one tile in a floor is like that -- chapter 08 planks a bridge
        across grass, chapter 10 sets embers glowing in the cave rock -- pass a
        function of the tile id instead of a number, so the one tile keeps its
        pattern without the rest of the ground turning to static.

        ``jumble`` mirrors each copy of a tile at random so a field does not
        read as one square repeated. Turn it off where the pattern has to carry
        across the seam: chapter 03's planks run the width of the bridge, and
        flipping every other tile chops them into patchwork.
        """
        cache = {}
        soil = BOOK.index(SOIL)
        nx = (self.size[0] + TILE_OUT - 1) // TILE_OUT
        ny = (self.size[1] + TILE_OUT - 1) // TILE_OUT
        for tx in range(nx):
            for ty in range(ny):
                tid = tiles(tx, ty)
                if tid not in cache:
                    c = contrast(tid) if callable(contrast) else contrast
                    cache[tid] = tile_surface(nn, tid, c)
                colour, lift = cache[tid]
                # Eight voxels of a 24px tile is a small pattern, and laying the
                # same eight down over and over reads as a checkerboard from the
                # battle camera. Flipping each copy breaks the repeat up.
                flip = random.Random(tx * 31 + ty * 17).randrange(4) if jumble else 0
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
                        # The camera looks along the ground at about 23 degrees,
                        # so the SIDE of a column shows nearly three times the
                        # area its top face does -- and every raised grass blade
                        # and every step in the terrain exposes one. Packing the
                        # column with bare soil right up to the surface turns a
                        # green field brown from the battle camera, so the top
                        # few voxels carry the surface colour down with them.
                        turf = max(0, top - TURF)
                        self.a[x, y, :turf] = soil
                        self.a[x, y, turf:top + 1] = c

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
        counts = np.bincount(self.a.ravel(), minlength=len(BOOK.colours) + 1)
        lut, palette = BOOK.squeeze(counts)
        xs, ys, zs = np.nonzero(self.a)
        cs = lut[self.a[xs, ys, zs]]
        voxels = list(zip(xs.tolist(), ys.tolist(), zs.tolist(), cs.tolist()))
        voxlib.write_vox(path, self.size, voxels, palette=palette)
        return len(voxels)


def building(bd, key, x, y, scale=SCALE):
    """Level the ground a model will stand on, and hand back its stamp.

    Order matters. ``stamp`` levels what it is about to stand on, but by then
    ``lay_ground`` has already laid the grass at the heights the ground used to
    have, and wherever that was above the new level it pokes through the bottom
    of the walls -- chapter 05's cathedral was standing knee deep in its own
    lawn. Levelling the site first, painting the ground, then standing the model
    on it puts the three in the order a builder would.

        cathedral = building(bd, 'red_cathedral_1', *CATHEDRAL_05)
        bd.lay_ground(tiles_05, '05')
        cathedral()
    """
    m = obstacle(key, scale)
    bd.flatten(x, x + m.shape[0], y, y + m.shape[1])
    return lambda: bd.stamp(m, x, y)


def scatter_trees(bd, keys, boxes, spacing=11, jitter=4, seed=3, avoid=(),
                  scale=SCALE, where=None):
    """Sprinkle models through ``boxes``, skipping the ``avoid`` rects.

    Trees are what it is nearly always handed, and what the spacing and jitter
    defaults are pitched for, but nothing in here is about trees: chapter 10
    strews its burning pillars through the cavern the same way.

    ``where(cx, cy)``, if given, has the last word on each spot. A rectangle is
    no use for keeping trees out of something that winds, like chapter 14's
    riverbed.
    """
    rnd = random.Random(seed)
    models = [obstacle(k, scale) for k in keys]
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
                if where is not None and not where(cx, cy):
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


# --------------------------------------------------------------------------- #
# chapter 03 -- the bridge                                                    #
# --------------------------------------------------------------------------- #

# Chapter 03's map is one long timber bridge on red trestles, crossing a lake
# too wide to see the far side of, with a rocky shore and a pine wood at the
# bottom of the map. There is nothing else on it -- the whole chapter is the
# crossing -- so the backdrop is the crossing seen down its length: planks
# running away from the camera, a railing either side converging on the far
# shore, open water past them.
#
# The bridge is the one thing here the tile art cannot carry on its own. The
# railings are painted into tiles 197..205 flat at deck level, so laying those
# tiles down gives red marks on the planking and no fence at all; the posts and
# rails are built as geometry instead, in the art's own reds.

WATER_03 = 173                        # open water
DECK_03 = 198                         # planking; one tile, so the planks line up
SHORE_03 = (177, 9, 1)                # the rock the bridge lands on
GRASS_03 = (3, 6, 13)                 # the meadow behind the shore

DECK_TX_03 = (8, 19)          # deck tile columns -> x 64..152, on the camera axis
SHORE_TY_03 = 19              # tile rows from here back are dry land
WATER_Z_03 = GROUND_Z - 7     # the lake, seven voxels below the deck
DECK_Z_03 = GROUND_Z + 1      # the planking, eight above the water

RAIL_RED_03 = (116, 32, 12)   # the posts, sampled off tile 200
RAIL_DARK_03 = (72, 4, 0)     # their shaded side, and the rails


def tiles_03(tx, ty):
    rnd = random.Random(tx * 977 + ty * 131 + 3)
    if ty >= SHORE_TY_03:
        if ty >= SHORE_TY_03 + 2 and rnd.random() < 0.6:
            return GRASS_03[rnd.randrange(len(GRASS_03))]
        return SHORE_03[rnd.randrange(len(SHORE_03))]
    if DECK_TX_03[0] <= tx < DECK_TX_03[1]:
        return DECK_03
    return WATER_03


def railing(bd, x, y0, y1, z, height=11, spacing=13, width=3):
    """A run of trestle posts and rails along +Y, just off a deck edge.

    The posts stand outside the planking rather than on it, which is where the
    art has them: over the water, where they are a red picket line against blue
    instead of dark red on brown.
    """
    post = BOOK.index(RAIL_RED_03)
    dark = BOOK.index(RAIL_DARK_03)
    for y in range(y0, y1, spacing):
        bd.a[x:x + width, y:y + 2, z - 4:z + height] = post
        bd.a[x:x + width, y + 1:y + 2, z - 4:z + height] = dark    # its far half
    for dz in (height - 2, height - 6):
        bd.a[x:x + width, y0:y1, z + dz] = post
        bd.a[x:x + width, y0:y1, z + dz - 1] = post
        bd.a[x:x + width, y0:y1, z + dz - 2] = dark


def recipe_03(bd):
    dx0, dx1 = DECK_TX_03[0] * TILE_OUT, DECK_TX_03[1] * TILE_OUT
    shore_y = SHORE_TY_03 * TILE_OUT

    # A lake is flat, so this one skips undulate: the deck and the water are two
    # levels set outright, and only the shore behind them climbs.
    bd.ground[:, :] = WATER_Z_03
    bd.terrace(dx0, dx1, 0, shore_y, DECK_Z_03)
    bd.terrace(0, bd.size[0], shore_y, bd.size[1], DECK_Z_03)
    bd.rise(y0=shore_y, height=10)
    bd.lay_ground(tiles_03, '03', contrast=1.0, jumble=False)

    railing(bd, dx0 - 3, 0, shore_y, DECK_Z_03 + 1)
    railing(bd, dx1, 0, shore_y, DECK_Z_03 + 1)

    # The pine wood from the bottom of the map, now on the far shore. Nothing
    # stands on the bridge itself -- the deck is what the fighters read against.
    scatter_trees(
        bd, ('tree_dark_green', 'tree_light_green'),
        boxes=[(0, 256, shore_y + 6, 172, 0.8)],
        spacing=9, jitter=6, seed=11)


# --------------------------------------------------------------------------- #
# chapter 04 -- the forest                                                    #
# --------------------------------------------------------------------------- #

# Chapter 04 has no buildings at all: twenty by twenty tiles of pine wood, with
# bare earth trodden through the grass where the paths run and a fence across
# the bottom. So the backdrop is trees and ground, and all the composition it
# has is how the trees are spaced -- thick down both edges to frame the fight,
# thinning towards the middle, and closing the horizon off at the back.
#
# The earth matters more than it looks. Two chapters running on plain grass read
# as the same field, and the scar the map wears down its middle puts a patch of
# brown exactly where the creatures are drawn.

GRASS_04 = (20, 44, 37)
DIRT_CORE_04 = (117, 110)              # bare earth
DIRT_MID_04 = (122, 143, 118, 105)     # earth with grass coming back through
DIRT_EDGE_04 = (119, 121, 126, 125, 123, 109, 112, 116)

SCAR_04 = (13.0, 14.0, 1.0, 0.55)      # centre tx, ty, and how far it spreads

PINES_04 = ('tree_dark_green', 'tree_light_green', 'tree_bright_green')


def tiles_04(tx, ty):
    rnd = random.Random(tx * 977 + ty * 131 + 4)
    cx, cy, kx, ky = SCAR_04
    # A round patch of earth reads as a crater. Bending the distance by a couple
    # of slow waves gives it the ragged edge of ground trodden bare by feet.
    d = math.hypot((tx - cx) * kx, (ty - cy) * ky)
    d += 1.6 * math.sin(ty * 0.9) + 0.9 * math.cos(tx * 1.3 + ty * 0.4)
    if d < 2.5:
        return DIRT_CORE_04[rnd.randrange(len(DIRT_CORE_04))]
    if d < 4.5:
        return DIRT_MID_04[rnd.randrange(len(DIRT_MID_04))]
    if d < 6.5:
        return DIRT_EDGE_04[rnd.randrange(len(DIRT_EDGE_04))]
    return GRASS_04[rnd.randrange(len(GRASS_04))]


def recipe_04(bd):
    bd.undulate(amplitude=3.0, wavelength=70.0, seed=4)
    bd.rise(y0=110, height=8)
    bd.lay_ground(tiles_04, '04')

    # The wood, thick enough at the back to be a wall of pines and thinning as it
    # comes forward, so the fight is in a clearing and not down a corridor.
    scatter_trees(
        bd, PINES_04,
        boxes=[
            (0, 256, 148, 176, 0.85),     # the far edge, closing the horizon
            (0, 88, 60, 176, 0.6),        # screen right
            (168, 256, 60, 176, 0.6),     # screen left
            (0, 72, 20, 60, 0.45),        # a few nearer, for depth
            (184, 256, 20, 60, 0.45),
        ],
        spacing=10, seed=14, avoid=(FIGHTER_ZONE,))

    # The two pines nearest the camera come in at scale 2, half again as big as
    # the wood behind them: a forest whose every tree is the same height is a row
    # of fence posts, and these two give the frame a foreground.
    bd.stamp(obstacle('tree_dark_green', 2), 44, 76)
    bd.stamp(obstacle('tree_bright_green', 2), 178, 80)


# --------------------------------------------------------------------------- #
# chapter 05 -- the town                                                      #
# --------------------------------------------------------------------------- #

# Chapter 05 is the biggest town in the game -- a red-roofed cathedral along the
# whole top of the map, a blue church beside it, mansions and halls below, all
# joined by cobbled streets, with the trees turning red. Only one of those can
# stand behind the fighters: put a second one there and the frame is a skyline,
# with the creatures read against masonry instead of ground.
#
# So the cathedral is off to screen left and a hall away on screen right, with
# the ground the creatures are actually seen against left as street and grass
# between them. A thatched hut sits between the two to say the town carries on.

GRASS_05 = (101,)
ROAD_05 = 100                  # cobblestone
EDGE_N_05 = 189                # grass with the cobbles on its far side
EDGE_S_05 = 184                # grass with the cobbles on its near side
ROAD_BAND_05 = (11, 14)        # ty0, ty1

CATHEDRAL_05 = (150, 108)      # near-left corner; 80 x 72 voxels at SCALE
HALL_05 = (12, 112)            # a blue-roofed hall away on screen right
HUT_05 = (60, 148)             # a thatched hut between the two


def tiles_05(tx, ty):
    rnd = random.Random(tx * 977 + ty * 131 + 5)
    by0, by1 = ROAD_BAND_05
    if by0 <= ty < by1:
        return ROAD_05
    if ty == by0 - 1:
        return EDGE_N_05
    if ty == by1:
        return EDGE_S_05
    return GRASS_05[rnd.randrange(len(GRASS_05))]


def recipe_05(bd):
    bd.undulate(amplitude=2.0, seed=5)
    bd.rise(y0=130, height=5)

    # The cathedral comes in at the ordinary scale, unlike chapter 02's house:
    # it is ten tiles wide to that house's six, and at 80 x 72 x 35 voxels it
    # ends exactly at the back of the model, so none of it is cut away.
    cathedral = building(bd, 'red_cathedral_1', *CATHEDRAL_05)
    hall = building(bd, 'blue_hall_2', *HALL_05)
    hut = building(bd, 'thatched_hut_1', *HUT_05)
    crate = building(bd, 'wooden_crate_1', 92, 118)

    bd.lay_ground(tiles_05, '05')

    cathedral()
    hall()
    hut()
    crate()

    scatter_trees(
        bd, ('tree_dark_red', 'tree_light_red', 'tree_blue', 'tree_dark_green'),
        boxes=[
            (232, 256, 108, 176, 0.6),   # past the cathedral, screen left
            (96, 150, 150, 176, 0.6),    # between the two roofs, closing the gap
            (0, 60, 152, 176, 0.55),     # along the far side, screen right
            (0, 40, 40, 104, 0.5),       # the frame edges, for depth
            (216, 256, 40, 104, 0.5),
        ],
        spacing=11, seed=9, avoid=(FIGHTER_ZONE,))


# --------------------------------------------------------------------------- #
# chapter 06 -- the harbour                                                   #
# --------------------------------------------------------------------------- #

# Chapter 06's map is a stone quay along the top with cargo crates stacked on
# it and the sea beyond, a rail fence separating it from the grass, and two
# red-roofed churches standing below on the turf among the autumn trees.
#
# Both churches together would wall the frame off the way chapter 05's would
# have, so only one stands: ``red_church_1``, the one with the cross over its
# door, away to screen left. What makes the picture a harbour is the rest of
# it -- the fence across, the cobbled quay behind it, the crates stacked on the
# stone and the basin's water opening out to screen right.
#
# The water has to come forward to be seen at all. Laid across the back of the
# model the way a lake would be, it is a blue thread on the horizon: the camera
# looks down at 23 degrees, so everything past y 120 is squeezed into the top
# fifth of the frame, and a flat thing put there gets no room. Brought up to the
# quay's own front edge instead, it opens out over the whole screen-right corner
# and reads as the harbour it is.

GRASS_06 = (103, 207, 230, 190)
QUAY_06 = 100                  # the harbour's cobbled stone
EDGE_06 = 189                  # grass with the quay's stone over its far side
SEA_06 = 275                   # the water in the basin

# The quay starts where chapter 02 puts its road, at the row the camera's sight
# line past the fighters meets the ground, and runs seven rows back before the
# water starts. It has to be that deep to be a quay: a narrower band of stone is
# a sliver on the horizon, and the crates standing on it look like they are
# standing on the harbour.
QUAY_TY_06 = 11                # tile rows from here back are paved quay...
BASIN_TX_06 = 9                # ...except below this column, which is basin
WATER_Z_06 = GROUND_Z - 5      # how far the water sits below the quay

CHURCH_06 = (148, 106)         # near-left corner; 80 x 64 voxels at SCALE

# The fence is painted into tiles 220..223 flat at ground level, the way chapter
# 03's railings were, so laying those tiles down would give brown marks on the
# turf and no fence at all. It is built as geometry instead, in the art's woods.
FENCE_WOOD_06 = (150, 112, 66)
FENCE_DARK_06 = (86, 58, 30)
FENCE_Y_06 = QUAY_TY_06 * TILE_OUT - 2


def tiles_06(tx, ty):
    rnd = random.Random(tx * 977 + ty * 131 + 6)
    if ty < QUAY_TY_06 - 1:
        return GRASS_06[rnd.randrange(len(GRASS_06))]
    if ty == QUAY_TY_06 - 1:
        return EDGE_06
    return SEA_06 if tx < BASIN_TX_06 else QUAY_06


def fence(bd, y, x0, x1, height=8, spacing=15, depth=2):
    """A run of posts and two rails across the scene, along +X.

    Chapter 03's ``railing`` runs away from the camera and can take its height
    from one terrace; this one runs across a field that rolls, so every post and
    every voxel of rail is set from the ground directly under it.
    """
    wood = BOOK.index(FENCE_WOOD_06)
    dark = BOOK.index(FENCE_DARK_06)
    x0, x1 = max(0, x0), min(bd.size[0], x1)
    for x in range(x0, x1, spacing):
        z = int(bd.ground[x, y]) + 1
        bd.a[x:x + 2, y:y + depth, z:z + height] = dark
    for x in range(x0, x1):
        z = int(bd.ground[x, y]) + 1
        for dz in (height - 1, height - 4):
            bd.a[x, y:y + depth, z + dz] = wood


def crates(bd, spots):
    """Cargo stacked on the quay: ``spots`` is (x, y, how many high).

    Every crate in a stack is placed outright rather than let ``stamp`` find its
    own footing, because only the bottom one of them stands on anything -- and
    the height they all take is the ground under the bottom one. The first cut
    took ``GROUND_Z`` for that and put two stacks out over the harbour, standing
    on the level the quay would have been at if the basin were not there.
    """
    m = obstacle('wooden_crate_1')
    lift = m.shape[2]
    for x, y, n in spots:
        z = bd.flatten(x, x + m.shape[0], y, y + m.shape[1], margin=0) + 1
        for i in range(n):
            bd.stamp(m, x, y, z + i * lift)


def recipe_06(bd):
    quay_y = QUAY_TY_06 * TILE_OUT
    basin_x = BASIN_TX_06 * TILE_OUT

    bd.undulate(amplitude=2.0, seed=6)
    # The quay is dressed stone and the basin is water: neither rolls with the
    # turf in front of them, so both are set outright.
    bd.terrace(basin_x, bd.size[0], quay_y, bd.size[1], GROUND_Z)
    bd.terrace(0, basin_x, quay_y, bd.size[1], WATER_Z_06)

    church = building(bd, 'red_church_1', *CHURCH_06)
    bd.lay_ground(tiles_06, '06')
    church()

    fence(bd, FENCE_Y_06, basin_x, bd.size[0])

    # Stacked along the quay's own edge, where the basin opens out beside them,
    # and standing on whatever ``crates`` finds under the bottom one of each:
    # placing them at a fixed height put two stacks out over the water.
    crates(bd, [(76, 94, 2), (76, 122, 1), (80, 150, 2), (198, 162, 1)])

    scatter_trees(
        bd, ('tree_dark_red', 'tree_light_red'),
        boxes=[
            (0, 64, 44, 80, 0.6),        # the autumn trees on the turf, screen right
            (180, 256, 44, 80, 0.6),     # and screen left, in front of the church
            (232, 256, 96, 168, 0.5),    # and on past it, closing the far corner
            (0, 40, 20, 40, 0.5),        # a couple near the frame edges, for depth
            (216, 256, 20, 40, 0.5),
        ],
        seed=6, avoid=(FIGHTER_ZONE,))


# --------------------------------------------------------------------------- #
# chapter 07 -- the wild wood                                                 #
# --------------------------------------------------------------------------- #

# Chapter 07 is another wood, which is exactly the problem: chapter 04 was one
# too, and two backdrops of pines on grass are one backdrop shown twice. What
# tells them apart on the map is what tells them apart here. Chapter 04's trees
# are the green pines of a tended forest with a round patch of earth trodden
# bare in the middle of it; chapter 07's are blue spruce, wilder and darker,
# and the earth is not a patch but a mud track a dozen tiles wide, worn from the
# bottom of the map to the top.
#
# So the track runs away from the camera rather than across it, and narrows with
# distance until the trees close over it -- the fighters stand in the mud of it,
# and the wood is what they are read against.

GRASS_07 = (145, 38, 41, 47, 31)
MUD_07 = 146                           # the track, worn down to bare earth
MUD_MID_07 = (71, 65, 59, 57, 51)      # earth with grass coming back through
MUD_EDGE_07 = (48, 58, 64, 53, 63, 69)
SAND_07 = 144                          # the pale clearing off to screen right

# The track's middle, in tile columns, and how wide it is at the camera: it
# loses about a fifth of a tile of half-width per row, so by the back of the
# model it has closed up and the wood runs unbroken across the horizon.
#
# The middle is 14 tiles and not 16: the camera stands at x 109, a good half
# tile to the right of the model's own centre line, and a track laid down the
# model's middle comes out down the left-hand side of the picture.
TRACK_CX_07 = 14.0
TRACK_HW_07 = 4.6
TRACK_TAPER_07 = 0.20

SPRUCE_07 = ('tree_blue', 'tree_blue', 'tree_blue',
             'tree_dark_green', 'tree_light_green')


def tiles_07(tx, ty):
    rnd = random.Random(tx * 977 + ty * 131 + 7)
    if tx < 6 and 15 <= ty <= 21:
        return SAND_07
    # A track with straight sides is a road; bending the middle by a slow wave
    # and the edge by a faster one gives it the wander of ground worn by feet.
    cx = TRACK_CX_07 + 1.8 * math.sin(ty * 0.28)
    d = abs(tx - cx) + 0.7 * math.sin(ty * 0.9 + tx * 0.5)
    hw = TRACK_HW_07 - TRACK_TAPER_07 * ty
    if d < hw:
        return MUD_07
    if d < hw + 1.2:
        return MUD_MID_07[rnd.randrange(len(MUD_MID_07))]
    if d < hw + 2.6:
        return MUD_EDGE_07[rnd.randrange(len(MUD_EDGE_07))]
    return GRASS_07[rnd.randrange(len(GRASS_07))]


def recipe_07(bd):
    bd.undulate(amplitude=3.0, wavelength=80.0, seed=7)
    bd.rise(y0=120, height=9)
    bd.lay_ground(tiles_07, '07')

    scatter_trees(
        bd, SPRUCE_07,
        boxes=[
            (0, 92, 40, 176, 0.7),       # the wood down screen right...
            (172, 256, 40, 176, 0.7),    # ...and screen left
            (0, 256, 156, 176, 0.85),    # closing over the head of the track
            (0, 60, 20, 40, 0.5),        # a few near the camera, for depth
            (200, 256, 20, 40, 0.5),
        ],
        spacing=9, seed=17, avoid=(FIGHTER_ZONE,))

    # The red trees the map keeps to its far corner, so the wood is not all one
    # colour, and two spruce at scale 2 to give the frame a foreground.
    scatter_trees(bd, ('tree_dark_red', 'tree_light_red'),
                  boxes=[(196, 256, 96, 152, 0.5)], spacing=10, seed=27)
    bd.stamp(obstacle('tree_blue', 2), 36, 64)
    bd.stamp(obstacle('tree_dark_green', 2), 198, 60)


# --------------------------------------------------------------------------- #
# chapter 08 -- the castle gate                                               #
# --------------------------------------------------------------------------- #

# Chapter 08 is the one map with a thing big enough to be the whole horizon:
# ``castle_wall_1`` is 29 tiles wide, the full width of the board, and at the
# backdrop's scale it comes out 232 x 64 x 48 -- near enough the model's own
# width, and tall enough that its parapet is above the top of the frame. So
# this backdrop is built the other way round from the rest: the wall is the
# back of it, and everything else is what stands between the camera and the
# gate.
#
# Which, on the map, is the moat and the bridge over it. Both go in on the
# camera axis, the way chapter 03's crossing did -- the planking runs away
# under the fighters and the water opens out either side of them -- and the
# autumn trees the map banks up along the moat fill the corners.

WATER_08 = 364                 # the moat
DECK_08 = 216                  # the drawbridge planking
ROAD_08 = 214                  # the cobbled road in the near foreground
GRASS_08 = (101, 381, 190)
BANK_N_08 = 218                # grass with the moat's water on its far side
BANK_S_08 = 271                # grass with the water on its near side

MOAT_TY_08 = (13, 17)          # ty0, ty1 -> y 104..136
DECK_TX_08 = (10, 19)          # deck tile columns -> x 80..152, on the camera axis
ROAD_TY_08 = (7, 9)            # the road across the foreground
WATER_Z_08 = GROUND_Z - 6
DECK_Z_08 = GROUND_Z + 1

WALL_08 = (12, 148)            # near-left corner; 232 x 64 voxels at SCALE
# The two knights and the two ball-topped pillars that stand at the bridge head
# on the map, brought up to scale 2 so they read as more than gateposts.
GUARDS_08 = ((62, 92), (158, 92), (62, 116), (158, 116))


def _deck_08(tx, ty):
    """True where the planking is: the moat's width, and a step onto each bank."""
    return (DECK_TX_08[0] <= tx < DECK_TX_08[1]
            and MOAT_TY_08[0] - 1 <= ty <= MOAT_TY_08[1])


def tiles_08(tx, ty):
    rnd = random.Random(tx * 977 + ty * 131 + 8)
    if _deck_08(tx, ty):
        return DECK_08
    if MOAT_TY_08[0] <= ty < MOAT_TY_08[1]:
        return WATER_08
    if ty == MOAT_TY_08[0] - 1:
        return BANK_N_08
    if ty == MOAT_TY_08[1]:
        return BANK_S_08
    if ROAD_TY_08[0] <= ty < ROAD_TY_08[1]:
        return ROAD_08
    return GRASS_08[rnd.randrange(len(GRASS_08))]


def recipe_08(bd):
    dx0, dx1 = DECK_TX_08[0] * TILE_OUT, DECK_TX_08[1] * TILE_OUT
    my0, my1 = MOAT_TY_08[0] * TILE_OUT, MOAT_TY_08[1] * TILE_OUT

    bd.undulate(amplitude=1.5, seed=8)
    bd.terrace(0, bd.size[0], my0, my1, WATER_Z_08)
    bd.terrace(dx0, dx1, my0 - TILE_OUT, my1 + TILE_OUT, DECK_Z_08)
    bd.terrace(0, bd.size[0], my1, bd.size[1], GROUND_Z)   # the far bank, level
    bd.rise(y0=my1, height=5)                              # and its slope to the wall

    # The planking has to keep its own grain at full contrast the way chapter
    # 03's does -- the planks are what says "bridge" -- while the grass and the
    # cobbles around it still need most of theirs thrown away.
    wall = building(bd, 'castle_wall_1', *WALL_08)
    bd.lay_ground(tiles_08, '08',
                  contrast=lambda t: 1.0 if t == DECK_08 else TILE_CONTRAST)
    wall()

    for x, y in GUARDS_08:
        bd.stamp(obstacle('stone_statue_1' if y < 100 else 'stone_pillar_1', 2),
                 x, y)

    scatter_trees(
        bd, ('tree_dark_red', 'tree_light_red'),
        boxes=[
            (0, 88, 72, 104, 0.75),      # banked along the moat, screen right
            (176, 256, 72, 104, 0.75),   # and screen left
            (0, 80, 140, 168, 0.6),      # a thinner row on the far bank
            (176, 256, 140, 168, 0.6),
        ],
        spacing=10, seed=18, avoid=(FIGHTER_ZONE,))
    scatter_trees(
        bd, ('tree_blue', 'tree_dark_green'),
        boxes=[(0, 52, 24, 64, 0.5), (204, 256, 24, 64, 0.5)],
        spacing=11, seed=28)


# --------------------------------------------------------------------------- #
# chapter 09 -- the statue avenue                                             #
# --------------------------------------------------------------------------- #

# Chapter 09 is a paved crossroads on dark turf, lined the whole way with stone:
# knights on pedestals and pillars with a stone ball on top, thirty of them,
# with a monument in the middle of it and the pines standing off at the edges.
#
# One of those avenues seen down its length is the backdrop. Two rows of stone
# converging into the distance do the same work chapter 03's railings do -- they
# draw the eye to the back of the frame and put the fighters at the end of a
# perspective rather than in front of a wall -- and the monument closes the far
# end of it. Nothing stands on the paving between the rows, which is the point:
# that lane is the fighter corridor.

PAVE_09 = 72                   # the avenue's blue-and-tan cobble
GRASS_09 = (74, 75, 61, 54, 53, 71)
TURF_ISLE_09 = (12, 15, 17)    # turf with the paving creeping into it

AVENUE_TX_09 = (10, 18)        # -> x 80..144, the lane the fighters stand in
STONE_X_09 = (60, 148)         # the two rows, just off the paving either side
STONE_Y_09 = (36, 178)
STONE_SPACING_09 = 21

MONUMENT_09 = (82, 166)        # notice_board_1 at scale 2: 60 x 12 voxels


def tiles_09(tx, ty):
    rnd = random.Random(tx * 977 + ty * 131 + 9)
    if AVENUE_TX_09[0] <= tx < AVENUE_TX_09[1]:
        return PAVE_09
    if tx == AVENUE_TX_09[0] - 1 or tx == AVENUE_TX_09[1]:
        return TURF_ISLE_09[rnd.randrange(len(TURF_ISLE_09))]
    return GRASS_09[rnd.randrange(len(GRASS_09))]


def avenue(bd, keys, x, y0, y1, spacing, scale=2):
    """One row of stone standing down the side of the way, at ``spacing``.

    The models alternate in order rather than at random: this is a processional
    avenue, and what makes it read as one is that the same thing recurs at the
    same interval all the way to the end.
    """
    for i, y in enumerate(range(y0, y1, spacing)):
        bd.stamp(obstacle(keys[i % len(keys)], scale), x, y)


def recipe_09(bd):
    bd.undulate(amplitude=1.5, wavelength=110.0, seed=9)
    # A paved way is laid, not grown: the lane itself is flat, and only the turf
    # either side of it rolls.
    bd.terrace(AVENUE_TX_09[0] * TILE_OUT, AVENUE_TX_09[1] * TILE_OUT,
               0, bd.size[1], GROUND_Z)
    bd.rise(y0=140, height=4)

    monument = building(bd, 'notice_board_1', *MONUMENT_09, scale=2)
    bd.lay_ground(tiles_09, '09')
    monument()

    keys = ('stone_statue_1', 'stone_pillar_1')
    avenue(bd, keys, STONE_X_09[0], STONE_Y_09[0], STONE_Y_09[1],
           STONE_SPACING_09)
    avenue(bd, keys[::-1], STONE_X_09[1], STONE_Y_09[0], STONE_Y_09[1],
           STONE_SPACING_09)

    scatter_trees(
        bd, ('tree_dark_red', 'tree_blue', 'tree_light_green'),
        boxes=[
            (0, 48, 40, 176, 0.7),       # the pines standing off, screen right
            (208, 256, 40, 176, 0.7),    # and screen left
            (0, 60, 156, 176, 0.6),      # and closing the far corners
            (196, 256, 156, 176, 0.6),
        ],
        spacing=10, seed=19, avoid=(FIGHTER_ZONE,))


# --------------------------------------------------------------------------- #
# chapter 10 -- the fire cavern                                               #
# --------------------------------------------------------------------------- #

# Chapter 10 is underground: a floor of brown rock cut out of pure black, with
# fire burning on it -- low bowls, bright columns and dim ones -- and embers
# glowing in the stone between them.
#
# Every other backdrop can leave the top of the frame to the sky, because every
# other chapter is out of doors. This one cannot: a cave with a blue sky over it
# is a quarry. So the rock does not stop at the horizon, it climbs -- out of the
# floor at both sides and into a cliff across the back, high enough that its top
# is above the top of the frame -- and what closes the picture in is stone.

# Tiles 29 and 116 are the black the cavern is cut out of, not rock, and tile 2
# is half of each: laid as ground they come out as holes punched in the floor.
ROCK_10 = (51, 80, 83, 92, 95, 79, 21, 86, 93, 85, 20, 78, 90, 81, 89)
EMBER_10 = 72                  # rock with a coal still glowing in it

# Where the cavern floor ends and the walls begin. The back cliff has to stand
# by y 148: the frame's top edge is about z 79 that far in, and rock rising at
# five voxels a row from y 110 is well past that by then. The sides have to
# reach the same height, or a wedge of sky opens where the two walls meet.
CAVE_X_10 = (30, 228)
CAVE_Y_10 = (110, 148)
CAVE_TOP_10 = 89
CAVE_SLOPE_10 = 5.0

FIRE_COLUMNS_10 = ('fire_pillar_2', 'fire_pillar_3')
FIRE_BOWLS_10 = ('fire_pillar_1',)


def tiles_10(tx, ty):
    rnd = random.Random(tx * 977 + ty * 131 + 10)
    if rnd.random() < 0.03:
        return EMBER_10
    return ROCK_10[rnd.randrange(len(ROCK_10))]


def cavern(bd, x0, x1, y0, y1, top=CAVE_TOP_10, slope=CAVE_SLOPE_10):
    """Close a cave in: rock climbing out of the floor at both sides and across
    the back, and dropping to the floor again behind the cliff.

    ``rise`` lifts the far ground to close a horizon off, which is all an
    outdoor chapter wants; this has to go much further -- above the top of the
    frame -- and it has to come back down afterwards, because everything behind
    the cliff is hidden by it and filling that with stone would only double the
    model for nothing.
    """
    xs = np.arange(bd.size[0], dtype=np.float64)[:, None]
    ys = np.arange(bd.size[1], dtype=np.float64)[None, :]
    # Rock does not have straight edges. Two slow waves bend the foot of every
    # wall so the cavern is a cave and not a room.
    wob = 5.0 * np.sin(ys / 19.0 + 0.6) + 3.0 * np.cos(xs / 13.0 + 1.1)
    side = np.maximum((x0 + wob) - xs, xs - (x1 - wob))
    side = np.where(ys < y1, side, -1e9)          # the cliff takes over past y1
    back = np.minimum(ys - (y0 + 0.4 * wob), (y1 - ys) * 3.0)
    d = np.maximum(side, back)
    bd.ground = bd.ground + np.clip(d * slope, 0, top - GROUND_Z).astype(np.int16)


def recipe_10(bd):
    bd.undulate(amplitude=2.5, wavelength=60.0, seed=10)
    cavern(bd, CAVE_X_10[0], CAVE_X_10[1], CAVE_Y_10[0], CAVE_Y_10[1])

    # The rock is drawn in blobs several pixels across, wide enough to survive a
    # backdrop voxel, so it keeps more of its grain than grass or cobbles would;
    # and an ember keeps all of its, because dulling it is putting it out.
    bd.lay_ground(tiles_10, '10',
                  contrast=lambda t: 1.0 if t == EMBER_10 else 0.7)

    # Fire is the only light down here, so it goes where it will be seen against
    # the walls -- the columns up against the rock, the bowls out on the floor.
# Sparingly. The first cut strewed them the way the map does -- seventy-one
    # of them, over a board forty-five tiles deep -- and packing that many into
    # one frame gave a bonfire with no cave left in it: fire is the only bright
    # thing down here, so a dozen of them is already a lot of picture.
    scatter_trees(bd, FIRE_COLUMNS_10,
                  boxes=[(34, 88, 60, 132, 0.45), (176, 226, 60, 132, 0.45)],
                  spacing=26, seed=20, scale=2, avoid=(FIGHTER_ZONE,))
    scatter_trees(bd, FIRE_BOWLS_10,
                  boxes=[(38, 92, 28, 128, 0.4), (174, 222, 28, 128, 0.4)],
                  spacing=30, seed=30, scale=2, avoid=(FIGHTER_ZONE,))

    # Two columns close to the camera, for the same reason chapter 04 has two
    # big pines there: without a foreground the cave is a painted flat.
    bd.stamp(obstacle('fire_pillar_2', 2), 56, 44)
    bd.stamp(obstacle('fire_pillar_3', 2), 196, 40)


# --------------------------------------------------------------------------- #
# shared by 11..14 -- ledges, rock faces, patchy grass                         #
# --------------------------------------------------------------------------- #

def _grid(bd):
    """Voxel x and y as broadcastable float arrays, for building masks."""
    return (np.arange(bd.size[0], dtype=np.float64)[:, None],
            np.arange(bd.size[1], dtype=np.float64)[None, :])


def _blob(tx, ty, spec, wob=0.2, seed=0.0):
    """Distance from the middle of a ragged ellipse, in radii: < 1 is inside.

    ``spec`` is (centre tx, centre ty, radius tx, radius ty) in tiles. The two
    waves on top are what keep a pond or a patch of bare earth from being drawn
    with compasses. Tile coordinates may be fractional, and arrays: that is how
    ``lay_where`` asks the question once per voxel instead of once per tile.
    """
    cx, cy, rx, ry = spec
    d = np.hypot((tx - cx) / rx, (ty - cy) / ry)
    return d + wob * (0.5 * np.sin(ty * 1.3 + tx * 0.4 + seed)
                      + 0.5 * np.cos(tx * 1.1 - seed))


def _patchy(tx, ty, seed):
    """Slow 0..1 noise over tile coordinates, for where the grass runs dark.

    Chapters 11 and 14 are painted in two greens, the bright turf and the
    long dark grass in drifts across it; a field of one green would be any
    chapter's field.
    """
    return (0.5 + 0.25 * np.sin(tx * 0.33 + seed) * np.cos(ty * 0.29 + seed * 0.7)
            + 0.25 * np.sin((tx + ty) * 0.21 + seed * 1.9))


def _pick(keys, seed):
    """``tiles(tx, ty)`` choosing at random among ``keys``, the same way per tile."""
    return lambda tx, ty: keys[random.Random(tx * 977 + ty * 131 + seed)
                               .randrange(len(keys))]


def lay_where(bd, tiles, nn, mask, contrast=TILE_CONTRAST):
    """Lay a second floor over the first, only where ``mask`` (x by y) is set.

    ``lay_ground`` decides a whole tile at a time, so a pond or a riverbed laid
    with it has edges cut in eight-voxel stairs -- from the battle camera a
    round pond is a stack of rectangles. Laying the water across the whole
    field and keeping it only inside a per-voxel outline gives the shore its
    curve back, while every voxel still wears the art's own tile colours.
    """
    other = Backdrop(bd.size)
    other.ground = bd.ground
    other.lay_ground(tiles, nn, contrast)
    bd.a[mask] = other.a[mask]


def raise_where(bd, mask, height):
    """Step the ground up by ``height`` wherever ``mask`` (x by y) is set.

    ``rise`` climbs smoothly to close a horizon; the maps from chapter 11 on are
    cut into terraces instead, each with a ragged brown line along its foot, and
    that is a step, not a slope.
    """
    bd.ground = bd.ground + np.where(mask, height, 0).astype(np.int16)


def rock_faces(bd, nn, tile_id, min_rise=4, lip=TURF):
    """Dress every step of ``min_rise`` voxels or more in a rock tile's colours.

    ``lay_ground`` packs a column with soil under its turf, which is right for
    a bank a couple of voxels high and wrong for a cliff: thirty voxels of one
    flat brown is a wall of cardboard. So the tile's own rock is wrapped round
    the face instead, indexed by height, and the strata come out running across
    it the way they do in the art. The top ``lip`` voxels keep the grass.
    """
    colour, _lift = tile_surface(nn, tile_id, contrast=0.9)
    g = bd.ground.astype(np.int32)
    p = np.pad(g, 1, mode='edge')
    low = np.minimum.reduce([p[2:, 1:-1], p[:-2, 1:-1], p[1:-1, 2:], p[1:-1, :-2]])
    for x, y in zip(*np.nonzero(g - low >= min_rise)):
        for z in range(max(0, int(low[x, y]) - 1), max(0, int(g[x, y]) - lip + 1)):
            c = colour[(x + y) % TILE_OUT, z % TILE_OUT]
            if c:
                bd.a[x, y, z] = c


# --------------------------------------------------------------------------- #
# chapter 11 -- the terraced wood                                             #
# --------------------------------------------------------------------------- #

# Chapter 11 is open high ground broken into terraces: ragged brown ledges wind
# across the grass, drifts of dark grass lie between them, blue spruce and green
# pines stand about in clumps, and in the middle of it all is one grove of trees
# turned red. A pond sits in a hollow at the bottom of the map and bare earth
# shows through in a couple of places.
#
# The terraces are what no earlier backdrop has, so one of them runs across the
# back as a step up with the red grove standing on top -- that is the picture
# the map is built around. The pond is brought forward to screen right, the way
# chapter 06's basin was, because water laid at the back is a thread on the
# horizon.

GRASS_11 = (161, 161, 161, 41, 38, 37, 39)
DARK_11 = (163, 163, 35, 40, 44, 45, 47)
DIRT_11 = 162
DIRT_EDGE_11 = (60, 69, 54, 62, 57, 65, 50, 53, 61)
WATER_11 = 72

POND_11 = (5.5, 8.5, 5.2, 3.4)           # centre tx, ty, radius tx, ty
DIRT_PATCH_11 = (26.5, 10.0, 3.2, 2.4)   # the bare earth off to screen left
WATER_Z_11 = GROUND_Z - 4

LEDGE_Y_11 = 126                         # the terrace's foot, give or take its wander
LEDGE_H_11 = 7


def _pond_11(tx, ty):
    return _blob(tx, ty, POND_11, seed=1.0) < 1.0


def _dirt_11(tx, ty):
    return _blob(tx, ty, DIRT_PATCH_11, wob=0.3, seed=2.0)


def _ledge_11(xs, ys):
    return ys >= LEDGE_Y_11 + 8.0 * np.sin(xs / 23.0 + 1.0) + 4.0 * np.sin(xs / 9.0)


tiles_11 = _pick(GRASS_11, 11)


def recipe_11(bd):
    bd.undulate(amplitude=2.0, wavelength=80.0, seed=11)
    xs, ys = _grid(bd)
    raise_where(bd, _ledge_11(xs, ys), LEDGE_H_11)
    pond = _pond_11(xs / TILE_OUT, ys / TILE_OUT)
    bd.ground[pond] = WATER_Z_11
    bd.lay_ground(tiles_11, '11')

    tx, ty = xs / TILE_OUT, ys / TILE_OUT
    lay_where(bd, _pick(DARK_11, 111), '11', (_patchy(tx, ty, 11) > 0.66) & ~pond)
    dirt = _dirt_11(tx, ty)
    lay_where(bd, _pick(DIRT_EDGE_11, 211), '11', (dirt < 1.1) & ~pond)
    lay_where(bd, lambda tx, ty: DIRT_11, '11', (dirt < 0.7) & ~pond)
    lay_where(bd, lambda tx, ty: WATER_11, '11', pond)

    def off_the_ledge(cx, cy):
        # A tree straddling the step stands at the average of the two levels:
        # half of it buried in the terrace, half of it hanging in the air.
        edge = LEDGE_Y_11 + 8.0 * math.sin(cx / 23.0 + 1.0) + 4.0 * math.sin(cx / 9.0)
        return abs(cy - edge) > 7 and not _pond_11(cx // TILE_OUT, cy // TILE_OUT)

    # The red grove, on top of the terrace and square behind the fight.
    scatter_trees(bd, ('tree_dark_red', 'tree_light_red'),
                  boxes=[(92, 196, 140, 178, 0.9)],
                  spacing=9, jitter=3, seed=21, where=off_the_ledge)
    scatter_trees(
        bd, ('tree_blue', 'tree_blue', 'tree_dark_green'),
        boxes=[
            (0, 92, 128, 178, 0.75),     # on the terrace, screen right
            (196, 256, 128, 178, 0.75),  # and screen left
            (176, 256, 36, 124, 0.75),   # clumps down screen left
            (0, 24, 36, 124, 0.6),       # past the pond, screen right
            (40, 96, 96, 124, 0.6),      # behind the pond, before the step
        ],
        spacing=9, seed=31, avoid=(FIGHTER_ZONE,), where=off_the_ledge)

    bd.stamp(obstacle('tree_blue', 2), 178, 44)
    bd.stamp(obstacle('tree_dark_green', 2), 60, 104)


# --------------------------------------------------------------------------- #
# chapter 12 -- the ravine                                                    #
# --------------------------------------------------------------------------- #

# Chapter 12 is a long valley floor between walls of brown rock, cut in steps
# like stairs, with caves opening black in the cliff faces. Two lines of red
# trees run down the valley either side of a trodden track, and the spruce and
# pines keep to the ground under the walls and up on top of them.
#
# So the backdrop is the valley seen along its length: the track under the
# fighters, the red trees lining it, and rock rising at both sides of the frame.
# The walls step in as they go back, the way the map's do, and the left-hand one
# swings across the far end with a cave mouth in it -- which is the one thing on
# this map nothing else in the game has.

GRASS_12 = (168, 168, 168, 214, 215)
DARK_12 = (169, 196, 201, 208, 202, 192)
TRACK_12 = (165, 155, 151, 150, 153)
TRACK_EDGE_12 = (160, 148, 144, 147, 159, 164)
ROCK_12 = 177

TRACK_CX_12 = 14.0                     # tiles; on the camera axis, not the model's
CLIFF_H_12 = 30
# The inner edge of each wall, in tile columns, by tile row. ``int`` is what
# makes them stairs rather than slopes.
CAVE_12 = (176, 194)                   # x0, x1 of the cave mouth in the far wall
SPUR_TY_12 = 18                        # where the left wall turns across the back
SPUR_TX_12 = 20

RED_ROWS_12 = (82, 174)                # x of the two lines of red trees
RED_STOP_12 = 96                       # the screen-left line stops short of the cave


def _right_wall_12(ty):
    return 5 + int(ty * 0.16 + 0.9 * math.sin(ty * 0.7))


def _left_wall_12(ty):
    if ty >= SPUR_TY_12:
        return SPUR_TX_12
    return 26 - int(ty * 0.14 + 0.9 * math.sin(ty * 0.5 + 1.0))


def tiles_12(tx, ty):
    rnd = random.Random(tx * 977 + ty * 131 + 12)
    cx = TRACK_CX_12 + 1.2 * math.sin(ty * 0.3)
    d = abs(tx - cx) + 0.5 * math.sin(ty * 0.9 + tx * 0.5)
    hw = 1.9 - 0.04 * ty
    if d < hw:
        return TRACK_12[rnd.randrange(len(TRACK_12))]
    if d < hw + 1.3:
        return TRACK_EDGE_12[rnd.randrange(len(TRACK_EDGE_12))]
    if _patchy(tx, ty, 12) > 0.66:
        return DARK_12[rnd.randrange(len(DARK_12))]
    return GRASS_12[rnd.randrange(len(GRASS_12))]


def cave_mouth(bd, x0, x1, y, depth=10, height=13):
    """Hollow an arched opening into a wall whose face is at ``y``, facing -Y.

    The inside is lined in near-black, because that is what a cave is in the
    art: not a room you can see into, but a hole.
    """
    black = BOOK.index((14, 10, 8))
    base = int(bd.ground[x0:x1, y - 4].min()) + 1
    mid, half = (x0 + x1) / 2.0, (x1 - x0) / 2.0
    for x in range(x0, x1):
        h = int(round(height * math.sqrt(max(0.0, 1 - ((x + 0.5 - mid) / half) ** 2))))
        if h <= 0:
            continue
        bd.a[x, y - 2:y + depth, base:base + h] = 0
        bd.a[x, y + depth, base:base + h + 1] = black       # the back of it
        bd.a[x, y - 2:y + depth + 1, base + h] = black      # its roof
        bd.a[x, y - 2:y + depth + 1, base - 1] = black      # its floor


def recipe_12(bd):
    bd.undulate(amplitude=1.5, wavelength=90.0, seed=12)
    xs, ys = _grid(bd)
    ty = np.floor(ys / TILE_OUT).astype(int)[0]
    right = np.array([_right_wall_12(t) for t in ty], np.float64)[None, :] * TILE_OUT
    left = np.array([_left_wall_12(t) for t in ty], np.float64)[None, :] * TILE_OUT
    raise_where(bd, (xs < right) | (xs >= left), CLIFF_H_12)

    bd.lay_ground(tiles_12, '12')
    rock_faces(bd, '12', ROCK_12)
    cave_mouth(bd, CAVE_12[0], CAVE_12[1], SPUR_TY_12 * TILE_OUT)

    # The two lines of red trees, lining the track the way they do on the map:
    # dark and light alternating, a little ragged, running to the far wall.
    rnd = random.Random(12)
    for i, y in enumerate(range(44, SPUR_TY_12 * TILE_OUT - 10, 12)):
        for row, x in enumerate(RED_ROWS_12):
            if row == 1 and y > RED_STOP_12:
                continue                  # keep the cave mouth in sight
            key = ('tree_dark_red', 'tree_light_red')[(i + row) % 2]
            bd.stamp(obstacle(key), x + rnd.randint(-3, 3), y + rnd.randint(-2, 2))

    def on_the_floor(cx, cy):
        t = int(cy // TILE_OUT)
        return _right_wall_12(t) * TILE_OUT + 4 < cx < _left_wall_12(t) * TILE_OUT - 4

    def up_top(cx, cy):
        t = int(cy // TILE_OUT)
        return (cx < _right_wall_12(t) * TILE_OUT - 6
                or cx > _left_wall_12(t) * TILE_OUT + 6)

    pines = ('tree_blue', 'tree_dark_green', 'tree_blue')
    # Spruce under the walls, between them and the red lines...
    scatter_trees(bd, pines,
                  boxes=[(40, 76, 40, 176, 0.6), (180, 216, 40, RED_STOP_12, 0.6)],
                  spacing=10, seed=22, avoid=(FIGHTER_ZONE,), where=on_the_floor)
    # The far end of the valley, where it bends out of sight behind the wall.
    scatter_trees(bd, pines, boxes=[(56, 164, 162, 178, 0.9)],
                  spacing=8, jitter=3, seed=42, where=on_the_floor)
    # ...and up on top of them, along the rim.
    scatter_trees(bd, pines,
                  boxes=[(0, 64, 20, 178, 0.6), (184, 256, 20, 178, 0.6)],
                  spacing=10, seed=32, where=up_top)


# --------------------------------------------------------------------------- #
# chapter 13 -- the enemy camp                                                #
# --------------------------------------------------------------------------- #

# Chapter 13 is the army's camp: a clearing of trodden brown earth in a wood of
# pale dead pines, with round tents pitched about it -- grey ones and blue-and-
# white striped ones -- barricades of sharpened logs stacked between them, a
# timbered well in the middle and tree stumps where the wood was cut back.
#
# It is the first backdrop with no green in it, which is right: this is a
# chapter fought on dry ground. The tents stand either side of the fight at
# scale 2, where they read as tents and not as mushrooms, and the logs and the
# well -- which the map only paints flat into its tiles -- are built as geometry
# in the art's own browns, the way chapter 03's railings were.

DIRT_13 = (21, 21, 99)
DIRT_EDGE_13 = (22, 29, 38, 25, 26, 39, 30, 28, 18, 16, 20)
DRY_GRASS_13 = (97, 97, 31, 17, 19)

CAMP_13 = (14.0, 11.0, 13.0, 9.5)     # the trodden clearing, in tiles

TENTS_13 = (('tent_gray_1', 176, 88), ('tent_blue_1', 204, 136),
            ('tent_blue_1', 36, 94), ('tent_gray_1', 6, 142))
LOGS_13 = ((52, 62), (176, 40), (112, 162), (220, 124))
WELL_13 = (70, 146)
STUMPS_13 = ((84, 40), (212, 46), (164, 156), (30, 76), (236, 104))

LOG_BARK_13 = (104, 72, 36)
LOG_LIGHT_13 = (148, 112, 76)
LOG_DARK_13 = (76, 48, 16)
LOG_END_13 = (132, 96, 60)
WELL_WATER_13 = (24, 40, 40)


def tiles_13(tx, ty):
    rnd = random.Random(tx * 977 + ty * 131 + 13)
    d = _blob(tx, ty, CAMP_13, wob=0.25, seed=3.0)
    if d < 0.75:
        return DIRT_13[rnd.randrange(len(DIRT_13))]
    if d < 1.0:
        return DIRT_EDGE_13[rnd.randrange(len(DIRT_EDGE_13))]
    return DRY_GRASS_13[rnd.randrange(len(DRY_GRASS_13))]


def _log(bd, x0, x1, cy, cz, r, point=3):
    """One log lying along +X, its two ends whittled to a point."""
    bark, light = BOOK.index(LOG_BARK_13), BOOK.index(LOG_LIGHT_13)
    dark, end = BOOK.index(LOG_DARK_13), BOOK.index(LOG_END_13)
    for x in range(x0, x1):
        taper = min(x - x0, x1 - 1 - x)
        rr = r * min(1.0, (taper + 1.0) / (point + 1.0))
        for y in range(int(cy - r) - 1, int(cy + r) + 2):
            for z in range(int(cz - r) - 1, int(cz + r) + 2):
                dy, dz = y + 0.5 - cy, z + 0.5 - cz
                if dy * dy + dz * dz > rr * rr or z < 0:
                    continue
                c = light if dz > rr * 0.45 else dark if dz < -rr * 0.45 else bark
                if taper == point:
                    c = end
                bd.a[x, y, z] = c


def log_pile(bd, x, y, length=24, r=2.6):
    """Three sharpened logs, two lying and one on top, along +X."""
    z = bd.flatten(x, x + length, y, y + int(4 * r) + 1, margin=1)
    _log(bd, x, x + length, y + r, z + r, r)
    _log(bd, x, x + length, y + 3 * r, z + r, r)
    _log(bd, x + 1, x + length - 1, y + 2 * r, z + r * 2.7, r)


def well(bd, x, y, size=24, wall=4, height=6):
    """A square timber well-head with dark water inside it."""
    bark, light = BOOK.index(LOG_BARK_13), BOOK.index(LOG_LIGHT_13)
    dark, water = BOOK.index(LOG_DARK_13), BOOK.index(WELL_WATER_13)
    z = bd.flatten(x, x + size, y, y + size, margin=1) + 1
    box = bd.a[x:x + size, y:y + size, z:z + height]
    box[:, :, :] = 0
    for k in range(height):
        c = light if k % 3 == 2 else dark if k % 3 == 0 else bark
        box[:wall, :, k] = c
        box[-wall:, :, k] = c
        box[:, :wall, k] = c
        box[:, -wall:, k] = c
    bd.a[x + wall:x + size - wall, y + wall:y + size - wall, z - 1:z + 2] = water
    for px in (x, x + size - 3):          # the corner stakes, standing proud
        for py in (y, y + size - 3):
            bd.a[px:px + 3, py:py + 3, z + height:z + height + 3] = bark
            bd.a[px + 1, py + 1, z + height + 3] = light


def stump(bd, x, y, r=3.2, height=3):
    side, top = BOOK.index((88, 60, 24)), BOOK.index(LOG_LIGHT_13)
    ring = BOOK.index((116, 84, 48))
    z = int(bd.ground[x, y]) + 1
    for i in range(int(-r) - 1, int(r) + 2):
        for j in range(int(-r) - 1, int(r) + 2):
            d = math.hypot(i, j)
            if d > r:
                continue
            bd.a[x + i, y + j, z:z + height] = side
            bd.a[x + i, y + j, z + height - 1] = ring if d < r * 0.45 else top


def recipe_13(bd):
    bd.undulate(amplitude=1.5, wavelength=80.0, seed=13)

    tents = [building(bd, key, x, y, scale=2) for key, x, y in TENTS_13]
    bd.lay_ground(tiles_13, '13')
    for t in tents:
        t()
    for x, y in LOGS_13:
        log_pile(bd, x, y)
    well(bd, *WELL_13)
    for x, y in STUMPS_13:
        stump(bd, x, y)

    clear = tuple((x - 4, x + 40, y - 4, y + 40) for _k, x, y in TENTS_13) + (
        (WELL_13[0] - 6, WELL_13[0] + 30, WELL_13[1] - 6, WELL_13[1] + 30),)
    scatter_trees(
        bd, ('tree_dark_gray', 'tree_light_gray'),
        boxes=[
            (0, 256, 164, 178, 0.9),     # the wood closing round the back
            (216, 256, 36, 178, 0.75),   # screen left, past the tents
            (0, 36, 36, 140, 0.7),       # screen right
            (40, 88, 116, 178, 0.5),     # thinning in towards the clearing
            (0, 40, 16, 36, 0.5),        # a few near the frame edges, for depth
            (220, 256, 16, 36, 0.5),
        ],
        spacing=9, seed=23, avoid=(FIGHTER_ZONE,) + clear)
    bd.stamp(obstacle('tree_light_gray', 2), 224, 60)


# --------------------------------------------------------------------------- #
# chapter 14 -- the dry riverbed                                              #
# --------------------------------------------------------------------------- #

# Chapter 14 is green country cut through by a broad bed of bare brown earth,
# a river that has run dry, winding from one corner of the map to the other.
# Pale pines and brown ones stand along its banks in lines, the green pines keep
# to groves further off, and the grass steps up in terraces towards the edges.
#
# Chapter 07 already sent a mud track straight away from the camera, so this bed
# crosses the frame on the slant instead: in from the near screen-left corner,
# under the fighters, and away to the far right, sunk a couple of voxels below
# its banks, with the pale trees following its edges.

GRASS_14 = (41, 41, 41, 38, 35, 44, 33, 43, 39, 36)
DARK_14 = (176, 176, 58, 61, 49, 65, 57, 62, 51)
BED_14 = 177
BANK_14 = (6, 9, 22, 23, 12, 2, 5, 15, 13, 14, 16, 7)
BED_Z_14 = GROUND_Z - 2

BED_HW_14 = 2.6                  # half-width of the bare earth, in tiles
TERRACE_X_14 = 44                # the step up along screen right


def _bed_cx_14(ty):
    """The riverbed's middle in tile columns. It passes x ~ 126 at row 13, where
    the camera's sight line past the fighters meets the ground."""
    return 28.0 - 1.05 * ty + 2.2 * np.sin(ty * 0.33)


def _bed_d_14(tx, ty):
    return np.abs(tx - _bed_cx_14(ty)) + 0.4 * np.sin(ty * 1.1 + tx * 0.3)


tiles_14 = _pick(GRASS_14, 14)


def recipe_14(bd):
    bd.undulate(amplitude=2.0, wavelength=80.0, seed=14)
    xs, ys = _grid(bd)
    raise_where(bd, (xs < TERRACE_X_14 + 7.0 * np.sin(ys / 17.0) + 3.0 * np.sin(ys / 7.0))
                & (ys > 70), 6)
    d = _bed_d_14(xs / TILE_OUT, ys / TILE_OUT)
    bd.ground[d < BED_HW_14] = BED_Z_14
    bd.lay_ground(tiles_14, '14')
    lay_where(bd, _pick(DARK_14, 114), '14',
              _patchy(xs / TILE_OUT, ys / TILE_OUT, 14) > 0.68)
    lay_where(bd, _pick(BANK_14, 214), '14', d < BED_HW_14 + 1.0)
    lay_where(bd, lambda tx, ty: BED_14, '14', d < BED_HW_14)

    # The pale and brown pines along both banks, a step back from the edge.
    rnd = random.Random(14)
    pale = ('tree_light_gray', 'tree_dark_gray')
    for y in range(8, bd.size[1] - 4, 11):
        cx = _bed_cx_14(y / float(TILE_OUT)) * TILE_OUT
        for side in (-1, 1):
            x = int(cx + side * (BED_HW_14 + 1.6) * TILE_OUT) + rnd.randint(-3, 3) - 4
            if FIGHTER_ZONE[0] <= x + 4 <= FIGHTER_ZONE[1] and y <= FIGHTER_ZONE[3]:
                continue
            if 0 <= x < bd.size[0] - 8 and rnd.random() < 0.85:
                bd.stamp(obstacle(pale[rnd.randrange(2)]), x, y + rnd.randint(-2, 2))

    def off_the_bed(cx, cy):
        near_terrace = abs(cx - (TERRACE_X_14 + 7.0 * math.sin(cy / 17.0)
                                 + 3.0 * math.sin(cy / 7.0))) < 7
        return (_bed_d_14(cx / TILE_OUT, cy / TILE_OUT) > BED_HW_14 + 2.6
                and not near_terrace)

    scatter_trees(
        bd, ('tree_light_green', 'tree_dark_green'),
        boxes=[
            (0, 40, 72, 178, 0.85),      # the grove up on the terrace, screen right
            (150, 256, 100, 178, 0.8),   # the far side of the bed, screen left
            (40, 96, 130, 178, 0.7),
            (172, 256, 20, 60, 0.5),     # near the frame edges, for depth
            (40, 92, 36, 70, 0.5),
        ],
        spacing=9, seed=24, avoid=(FIGHTER_ZONE,), where=off_the_bed)
    bd.stamp(obstacle('tree_dark_gray', 2), 62, 50)
    bd.stamp(obstacle('tree_light_green', 2), 174, 64)


RECIPES = {'02': recipe_02, '03': recipe_03, '04': recipe_04, '05': recipe_05,
           '06': recipe_06, '07': recipe_07, '08': recipe_08, '09': recipe_09,
           '10': recipe_10, '11': recipe_11, '12': recipe_12, '13': recipe_13,
           '14': recipe_14}


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
