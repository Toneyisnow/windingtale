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
  pale pines along its banks, green groves and a terrace beyond;
* 15, the lakeshore -- trodden earth on the shore, the lake open to screen
  right with a wooded island, green and pale pines behind;
* 16, the snowfield -- white plain, an open pool, a rock-edged ridge across
  the back with snow trees on it;
* 17, the ice island -- an ice causeway up the camera axis between sea, the
  ring road across, the terraced hill and its stair ahead;
* 18, the chasm bridge -- a plank deck over black nothing, red rails
  converging on a clifftop of blue spruce;
* 19, the red and blue wood -- a grass lane between belts of red and blue
  pines, a sandy patch, grey gravel;
* 20, the blue heights -- blue rock, grey-green terraces with a stair, a black
  gulf to screen right;
* 21, the forest temple -- an earth road between stone posts to a dark
  platform and its stair, slabs either side, pale pines;
* 22, the sanctum of the orbs -- a lawn ringed by black ramparts, the altar,
  six orb pillars, statues, darkness overhead;
* 23, the temple in the hills -- a paved way between the temple's wings,
  statues and posts, brown rock climbing into the dark;
* 24, the floating rock -- brown rock breaking off into a void, statues on
  black flagstones, pillars of light standing up out of the dark;
* 25, the lava caves -- a lava river crossing behind the fight, a black pit,
  fires, cave rock all round;
* 26, the pit hall -- iron grating between black shafts, a great diamond
  shaft behind the fighters;
* 27, the temple of light -- brown rock at the foot of pale terraces, a stair,
  a pool with steles, pillars of light down both flanks;
* 28, the water palace -- grating, a pale walkway, a channel, a row of pools
  with steles and pillars of light;
* 29, the dragons' sanctum -- the grating causeway between stepped channels
  and rows of light, up to the dais;
* 30, the last hall -- pale floor, channels wrapping round a raised hall and
  its stair, black rock jagged all round.

Chapters 20 on use ``enclose`` to wall the back and sides off: the battle
scene's sky is the same bright day for every chapter, and a temple or a void
with blue sky over it is wrong.

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
    # The model's array is taller than the crate (the top few layers are empty),
    # so stepping up by the array height leaves every crate above the first
    # hanging in the air. Step by the crate itself.
    lift = int(np.nonzero(m.any(axis=(0, 1)))[0].max()) + 1
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
CAVE_Y_10 = (100, 152)
CAVE_TOP_10 = 89
CAVE_SLOPE_10 = 5.0

FIRE_COLUMNS_10 = ('fire_pillar_2', 'fire_pillar_3')
FIRE_BOWLS_10 = ('fire_pillar_1',)


def tiles_10(tx, ty):
    rnd = random.Random(tx * 977 + ty * 131 + 10)
    if rnd.random() < 0.03:
        return EMBER_10
    return ROCK_10[rnd.randrange(len(ROCK_10))]


def _vnoise(xs, ys, cell, seed):
    """Smooth 0..1 value noise over voxel coordinates (broadcastable arrays)."""
    rnd = np.random.RandomState(seed)
    g = rnd.rand(int(max(np.max(xs), 1) / cell) + 3, int(max(np.max(ys), 1) / cell) + 3)
    u, v = xs / float(cell), ys / float(cell)
    iu, iv = np.floor(u).astype(int), np.floor(v).astype(int)
    fu, fv = u - iu, v - iv
    fu, fv = fu * fu * (3 - 2 * fu), fv * fv * (3 - 2 * fv)
    a = g[iu, iv] * (1 - fu) + g[iu + 1, iv] * fu
    b = g[iu, iv + 1] * (1 - fu) + g[iu + 1, iv + 1] * fu
    return a * (1 - fv) + b * fv


# The cavern is a lava tube: a long tunnel that lava once ran through, its cross
# section a wide ellipse -- flat-ish floor, walls that curve up and over into one
# arched ceiling, like a cylinder laid on its side -- running away from the camera
# and wandering from side to side as it goes.
# The fighters' ground: the tunnel is narrower than the open-air backdrops' floor, so the
# usual FIGHTER_ZONE would cover all of it and leave nowhere for the fires.
TUBE_ZONE_10 = (94, 130, 20, 150)
# The tunnel's middle line. 111 put it on the camera's axis; the picture is then slid
# 32 voxels the other way (about a fifth of the frame at the depth the background
# is seen at), so the fighters sit right of the middle of the bore.
TUBE_X_10 = 143
TUBE_END_10 = 168              # where the tunnel is stopped off by rock
CEILING_THICK_10 = 8           # voxels of rock laid over the arch (nobody sees more)


def _tube_axis(ys):
    """Where the tunnel's middle is at each depth (x), and its half-width."""
    # On the camera's line (x 111 is the middle of the frame), bending only a little,
    # so the two sides of the bore are much the same picture.
    cx = TUBE_X_10 + 8 * np.sin(np.clip(ys - 30, 0, None) / 50.0)
    a = 64 + 6 * np.sin(ys / 19.0 + 1.0) + 3 * np.sin(ys / 8.3)
    return cx, a


def cavern(bd, top=CAVE_TOP_10):
    """Cut the tunnel out of solid rock, as a floor height map plus a ceiling.

    Every column of the model is either inside the tunnel -- floor rising along
    the ellipse's lower arc, air, then ceiling from its upper arc up -- or wholly
    rock. The floor is the heightmap the other recipes use; the ceiling is a second
    surface (``bd.ceiling``, the z of its first voxel) that ``rock_walls`` fills,
    since a heightmap cannot hang anything over a floor.

    The first cut was a floor with a ramp of rock rising off each side and across
    the back: from the battle camera, a room with walls. A tunnel has no walls, only
    one surface that curls all the way round.
    """
    xs = np.arange(bd.size[0], dtype=np.float64)[:, None]
    ys = np.arange(bd.size[1], dtype=np.float64)[None, :]
    cx, a = _tube_axis(ys)
    b = 39.0 + 3.0 * np.sin(ys / 31.0 + 0.4)          # half-height of the bore
    u = (xs - cx) / a
    inside = (np.abs(u) < 1.0) & (ys < TUBE_END_10)
    s = np.sqrt(np.clip(1.0 - u * u, 0.0, 1.0))

    # Rock is never smooth: noise roughens the arc, more the further it has climbed.
    lumps = (_vnoise(xs, ys, 9.0, 101) - 0.5) * 2.0 + 0.6 * (_vnoise(xs, ys, 4.0, 102) - 0.5) * 2.0
    rise = b * (1.0 - s)
    rise = rise + lumps * np.clip(rise, 0, 10) * 0.35
    rise = np.where(inside, np.clip(rise, 0, top - GROUND_Z), top - GROUND_Z)
    bd.ground = bd.ground + rise.astype(np.int16)

    # The crown of the arch, ragged with hanging rock.
    crown = GROUND_Z + b * (1.0 + s) + 3.0 * (_vnoise(xs, ys, 7.0, 104) - 0.5) * 2.0
    crown = np.where(inside, crown, bd.size[2]).astype(np.int16)
    bd.ceiling = np.minimum(crown, bd.size[2])
    bd.tube_cx = cx[0]


# What the walls are made of: dark basalt, in a few warm greys, laid down in
# strata. It replaces the floor's tan tiles up the walls -- those were the
# "yellow" of the first cut, and they stripe when a cliff is a column of tile.
WALL_ROCK_10 = ((40, 30, 30), (54, 40, 36), (68, 50, 42), (82, 60, 46),
                (50, 44, 46), (34, 26, 28))
WALL_MIN_10 = GROUND_Z + 6     # ground height from which a column counts as wall


def rock_walls(bd):
    """Repaint the tunnel's rock: every voxel of a climbed column, and the ceiling laid
    over the arch, in strata of basalt."""
    xs = np.arange(bd.size[0], dtype=np.float64)[:, None]
    ys = np.arange(bd.size[1], dtype=np.float64)[None, :]
    wall = bd.ground >= WALL_MIN_10
    ceiling = bd.ceiling
    along = xs * 0.8 + ys * 0.6
    tones = [[BOOK.index(tuple(int(c * s) for c in rgb)) for s in (1.0, 0.82, 0.66)]
             for rgb in WALL_ROCK_10]
    tones = np.array(tones, np.uint16)                  # tone, shade
    grain = _vnoise(along, ys * 0.0 + 1, 3.0, 111)      # crumbly, along the wall
    for z in range(GROUND_Z - 2, bd.size[2]):
        layer = bd.a[:, :, z]
        rows = _vnoise(along, np.full_like(along, z * 1.0), 5.0, 112)
        strata = _vnoise(np.full_like(along, z * 1.0), np.full_like(along, 7.0), 3.0, 113)
        pick = np.clip((0.5 * rows + 0.5 * strata + 0.35 * (grain - 0.5)) * len(WALL_ROCK_10),
                       0, len(WALL_ROCK_10) - 1e-6).astype(int)
        shade = np.clip(((z - GROUND_Z) / 70.0 * 2.0 + 0.25 * (rows - 0.5)), 0, 1.999).astype(int)
        paint = tones[pick, shade]
        roof = (z >= ceiling) & (z < ceiling + CEILING_THICK_10)
        m = (wall & (layer > 0)) | roof
        layer[m] = paint[m]


# The far end of the bore. The tunnel is stopped off at TUBE_END_10 with a flat face of
# rock, and from the battle camera that face is a plainly visible ellipse -- the cut
# through the tube -- sitting dead ahead. Rather than pretend the tunnel goes on, the
# last stretch of it fades into the dark: every voxel from FADE_FROM_10 back is dimmed
# a little more the deeper it lies, so the walls run out into black and the end face is
# black, its rim melting into the rock round it.
FADE_FROM_10 = 126
FADE_LEVELS_10 = (1.0, 0.82, 0.66, 0.5, 0.36, 0.24, 0.14, 0.07, 0.03)


def fade_depth(bd):
    n = len(FADE_LEVELS_10) - 1
    span = float(TUBE_END_10 - FADE_FROM_10)
    for k in range(1, len(FADE_LEVELS_10)):
        # the rows whose fade lands on this level (the last level takes everything behind)
        y0 = FADE_FROM_10 + int(round(span * (k - 0.5) / n))
        y1 = bd.size[1] if k == n else FADE_FROM_10 + int(round(span * (k + 0.5) / n))
        if y0 >= y1:
            continue
        f = FADE_LEVELS_10[k]
        lut = np.zeros(len(BOOK.colours) + 1, np.uint16)
        for i, (r, g, b, _a) in enumerate(list(BOOK.colours)):
            lut[i + 1] = BOOK.index((r * f, g * f, b * f))
        block = bd.a[:, y0:y1, :]
        # lut may have grown while it was built (new dark colours); index by the old ones only
        block[:] = np.where(block > 0, lut[np.minimum(block, len(lut) - 1)], 0)


# Lava: from the flames' own palette, so it belongs to the same fire as the pillars.
LAVA_10 = ((255, 226, 110), (255, 170, 40), (240, 110, 18), (200, 60, 10))
CRUST_10 = ((78, 30, 20), (52, 22, 18))


def _face(bd, x, y, least=3):
    """How many voxels of the column at (x, y) the camera can see: from the
    ground of the lowest neighbour up to its own. On a steep wall that is many
    -- painting only the top few leaves a dotted line instead of a fall."""
    lo = int(bd.ground[max(0, x - 1):x + 2, max(0, y - 1):y + 2].min())
    return max(least, int(bd.ground[x, y]) - lo + 1)


def _lava_paint(bd, xs, ys, seed, depth=3, core_bias=0.0):
    """Turn the visible face of the columns (xs, ys) to molten rock."""
    palette = [BOOK.index(c) for c in LAVA_10]
    for x, y, w in zip(xs, ys, core_bias if np.ndim(core_bias) else [core_bias] * len(xs)):
        top = int(bd.ground[x, y])
        n = _vnoise(np.array([[x * 1.0]]), np.array([[y * 1.0]]), 3.0, seed)[0, 0]
        pick = int(np.clip((n * 0.9 + w * 0.5) * len(palette), 0, len(palette) - 1))
        pick = len(palette) - 1 - pick                      # 0 = hottest
        bd.a[x, y, max(0, top - _face(bd, x, y, depth) + 1):top + 1] = palette[pick]


def lava_pool(bd, cx, cy, rx, ry, seed):
    """A small pool sunk into the floor: hot in the middle, crusted at the rim."""
    xs = np.arange(bd.size[0], dtype=np.float64)[:, None]
    ys = np.arange(bd.size[1], dtype=np.float64)[None, :]
    wob = 0.22 * np.sin(np.arctan2(ys - cy, xs - cx) * 3 + seed) + 0.12 * np.sin(np.arctan2(ys - cy, xs - cx) * 5 + seed * 2)
    r = np.hypot((xs - cx) / rx, (ys - cy) / ry) + wob
    crust = [BOOK.index(c) for c in CRUST_10]
    inner = np.argwhere(r < 1.0)
    rim = np.argwhere((r >= 1.0) & (r < 1.3))
    for x, y in rim:
        top = int(bd.ground[x, y])
        bd.a[x, y, max(0, top - 1):top + 1] = crust[(x + y) % 2]
    for x, y in inner:
        # sunk one voxel below the rock round it
        top = int(bd.ground[x, y])
        bd.a[x, y, top:top + 1] = 0
        bd.ground[x, y] = top - 1
    xs_i, ys_i = inner[:, 0], inner[:, 1]
    heat = 1.0 - r[xs_i, ys_i]
    _lava_paint(bd, xs_i, ys_i, seed, depth=2, core_bias=heat)


def lava_flow(bd, x, y, seed, rnd, width=2.7, pool=(9, 6)):
    """A lava fall: from a crack high on the wall, down the steepest way to the floor.

    The way down is followed on the heightmap, wandering a little; the fall
    widens as it goes, its edge is crusted black-red and it ends in a small pool.
    """
    g = bd.ground.astype(np.float64)
    gx, gy = np.gradient(g)
    cx, cy = float(x), float(y)
    path = []
    heading = 0.0
    for _ in range(400):
        ix, iy = int(round(cx)), int(round(cy))
        if not (1 <= ix < bd.size[0] - 1 and 1 <= iy < bd.size[1] - 1):
            break
        if g[ix, iy] < GROUND_Z + 5:
            break
        path.append((cx, cy))
        vx, vy = -gx[ix, iy], -gy[ix, iy]
        n = math.hypot(vx, vy) or 1.0
        heading = 0.9 * heading + rnd.uniform(-0.9, 0.9)
        dx, dy = vx / n, vy / n
        cx += dx - dy * heading * 0.7
        cy += dy + dx * heading * 0.7
    if len(path) < 6:
        return
    crust = [BOOK.index(c) for c in CRUST_10]
    n = len(path)
    cells = {}
    # The fall does not run true: it swings from side to side across the face, a
    # few voxels each way, the way it would find its way round lumps of rock.
    swung = []
    for i, (px, py) in enumerate(path):
        qx, qy = path[min(i + 1, n - 1)][0] - path[max(i - 1, 0)][0], path[min(i + 1, n - 1)][1] - path[max(i - 1, 0)][1]
        norm = math.hypot(qx, qy) or 1.0
        swing = 2.6 * math.sin(i * 0.5 + seed) + 1.3 * math.sin(i * 1.3 + seed * 2.0)
        swung.append((px - qy / norm * swing, py + qx / norm * swing))
    path = swung
    for i, (px, py) in enumerate(path):
        w = width * (0.6 + 0.7 * i / n) * (0.8 + 0.5 * abs(math.sin(i * 0.23 + seed)))
        r = int(math.ceil(w + 1.4))
        for ox in range(-r, r + 1):
            for oy in range(-r, r + 1):
                px_i, py_i = int(round(px)) + ox, int(round(py)) + oy
                if not (0 <= px_i < bd.size[0] and 0 <= py_i < bd.size[1]):
                    continue
                dist = math.hypot(ox, oy)
                key = (px_i, py_i)
                if dist <= w:
                    cells[key] = max(cells.get(key, 0), 2 - dist / w)
                elif dist <= w + 1.4 and key not in cells:
                    cells[key] = -1
    core = [(k, v) for k, v in cells.items() if v > 0]
    for (px_i, py_i), v in cells.items():
        if v < 0:
            top = int(bd.ground[px_i, py_i])
            bd.a[px_i, py_i, max(0, top - _face(bd, px_i, py_i, 2) + 1):top + 1] = crust[(px_i + py_i) % 2]
    if core:
        xs = [k[0] for k, _ in core]
        ys = [k[1] for k, _ in core]
        heat = np.array([min(1.0, v / 2.0) for _, v in core])
        _lava_paint(bd, xs, ys, seed, depth=3, core_bias=heat)
    fx, fy = path[-1]
    lava_pool(bd, fx, fy, pool[0], pool[1], seed)


def recipe_10(bd):
    bd.undulate(amplitude=2.5, wavelength=60.0, seed=10)
    cavern(bd)

    # The rock is drawn in blobs several pixels across, wide enough to survive a
    # backdrop voxel, so it keeps more of its grain than grass or cobbles would;
    # and an ember keeps all of its, because dulling it is putting it out.
    bd.lay_ground(tiles_10, '10',
                  contrast=lambda t: 1.0 if t == EMBER_10 else 0.7)
    rock_walls(bd)
    fade_depth(bd)

    # Fire is the only light down here, so it goes where it will be seen against
    # the walls -- the columns up against the rock, the bowls out on the floor.
    # Sparingly. The first cut strewed them the way the map does -- seventy-one
    # of them, over a board forty-five tiles deep -- and packing that many into
    # one frame gave a bonfire with no cave left in it: fire is the only bright
    # thing down here, so a dozen of them is already a lot of picture.
    scatter_trees(bd, FIRE_COLUMNS_10,
                  boxes=[(163, 191, 60, 150, 0.5), (80, 94, 60, 150, 0.5)],
                  spacing=26, seed=20, scale=2, avoid=(TUBE_ZONE_10, LAVA_POOL_ZONE_10))
    scatter_trees(bd, FIRE_BOWLS_10,
                  boxes=[(163, 191, 28, 150, 0.45), (80, 94, 28, 150, 0.45)],
                  spacing=30, seed=30, scale=2, avoid=(TUBE_ZONE_10, LAVA_POOL_ZONE_10))

    # Two columns close to the camera, for the same reason chapter 04 has two
    # big pines there: without a foreground the cave is a painted flat.
    bd.stamp(obstacle('fire_pillar_2', 2), 174, 44)
    bd.stamp(obstacle('fire_pillar_3', 2), 72, 40)

    # Lava. A small pool on the floor, off the fighters' ground, and falls of it
    # running down the walls out of cracks in the rock, each ending in a puddle.
    rnd = random.Random(1010)
    lava_pool(bd, LAVA_POOL_10[0], LAVA_POOL_10[1], LAVA_POOL_10[2], LAVA_POOL_10[3], 5)
    for i, (side, at) in enumerate(LAVA_FALLS_10):
        start = _wall_start(bd, side, at)
        if start is not None:
            lava_flow(bd, start[0], start[1], 40 + i, rnd)


# The floor pool: (x, y, radius x, radius y). Screen right of the fighters and well
# inside the frame (at y 80 the frame spans about x 35..183).
LAVA_POOL_10 = (174, 110, 16, 10)
LAVA_POOL_ZONE_10 = (154, 194, 96, 124)

# Where the falls come out of the rock: the tunnel's side ('left' = large x, 'right' =
# small x) and the depth (y) along it. They start high on the curve and run down it.
LAVA_FALLS_10 = (('right', 84), ('left', 70), ('right', 108), ('left', 100), ('right', 136), ('left', 140))


def _wall_start(bd, side, at, height=GROUND_Z + 26):
    """The first point on the tunnel's side, going out from its middle, that is ``height`` up."""
    g = bd.ground
    mid = int(round(bd.tube_cx[at]))
    rng = range(mid, bd.size[0]) if side == 'left' else range(mid, -1, -1)
    for x in rng:
        if g[x, at] >= height:
            return x, at
    return None


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


def sink_colour(bd, mask):
    """Carry each masked column's surface colour all the way down.

    Open water and bottomless dark are one colour right through: packed with
    soil under a skin of blue, the sea shows a brown band wherever the battle
    camera catches the side of the model, and a pit shows brown walls.
    """
    top = np.clip(bd.ground.astype(np.int32), 0, bd.size[2] - 1)
    xs, ys = np.nonzero(mask)
    cols = bd.a[xs, ys, top[xs, ys]]
    for x, y, c, z in zip(xs, ys, cols, top[xs, ys]):
        if c:
            bd.a[x, y, :z + 1] = c


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


# --------------------------------------------------------------------------- #
# chapter 15 -- the lakeshore                                                 #
# --------------------------------------------------------------------------- #

# Chapter 15 is a great lake filling the left of the map, its shore a broad band
# of bare brown earth, with green pines in woods along the right and the pale,
# brown-barked pines massed in the far corners. A wooded island sits out in the
# water and plank bridges cross to it.
#
# The fight stands on the trodden shore. The lake opens out to screen right,
# brought forward the way chapter 06's basin was so it reads as water and not a
# thread, with the wooded island out in it; the green wood runs down screen left
# and the pale pines bank up behind.

GRASS_15 = (174, 174, 41, 37, 43, 30, 31)
LUSH_15 = (35, 175, 47, 44, 32)        # the darker turf in drifts
DIRT_15 = 177
DIRT_EDGE_15 = (9, 5, 10, 6, 2, 1, 7)  # grass with the earth showing through
WATER_15 = 173
WATER_Z_15 = GROUND_Z - 4

LAKE_15 = (1.5, 11.5, 10.5, 7.5)       # centre tx, ty, radius tx, ty
SHORE_15 = (15.0, 10.0, 5.0, 4.2)      # the trodden earth under the fight
ISLE_15 = (3.0, 10.5, 2.4, 1.8)        # the wooded island out in the water


def recipe_15(bd):
    bd.undulate(amplitude=2.0, wavelength=80.0, seed=15)
    bd.rise(y0=130, height=6)
    xs, ys = _grid(bd)
    tx, ty = xs / TILE_OUT, ys / TILE_OUT
    lake = _blob(tx, ty, LAKE_15, wob=0.15, seed=1.5)
    isle = _blob(tx, ty, ISLE_15, wob=0.2, seed=4.0)
    water = (lake < 1.0) & (isle >= 1.0)
    bd.ground[water] = WATER_Z_15
    bd.lay_ground(_pick(GRASS_15, 15), '15')

    lay_where(bd, _pick(LUSH_15, 115), '15', (_patchy(tx, ty, 15) > 0.7) & ~water)
    shore = _blob(tx, ty, SHORE_15, wob=0.3, seed=2.5)
    earth = ((lake < 1.25) | (shore < 1.0)) & ~water
    lay_where(bd, _pick(DIRT_EDGE_15, 215), '15',
              ((lake < 1.45) | (shore < 1.25)) & ~water & ~earth)
    lay_where(bd, lambda tx, ty: DIRT_15, '15', earth & (isle >= 0.75))
    lay_where(bd, lambda tx, ty: WATER_15, '15', water)

    def dry(cx, cy):
        l = _blob(cx / TILE_OUT, cy / TILE_OUT, LAKE_15, wob=0.15, seed=1.5)
        i = _blob(cx / TILE_OUT, cy / TILE_OUT, ISLE_15, wob=0.2, seed=4.0)
        return l > 1.35 or i < 0.7

    greens = ('tree_dark_green', 'tree_light_green')
    pale = ('tree_dark_gray', 'tree_light_gray')
    scatter_trees(bd, greens, boxes=[(10, 40, 76, 100, 0.9)],
                  spacing=8, jitter=2, seed=25, where=dry)       # the island
    scatter_trees(
        bd, greens,
        boxes=[
            (176, 256, 40, 140, 0.85),   # the green wood down screen left
            (0, 100, 146, 178, 0.85),    # the far shore, past the lake
            (150, 200, 120, 160, 0.5),
        ],
        spacing=8, seed=35, avoid=(FIGHTER_ZONE,), where=dry)
    scatter_trees(
        bd, pale,
        boxes=[
            (196, 256, 130, 178, 0.9),   # the pale pines massed in the far corner
            (92, 196, 160, 178, 0.8),    # and along the back
        ],
        spacing=9, seed=45, where=dry)
    bd.stamp(obstacle('tree_dark_green', 2), 196, 52)
    bd.stamp(obstacle('tree_light_gray', 2), 226, 108)


# --------------------------------------------------------------------------- #
# chapter 16 -- the snowfield                                                 #
# --------------------------------------------------------------------------- #

# Chapter 16 is the first snow: a white plain broken by ridges, each one a step
# with a ragged brown edge of rock along it, snow-laden trees in woods along
# the ridges, and in the middle a hollow of rough frozen ground with dark pools
# of open water in it and drifts of grey-green where the snow has blown thin.
#
# Chapter 11 already stepped a terrace across the back, so here the ridge does
# that job in snow and rock: the fight is out on the white, a pool lies open to
# screen right, the rough ice of the hollow round it, and the snow trees stand
# along the ridge top and down screen left.

SNOW_16 = (104, 104, 104, 41, 38, 42)
ICE_16 = (102,)                        # the rough frozen ground of the hollow
THIN_16 = 101                          # grey-green where the snow lies thin
WATER_16 = 105
ROCK_16 = 86
WATER_Z_16 = GROUND_Z - 3

POOL_16 = (5.0, 9.0, 4.6, 3.2)
HOLLOW_16 = (9.0, 10.0, 9.5, 6.0)
THIN_PATCH_16 = (26.0, 9.0, 3.0, 2.0)
RIDGE_Y_16 = 132
RIDGE_H_16 = 9


def _ridge_16(xs):
    return RIDGE_Y_16 + 9.0 * np.sin(xs / 27.0 + 0.6) + 4.0 * np.sin(xs / 8.0)


def recipe_16(bd):
    bd.undulate(amplitude=1.5, wavelength=90.0, seed=16)
    xs, ys = _grid(bd)
    tx, ty = xs / TILE_OUT, ys / TILE_OUT
    raise_where(bd, ys >= _ridge_16(xs), RIDGE_H_16)
    pool = _blob(tx, ty, POOL_16, wob=0.25, seed=1.6) < 1.0
    bd.ground[pool] = WATER_Z_16
    bd.lay_ground(_pick(SNOW_16, 16), '16')

    ridge = ys >= _ridge_16(xs) - 1
    hollow = _blob(tx, ty, HOLLOW_16, wob=0.35, seed=2.6)
    lay_where(bd, _pick(ICE_16, 116), '16', (hollow < 1.0) & ~pool & ~ridge)
    lay_where(bd, lambda tx, ty: THIN_16, '16',
              (_blob(tx, ty, THIN_PATCH_16, wob=0.3, seed=3.6) < 1.0) & ~ridge)
    lay_where(bd, lambda tx, ty: WATER_16, '16', pool)
    # The ridge step stays snow all the way down. Dressed in the rock tile
    # (rock_faces) its dark speckle came out as two dotted black lines across
    # the ground behind the fighters.
    g = bd.ground.astype(np.int32)
    p = np.pad(g, 1, mode='edge')
    low = np.minimum.reduce([p[2:, 1:-1], p[:-2, 1:-1], p[1:-1, 2:], p[1:-1, :-2]])
    sink_colour(bd, (g - low >= 3) & ~pool)

    def off_the_edge(cx, cy):
        edge = RIDGE_Y_16 + 9.0 * math.sin(cx / 27.0 + 0.6) + 4.0 * math.sin(cx / 8.0)
        return (abs(cy - edge) > 7
                and _blob(cx / TILE_OUT, cy / TILE_OUT, POOL_16, wob=0.25, seed=1.6) > 1.2)

    snowy = ('tree_snow_red', 'tree_snow_blue')
    scatter_trees(
        bd, snowy,
        boxes=[
            (0, 256, 140, 178, 0.8),     # along the ridge top
            (180, 256, 40, 130, 0.7),    # the wood down screen left
            (0, 30, 40, 130, 0.5),       # past the pool, screen right
        ],
        spacing=9, seed=26, avoid=(FIGHTER_ZONE,), where=off_the_edge)
    bd.stamp(obstacle('tree_snow_blue', 2), 194, 54)
    bd.stamp(obstacle('tree_snow_red', 2), 30, 116)


# --------------------------------------------------------------------------- #
# shared by 17, 18, 22, 23, 26, 29 -- a scene turned on the slant             #
# --------------------------------------------------------------------------- #

# A causeway, a bridge or a paved way laid straight up the camera axis puts
# the two fighters side by side across it, and one of them ends up over the
# water or the drop at its edge. Turned 45 degrees, the way runs across the
# frame on the slant, from the near screen-right corner away to the far
# screen-left, and both fighters stand on it.
#
# Turning the finished model would leave its corners empty in the frame, so
# these recipes are laid out in their own (u, v) frame instead: every mask is
# a function of ``turn.u`` / ``turn.v`` rather than x / y, and every model goes
# in through ``turn.stamp``. The design reads exactly as an unturned recipe
# does -- the way runs up v at u 116 -- and anything that belongs to the
# screen rather than the scene (``enclose``, ``FIGHTER_ZONE``) stays in x / y.

TURN_PIVOT = (116.0, 76.0)             # the ground the fighters are seen standing on
TURN_DEG = -45.0                       # in model space; the screen is mirrored, so
                                       # this turns the scene anticlockwise on screen


class Turn(object):
    def __init__(self, bd, deg=TURN_DEG, pivot=TURN_PIVOT):
        a = math.radians(deg)
        self.bd, self.c, self.s, self.p = bd, math.cos(a), math.sin(a), pivot
        xs, ys = np.broadcast_arrays(*_grid(bd))
        dx, dy = xs - pivot[0], ys - pivot[1]
        self.u = pivot[0] + self.c * dx + self.s * dy
        self.v = pivot[1] - self.s * dx + self.c * dy

    def world(self, u, v):
        du, dv = u - self.p[0], v - self.p[1]
        return (self.p[0] + self.c * du - self.s * dv,
                self.p[1] + self.s * du + self.c * dv)

    def inside(self, u, v):
        wx, wy = self.world(u, v)
        return 0 <= wx < self.bd.size[0] and 0 <= wy < self.bd.size[1]

    def stamp(self, m, x, y, z=None):
        """``bd.stamp`` with (x, y) the model's near-left corner in the design;
        the model keeps its own facing, only where it stands turns."""
        cx, cy = self.world(x + m.shape[0] / 2.0, y + m.shape[1] / 2.0)
        self.bd.stamp(m, int(round(cx - m.shape[0] / 2.0)),
                      int(round(cy - m.shape[1] / 2.0)), z)


def scatter_turned(bd, turn, keys, boxes, spacing=11, jitter=4, seed=3,
                   avoid=(), scale=SCALE, where=None):
    """``scatter_trees`` with ``boxes`` and ``where`` in the turned design and
    ``avoid`` on the screen (x / y, like ``FIGHTER_ZONE``)."""
    rnd = random.Random(seed)
    models = [obstacle(k, scale) for k in keys]
    for (bx0, bx1, by0, by1, density) in boxes:
        for gx in range(int(bx0), int(bx1), spacing):
            for gy in range(int(by0), int(by1), spacing):
                if rnd.random() > density:
                    continue
                x = gx + rnd.randint(-jitter, jitter)
                y = gy + rnd.randint(-jitter, jitter)
                m = models[rnd.randrange(len(models))]
                cu, cv = x + m.shape[0] / 2.0, y + m.shape[1] / 2.0
                wx, wy = turn.world(cu, cv)
                if not turn.inside(cu, cv):
                    continue
                if any(ax0 <= wx <= ax1 and ay0 <= wy <= ay1
                       for (ax0, ax1, ay0, ay1) in avoid):
                    continue
                if where is not None and not where(cu, cv):
                    continue
                turn.stamp(m, x, y)


def railing_turned(bd, turn, u0, v_end, z, height=11, spacing=13, width=3):
    """Chapter 03's ``railing``, running up v instead of y."""
    post, dark = BOOK.index(RAIL_RED_03), BOOK.index(RAIL_DARK_03)
    band = (turn.u >= u0) & (turn.u < u0 + width) & (turn.v < v_end)
    posts = band & (np.mod(turn.v, spacing) < 2.0)
    far = posts & (np.mod(turn.v, spacing) >= 1.0)
    for zz in range(z - 4, z + height):
        bd.a[posts, zz] = post
        bd.a[far, zz] = dark
    for dz in (height - 2, height - 6):
        bd.a[band, z + dz] = post
        bd.a[band, z + dz - 1] = post
        bd.a[band, z + dz - 2] = dark


# --------------------------------------------------------------------------- #
# chapter 17 -- the ice island                                                #
# --------------------------------------------------------------------------- #

# Chapter 17 is an island of ice in a dark sea, laid out like a target: a ring
# road of ice round the outside, open water inside it, and in the middle a hill
# stepped up in terraces with a stone stair climbing to the top. Four causeways
# cross the water to the ring from the edges of the map, the south one lined
# with snow-laden pines in pairs.
#
# The backdrop stands on the south causeway and looks up it: ice under the
# fighters, sea opening on both sides, the ring road crossing the frame, more
# water, and the terraced hill with its stair dead ahead. Like chapter 03's
# bridge, the converging rows -- here the pines -- carry the eye to it.

ICE_17 = (102,)
SEA_17 = 105
STAIR_17 = 196                         # the middle of the stair's three-by-three
TERRACE_17 = (48, 49, 50, 102)
TERRACE_ROCK_17 = 103
WATER_Z_17 = GROUND_Z - 5

HILL_17 = (116.0, 214.0)               # centre of the rings, behind the model
RING_R_17 = (78.0, 96.0)               # the ring road's inner and outer radius
HILL_R_17 = (66.0, 50.0)               # where each terrace step begins
CAUSEWAY_HW_17 = 30


def recipe_17(bd):
    turn = Turn(bd)
    xs, ys = turn.u, turn.v                # the design, laid out turned
    d = np.hypot(xs - HILL_17[0], ys - HILL_17[1])
    causeway = (np.abs(xs - HILL_17[0]) < CAUSEWAY_HW_17) & (d > RING_R_17[1] - 4)
    ring = (d >= RING_R_17[0]) & (d < RING_R_17[1])
    hill = d < HILL_R_17[0]
    land = causeway | ring | hill
    bd.ground[:, :] = GROUND_Z
    bd.ground[~land] = WATER_Z_17
    bd.ground[hill] += 5
    bd.ground[d < HILL_R_17[1]] += 5
    stair = hill & (np.abs(xs - HILL_17[0]) < 13)
    t = np.clip((HILL_R_17[0] + 2 - d) / 20.0, 0, 1)
    bd.ground[stair] = (GROUND_Z + 2 * np.round(t * 5)).astype(np.int16)[stair]

    bd.lay_ground(lambda tx, ty: SEA_17, '17')
    lay_where(bd, _pick(ICE_17, 17), '17', land)
    lay_where(bd, _pick(TERRACE_17, 117), '17', hill & (d >= HILL_R_17[1]) & ~stair)
    lay_where(bd, lambda tx, ty: STAIR_17, '17', stair)
    rock_faces(bd, '17', TERRACE_ROCK_17, min_rise=3)
    sink_colour(bd, ~land)

    # The pines in pairs down the causeway, the way the map lines its south
    # approach, and a few up on the terraces either side of the stair. None
    # goes where the fighters are drawn.
    pine = obstacle('pine_snow', 2)
    for y in range(-40, 120, 22):
        for x in (HILL_17[0] - CAUSEWAY_HW_17 + 2, HILL_17[0] + CAUSEWAY_HW_17 - 8):
            if abs(math.hypot(x + 3 - HILL_17[0], y + 3 - HILL_17[1])
                   - (RING_R_17[0] + RING_R_17[1]) / 2) < 12:
                continue
            wx, wy = turn.world(x + 3, y + 3)
            if (not turn.inside(x + 3, y + 3)
                    or (FIGHTER_ZONE[0] - 10 <= wx <= FIGHTER_ZONE[1] and wy < 110)):
                continue
            turn.stamp(pine, int(x), y)
    for x, y in ((76, 158), (146, 158), (88, 170), (134, 170)):
        turn.stamp(pine, x, y)


# --------------------------------------------------------------------------- #
# chapter 18 -- the chasm bridge                                              #
# --------------------------------------------------------------------------- #

# Chapter 18 is a single plank bridge slung across a bottomless black chasm,
# from one grassy clifftop to another, with blue spruce on the heights and a
# wooden stair cut down each cliff face.
#
# So the fight happens on the bridge itself, the way chapter 03's does -- but
# where 03 has water under the deck this has nothing: black, all the way down.
# The deck runs away from the camera to the far cliff, its red rails
# converging on the clifftop, and the spruce stand along the edge over the drop.

GRASS_18 = (168, 169, 165, 164, 144, 146)
EARTH_18 = 177
VOID_18 = 247
CLIFF_ROCK_18 = 33
CHASM_Z_18 = 1

BRIDGE_X_18 = (92, 140)                # the deck, on the camera axis
CLIFF_Y_18 = 132                       # where the far clifftop begins
PLANK_18 = ((128, 92, 52), (112, 78, 42), (98, 68, 36))
PLANK_GAP_18 = (52, 34, 18)


def _cliff_18(xs):
    return CLIFF_Y_18 + 6.0 * np.sin(xs / 21.0 + 0.3) + 3.0 * np.sin(xs / 7.0)


def recipe_18(bd):
    turn = Turn(bd)
    xs, ys = turn.u, turn.v                # the design, laid out turned
    land = ys >= _cliff_18(xs)
    bd.undulate(amplitude=1.5, wavelength=70.0, seed=18)
    t = np.clip((ys - CLIFF_Y_18) / float(bd.size[1] - CLIFF_Y_18), 0, 1)
    bd.ground = (bd.ground + t ** 2 * 6).astype(np.int16)      # ``rise``, turned
    bd.ground[~land] = CHASM_Z_18
    bd.lay_ground(_pick(GRASS_18, 18), '18')
    head = land & (np.abs(xs - 116) < 30) & (ys < CLIFF_Y_18 + 22)
    lay_where(bd, lambda tx, ty: EARTH_18, '18', head)
    lay_where(bd, lambda tx, ty: VOID_18, '18', ~land)
    sink_colour(bd, ~land)
    rock_faces(bd, '18', CLIFF_ROCK_18, min_rise=4)

    # The deck, plank by plank across its width, with a dark gap between each.
    x0, x1 = BRIDGE_X_18
    end = int(CLIFF_Y_18 + 8)
    deck = (xs >= x0) & (xs < x1) & (ys < end)
    vi = np.floor(ys).astype(int)
    planks = np.array([BOOK.index(c) for c in PLANK_18], np.uint16)
    plank = planks[np.mod((vi // 4) * 7 + 3, len(planks))]
    colour = np.where(np.mod(vi, 4) == 3, BOOK.index(PLANK_GAP_18), plank)
    for z in (GROUND_Z - 1, GROUND_Z):
        bd.a[deck, z] = colour[deck]
    railing_turned(bd, turn, x0 - 3, end, GROUND_Z + 1)
    railing_turned(bd, turn, x1, end, GROUND_Z + 1)

    def on_top(cx, cy):
        return cy > _cliff_18(np.array(cx, dtype=np.float64)) + 6
    scatter_turned(
        bd, turn, ('tree_blue', 'tree_blue', 'tree_dark_green'),
        boxes=[
            (-90, 84, 136, 270, 0.8),    # the heights either side of the bridge head
            (148, 330, 136, 270, 0.8),
            (84, 148, 158, 270, 0.7),    # and closing the far side
        ],
        spacing=9, seed=28, where=on_top)


# --------------------------------------------------------------------------- #
# chapter 19 -- the red and blue wood                                         #
# --------------------------------------------------------------------------- #

# Chapter 19 is a wood planted in belts: thick bands of pines, red and blue
# mixed together, lying across the map with lanes of open grass between them,
# dark turf in their shade, pale sandy tracks worn across the lanes and two
# patches of grey gravel.
#
# The fight is in one of the lanes. A belt of trees comes in from screen left,
# another stands off to screen right beyond a patch of gravel, and the next
# belt closes the far side; a sandy track is worn across the lane where the
# fighters stand.

GRASS_19 = (61, 61, 80, 83, 92, 84, 88)
SHADE_19 = (62, 62, 0, 12, 15, 3, 1)
SAND_19 = 63
GRAVEL_19 = 60

SAND_PATCH_19 = (16.0, 11.0, 4.2, 1.8)
GRAVEL_PATCH_19 = (5.0, 13.0, 2.6, 3.4)
REDBLUE_19 = ('tree_dark_red', 'tree_blue', 'tree_light_red', 'tree_blue')


def recipe_19(bd):
    bd.undulate(amplitude=1.5, wavelength=80.0, seed=19)
    bd.rise(y0=140, height=5)
    bd.lay_ground(_pick(GRASS_19, 19), '19')
    xs, ys = _grid(bd)
    tx, ty = xs / TILE_OUT, ys / TILE_OUT

    # The shade the belts throw: dark turf under and just out from each one,
    # its edge ragged rather than ruled.
    w = 10.0 * (_vnoise(xs, ys, 9.0, 191) - 0.5)
    shade = (((xs + w > 156) & (ys + w < 128)) | ((xs + w < 76) & (ys - w > 52))
             | (ys + w > 136))
    lay_where(bd, _pick(SHADE_19, 119), '19', shade | (_patchy(tx, ty, 19) > 0.78))
    lay_where(bd, lambda tx, ty: SAND_19, '19',
              _blob(tx, ty, SAND_PATCH_19, wob=0.35, seed=1.9) < 1.0)
    gravel = _blob(tx, ty, GRAVEL_PATCH_19, wob=0.35, seed=2.9) < 1.0
    lay_where(bd, lambda tx, ty: GRAVEL_19, '19', gravel)

    def off_gravel(cx, cy):
        return _blob(cx / TILE_OUT, cy / TILE_OUT, GRAVEL_PATCH_19, wob=0.35, seed=2.9) > 1.5
    scatter_trees(
        bd, REDBLUE_19,
        boxes=[
            (172, 256, 36, 116, 0.85),   # the belt coming in from screen left
            (0, 60, 64, 140, 0.85),      # the one standing off to screen right
            (0, 256, 150, 178, 0.9),     # and the next belt, closing the far side
        ],
        spacing=8, jitter=3, seed=29, avoid=(FIGHTER_ZONE,), where=off_gravel)
    bd.stamp(obstacle('tree_dark_red', 2), 186, 50)
    bd.stamp(obstacle('tree_blue', 2), 210, 70)


# --------------------------------------------------------------------------- #
# shared by 20..30 -- closing the picture in                                  #
# --------------------------------------------------------------------------- #

# The battle scene's sky is the same bright day for every chapter, and from
# chapter 22 on the game is fought indoors, or underground, or on rock hanging
# in a void: a blue sky over any of those is wrong. Chapter 10 closed its cave
# with a tunnel; these close theirs with a wall -- across the back, high enough
# that its top is over the top of the frame, and down both sides where the
# frame looks past the edge of the model.

ENCLOSE_TOP = 89                       # the frame's top edge is about z 84 at the back
VOID = ((8, 8, 12),)                   # nothing at all: the black the maps are cut from


def enclose(bd, tones, back=168, sides=0, side_from=70, top=ENCLOSE_TOP,
            ragged=5.0, seed=0):
    """Wall the back of the model off, and its sides if ``sides`` is non-zero.

    ``tones`` are the RGB colours the wall is laid in, as strata across it --
    one colour for a void, a few greys or browns for rock. ``back`` is the y it
    rises at and ``sides`` how thick the side walls are; both wander by
    ``ragged`` voxels so the foot of the wall is not ruled. Returns the mask of
    the columns it took, for anything that should keep off them.
    """
    xs, ys = _grid(bd)
    # The wander only ever brings the wall forward, so the back row and the
    # outer columns are always wall: a wall that wanders back off the edge of
    # the model leaves a slot of sky down it.
    aw = ragged * np.abs(2.0 * (_vnoise(xs, ys + 7.0, 13.0, seed) - 0.5))
    wall = ys + aw >= back
    if sides:
        wall |= ((xs < sides + aw) | (xs >= bd.size[0] - sides - aw)) & (ys >= side_from)
    bd.ground[wall] = top
    idx = np.array([BOOK.index(t) for t in tones], np.uint16)
    along = xs * 0.7 + ys * 0.7
    for z in range(0, top + 1):
        band = _vnoise(along, np.full_like(along, z * 1.0), 6.0, seed + 31)
        pick = np.clip((band * len(idx)).astype(int), 0, len(idx) - 1)
        layer = bd.a[:, :, z]
        layer[wall] = idx[pick][wall]
    return wall


# --------------------------------------------------------------------------- #
# chapter 20 -- the blue heights                                              #
# --------------------------------------------------------------------------- #

# Chapter 20 is fought by night on a mountain: blue rock underfoot, grey-green
# terraces stepping up in rings to a summit, stone stairs between them, and a
# great black gulf curling round the whole of it. Blue spruce and the tall
# blue pines stand about the edges.
#
# The fight is on the blue rock below the first terrace. The terraces step up
# across the back, a stair climbing them on the camera axis, and the gulf
# opens black to screen right.

ROCK_20 = (21, 21, 21, 0, 3, 1)
TERRACE_20 = (22, 22, 22, 67, 65, 66)
STAIR_TOP_20, STAIR_LOW_20 = 81, 90
GULF_20 = 20
TERRACE_FACE_20 = 13
GULF_Z_20 = 1

GULF_SPEC_20 = (1.5, 9.0, 6.0, 5.0)     # centre tx, ty, radius tx, ty
STEPS_20 = ((112, 7), (146, 7))         # y each terrace starts at, and its rise


def recipe_20(bd):
    bd.undulate(amplitude=1.5, wavelength=80.0, seed=20)
    xs, ys = _grid(bd)
    tx, ty = xs / TILE_OUT, ys / TILE_OUT
    edge = 6.0 * np.sin(xs / 19.0 + 0.4) + 3.0 * np.sin(xs / 7.0)
    stair = (np.abs(xs - 118) < 14) & (ys >= 0)
    for y0, h in STEPS_20:
        raise_where(bd, (ys >= y0 + edge) & ~stair, h)
    # The stair itself climbs in two-voxel treads instead of a step.
    total = sum(h for _y, h in STEPS_20)
    t = np.clip((ys - STEPS_20[0][0] + 6) / float(STEPS_20[-1][0] - STEPS_20[0][0] + 6), 0, 1)
    bd.ground[stair] = (bd.ground + 2 * np.round(t * total / 2.0)).astype(np.int16)[stair]
    gulf = _blob(tx, ty, GULF_SPEC_20, wob=0.3, seed=2.0) < 1.0
    bd.ground[gulf] = GULF_Z_20

    bd.lay_ground(_pick(ROCK_20, 20), '20')
    lay_where(bd, _pick(TERRACE_20, 120), '20', (ys >= STEPS_20[0][0] + edge - 1) & ~stair)
    lay_where(bd, lambda tx, ty: STAIR_TOP_20 if int(ty) % 2 else STAIR_LOW_20,
              '20', stair & (ys >= STEPS_20[0][0] - 8), contrast=1.0)
    lay_where(bd, lambda tx, ty: GULF_20, '20', gulf)
    sink_colour(bd, gulf)
    rock_faces(bd, '20', TERRACE_FACE_20, min_rise=4)

    def placed(cx, cy):
        e = 6.0 * math.sin(cx / 19.0 + 0.4) + 3.0 * math.sin(cx / 7.0)
        return (all(abs(cy - (y0 + e)) > 6 for y0, _h in STEPS_20)
                and abs(cx - 118) > 18
                and _blob(cx / TILE_OUT, cy / TILE_OUT, GULF_SPEC_20, wob=0.3, seed=2.0) > 1.2)
    scatter_trees(
        bd, ('tree_blue', 'tree_blue', 'pine_blue'),
        boxes=[
            (0, 256, 156, 178, 0.75),    # up on the summit
            (176, 256, 40, 150, 0.6),    # down screen left
            (60, 96, 118, 150, 0.5),     # on the first terrace, screen right
        ],
        spacing=10, seed=30, avoid=(FIGHTER_ZONE,), where=placed)
    bd.stamp(obstacle('pine_blue', 2), 200, 60)


# --------------------------------------------------------------------------- #
# chapter 21 -- the forest temple                                             #
# --------------------------------------------------------------------------- #

# Chapter 21 is a great platform of dark stone standing in a clearing of
# trodden earth, deep in a wood of pale brown-barked pines. Earthen roads come
# in to it from all four sides between rows of stone posts, big stone slabs
# stand at the corners of the clearing, and wide stairs climb the platform.
#
# The backdrop comes in along one of those roads: earth under the fighters, the
# posts lining it either side, and at its end the platform rising across the
# back with its great stair on the camera axis, a slab off to each side and the
# pale wood crowding in at both edges.

GRASS_21 = (34, 34, 34, 108, 109, 112, 115)
EARTH_21 = 33
PLATFORM_21 = (32, 32, 32, 98, 96, 100, 101)
PLATFORM_FACE_21 = 53
STAIR_21 = (260, 266)

ROAD_HW_21 = 22                        # the road's half-width, about the camera axis
PLATFORM_Y_21 = 128
PLATFORM_H_21 = 7
CLEARING_21 = (14.5, 17.0, 11.0, 8.0)


def recipe_21(bd):
    bd.undulate(amplitude=1.0, wavelength=90.0, seed=21)
    xs, ys = _grid(bd)
    tx, ty = xs / TILE_OUT, ys / TILE_OUT
    stair = (np.abs(xs - 118) < 18) & (ys >= PLATFORM_Y_21 - 10) & (ys < PLATFORM_Y_21 + 2)
    platform = (ys >= PLATFORM_Y_21) & (np.abs(xs - 118) < 104)
    bd.terrace(0, bd.size[0], PLATFORM_Y_21 - 30, bd.size[1], GROUND_Z)
    raise_where(bd, platform, PLATFORM_H_21)
    t = np.clip((ys - (PLATFORM_Y_21 - 10)) / 12.0, 0, 1)
    bd.ground[stair] = (GROUND_Z + np.round(t * PLATFORM_H_21) + 0 * xs).astype(np.int16)[stair]

    bd.lay_ground(_pick(GRASS_21, 21), '21')
    clearing = _blob(tx, ty, CLEARING_21, wob=0.25, seed=2.1) < 1.0
    road = np.abs(xs - 118) < ROAD_HW_21
    lay_where(bd, lambda tx, ty: EARTH_21, '21', (clearing | road) & ~platform)
    lay_where(bd, _pick(PLATFORM_21, 121), '21', platform & ~stair)
    lay_where(bd, lambda tx, ty: STAIR_21[int(ty * 2) % 2], '21', stair, contrast=1.0)
    rock_faces(bd, '21', PLATFORM_FACE_21, min_rise=3)

    # The posts along the road, in alternating pairs, the way chapter 09's
    # avenue is set out; then a slab either side of the stair.
    for i, y in enumerate(range(50, PLATFORM_Y_21 - 18, 22)):
        key = ('stone_column_1', 'stone_column_2')[i % 2]
        bd.stamp(obstacle(key, 2), 118 - ROAD_HW_21 - 10, y)
        bd.stamp(obstacle(key, 2), 118 + ROAD_HW_21 + 2, y)
    bd.stamp(obstacle('stone_shrine_1', 2), 40, PLATFORM_Y_21 + 10)
    bd.stamp(obstacle('stone_shrine_1', 2), 168, PLATFORM_Y_21 + 10)

    scatter_trees(
        bd, ('tree_dark_gray', 'tree_light_gray'),
        boxes=[
            (0, 76, 20, PLATFORM_Y_21 - 8, 0.85),     # the pale wood, screen right
            (164, 256, 20, PLATFORM_Y_21 - 8, 0.85),  # and screen left
            (0, 30, PLATFORM_Y_21 + 8, 178, 0.8),     # round the platform's ends
            (220, 256, PLATFORM_Y_21 + 8, 178, 0.8),
        ],
        spacing=8, jitter=3, seed=31, avoid=(FIGHTER_ZONE, (70, 168, 0, 180)),
        where=lambda cx, cy: not (_blob(cx / TILE_OUT, cy / TILE_OUT, CLEARING_21,
                                        wob=0.25, seed=2.1) < 1.05))


# --------------------------------------------------------------------------- #
# chapter 22 -- the sanctum of the orbs                                       #
# --------------------------------------------------------------------------- #

# Chapter 22 is a round sanctum out in the dark: a lawn ringed by ramparts of
# black stone that step up and up to its rim, a raised floor of dark flagstones
# in the middle with a great altar slab on it, six pillars each holding up an
# orb of a different colour, and armoured statues keeping watch.
#
# The fight is on the lawn. The flagstones and the altar are behind it, the six
# orbs standing in an arc round them, and the ramparts close in at the back and
# both sides -- and past their rim there is nothing, so the wall behind is the
# black the map is cut from, not sky.

LAWN_22 = (74, 74, 74, 20, 21, 22)
FLAGS_22 = (76, 76, 76, 103, 100, 101)
RAMPART_22 = (75, 75, 13)
RAMPART_FACE_22 = 49

DAIS_Y_22 = 108
RING_22 = (116.0, 64.0, 104.0)         # centre x, y of the sanctum, and its radius
ORBS_22 = ('orb_pillar_red', 'orb_pillar_orange', 'orb_pillar_yellow',
           'orb_pillar_green', 'orb_pillar_blue', 'orb_pillar_purple')


def recipe_22(bd):
    turn = Turn(bd)
    xs, ys = turn.u, turn.v                # the design, laid out turned
    cx, cy, r = RING_22
    d = np.hypot((xs - cx) / 1.25, ys - cy)
    bd.ground[:, :] = GROUND_Z
    rampart = d >= r * 0.82
    raise_where(bd, rampart, 6)
    raise_where(bd, d >= r * 0.92, 6)
    dais = (ys >= DAIS_Y_22) & (np.abs(xs - cx) < 52) & ~rampart
    raise_where(bd, dais, 3)

    bd.lay_ground(_pick(LAWN_22, 22), '22')
    lay_where(bd, _pick(FLAGS_22, 122), '22',
              dais | ((np.abs(xs - cx) < 14) & (ys < DAIS_Y_22)))
    lay_where(bd, _pick(RAMPART_22, 222), '22', rampart)
    rock_faces(bd, '22', RAMPART_FACE_22, min_rise=3)
    enclose(bd, VOID, back=174, sides=4, seed=22)

    turn.stamp(obstacle('stone_shrine_2', 2), int(cx) - 30, DAIS_Y_22 + 24)
    for i, key in enumerate(ORBS_22):
        a = math.radians(200 + i * 28)
        turn.stamp(obstacle(key, 2), int(cx + 62 * math.cos(a)) - 4,
                 int(DAIS_Y_22 + 18 - 30 * math.sin(a)))
    for x, y in ((40, 96), (186, 96), (60, 124), (166, 124)):
        turn.stamp(obstacle('stone_statue_1', 2), x, y)
    for x, y in ((84, 96), (142, 96)):
        turn.stamp(obstacle('stone_column_2', 2), x, y)


# --------------------------------------------------------------------------- #
# chapter 23 -- the temple in the hills                                       #
# --------------------------------------------------------------------------- #

# Chapter 23 is a temple of black stone laid out on a green hilltop, ringed by
# brown rock that climbs away into the dark on every side. Its two wings
# reach forward either side of a stair, posts and pillars stand along its
# terraces, armoured statues guard the approach, and in its heart is a mound of
# bare earth with a ring of stone posts on it.
#
# The fight is on the turf of the approach, on the paved way up between the
# wings. The temple's terrace runs across the back with its posts and statues,
# the brown rock rises steeply at both sides, and over all of it is the dark.

TURF_23 = (74, 74, 74, 20, 21, 22, 29)
PAVE_23 = (76, 76, 76)
ROCK_23 = 34
EARTH_23 = 17

TERRACE_Y_23 = 118
TERRACE_H_23 = 5


def recipe_23(bd):
    bd.undulate(amplitude=1.0, wavelength=90.0, seed=23)
    turn = Turn(bd)
    xs, ys = turn.u, turn.v                # the design, laid out turned
    tx, ty = xs / TILE_OUT, ys / TILE_OUT
    # The hills: rock climbing away at both sides, steepening as it goes.
    off = np.abs(xs - 116) - 80 - 10.0 * np.sin(ys / 23.0 + 0.5) + np.clip(ys - 150, 0, None) * 2.0
    hills = np.clip(off, 0, None) ** 1.2 * 0.45
    hills = hills + 6.0 * _vnoise(xs, ys, 8.0, 230) * (hills > 0)
    bd.ground = np.minimum(bd.ground + hills, ENCLOSE_TOP - 4).astype(np.int16)
    rocky = hills > 1.5
    terrace = (ys >= TERRACE_Y_23) & ~rocky
    raise_where(bd, terrace, TERRACE_H_23)
    stair = (np.abs(xs - 116) < 14) & (ys >= TERRACE_Y_23 - 8) & (ys < TERRACE_Y_23 + 2)
    t = np.clip((ys - (TERRACE_Y_23 - 8)) / 10.0, 0, 1)
    bd.ground[stair] = (GROUND_Z + np.round(t * TERRACE_H_23) + 0 * xs).astype(np.int16)[stair]

    bd.lay_ground(_pick(TURF_23, 23), '23')
    way = (np.abs(xs - 116) < 16) & (ys < TERRACE_Y_23)
    wings = ((np.abs(xs - 116) > 34) & (np.abs(xs - 116) < 54)
             & (ys > TERRACE_Y_23 - 46) & ~rocky)
    lay_where(bd, _pick(PAVE_23, 123), '23', way | wings | terrace)
    mound = _blob(tx, ty, (14.5, 19.5, 4.0, 2.2), wob=0.3, seed=2.3) < 1.0
    lay_where(bd, lambda tx, ty: EARTH_23, '23', mound & terrace)
    lay_where(bd, lambda tx, ty: ROCK_23, '23', rocky)
    rock_faces(bd, '23', ROCK_23, min_rise=3)
    enclose(bd, VOID, back=176, sides=3, seed=23)

    for x in (62, 160):                       # the wings' posts and statues
        for y in (78, 100):
            turn.stamp(obstacle('stone_column_1', 2), x, y)
    for x, y in ((92, 96), (130, 96)):
        turn.stamp(obstacle('stone_statue_1', 2), x, y)
    for i, x in enumerate(range(86, 150, 16)):  # the ring of posts on the mound
        turn.stamp(obstacle(('stone_pillar_1', 'stone_column_2')[i % 2], 2), x,
                 TERRACE_Y_23 + 26 + (6 if i in (1, 2) else 0))


# --------------------------------------------------------------------------- #
# chapter 24 -- the floating rock                                             #
# --------------------------------------------------------------------------- #

# Chapter 24 is a single island of brown rock hanging in black nothing, held up
# on pillars of pale light that fall away beneath it. On top is a patch of
# black flagstones with three statues on it, ringed by stone posts.
#
# The fight is out on the rock. The flagstones and their statues are behind,
# the island's edge breaks off into the void a little beyond them and down
# both sides, and the pillars of light stand up out of the dark past the edge.

EARTH_24 = (43, 43, 22, 21, 25, 26)
FLAGS_24 = 72
CLIFF_24 = 27
VOID_24 = 42
VOID_Z_24 = 1
ISLE_24 = (116.0, 76.0, 96.0, 86.0)    # centre x, y, radius x, y in voxels
FLAGS_SPEC_24 = (14.5, 14.5, 4.0, 2.6)
BEAM_24 = ((255, 255, 236), (252, 248, 204), (248, 226, 150))
BEAMS_24 = ((24, 152, 6), (210, 150, 6), (150, 170, 9))


def light_beam(bd, cx, cy, r, tones):
    """A column of light standing up out of the void: white-hot in the core,
    burning orange at the rim, from the floor of the model to its top."""
    core, mid, rim = [BOOK.index(t) for t in tones]
    for x in range(int(cx - r) - 1, int(cx + r) + 2):
        for y in range(int(cy - r) - 1, int(cy + r) + 2):
            if not (0 <= x < bd.size[0] and 0 <= y < bd.size[1]):
                continue
            d = math.hypot(x + 0.5 - cx, y + 0.5 - cy) / r
            if d > 1.0:
                continue
            bd.a[x, y, :] = core if d < 0.55 else mid if d < 0.85 else rim


def recipe_24(bd):
    xs, ys = _grid(bd)
    tx, ty = xs / TILE_OUT, ys / TILE_OUT
    cx, cy, rx, ry = ISLE_24
    e = np.hypot((xs - cx) / rx, (ys - cy) / ry)
    e = e + 0.08 * np.sin(np.arctan2(ys - cy, xs - cx) * 7.0) + 0.1 * (_vnoise(xs, ys, 6.0, 240) - 0.5)
    isle = e < 1.0
    bd.undulate(amplitude=1.0, wavelength=60.0, seed=24)
    bd.ground[~isle] = VOID_Z_24
    bd.lay_ground(_pick(EARTH_24, 24), '24')
    flags = (_blob(tx, ty, FLAGS_SPEC_24, wob=0.35, seed=2.4) < 1.0) & isle
    lay_where(bd, lambda tx, ty: FLAGS_24, '24', flags)
    lay_where(bd, lambda tx, ty: VOID_24, '24', ~isle)
    sink_colour(bd, ~isle)
    rock_faces(bd, '24', CLIFF_24, min_rise=4)
    enclose(bd, VOID, back=176, sides=3, seed=24)
    for x, y, r in BEAMS_24:
        light_beam(bd, x, y, r, BEAM_24)

    for x, y in ((100, 118), (118, 124), (136, 118)):
        bd.stamp(obstacle('stone_statue_1', 2), x - 6, y)
    for x, y in ((70, 108), (162, 108), (84, 132), (148, 132)):
        bd.stamp(obstacle('stone_column_2', 2), x - 6, y)


# --------------------------------------------------------------------------- #
# chapter 25 -- the lava caves                                                #
# --------------------------------------------------------------------------- #

# Chapter 25 is the second fire cavern, and nothing like chapter 10's tunnel:
# a broad floor of brown rock riddled with black pits, rivers of white-hot lava
# running down through it in long falls, and fire everywhere -- pillars of it,
# bowls of it, gouts of it on the brink of every pit.
#
# The fight is on the open rock. A lava river crosses behind it on the slant,
# a pit opens black to screen right, the fires stand about the floor, and the
# cave rock closes the picture in where 10's arched over it.

FLOOR_25 = (66, 66, 66, 83, 88, 77, 80)
PIT_25 = 51
PIT_FACE_25 = 40
PIT_Z_25 = 1
CAVE_TONES_25 = ((70, 48, 26), (90, 62, 32), (54, 37, 20), (104, 73, 40), (62, 42, 22))

PIT_SPEC_25 = (2.5, 8.0, 4.6, 3.4)
RIVER_HW_25 = 30.0     # across x; the river runs at a slant, so it is about 10 wide


def _river_cx_25(ys):
    """The lava river's middle: in from the far screen-left corner, crossing
    the frame behind the fighters, and away off the screen-right side."""
    return 116.0 + 3.0 * (ys - 140.0) + 10.0 * np.sin(ys / 9.0)


def recipe_25(bd):
    bd.undulate(amplitude=2.0, wavelength=60.0, seed=25)
    xs, ys = _grid(bd)
    tx, ty = xs / TILE_OUT, ys / TILE_OUT
    pit = _blob(tx, ty, PIT_SPEC_25, wob=0.3, seed=2.5) < 1.0
    river_d = np.abs(xs - _river_cx_25(ys)) / RIVER_HW_25
    river = (river_d < 1.0) & ~pit
    bd.ground[pit] = PIT_Z_25
    bd.ground[river] -= 1

    bd.lay_ground(_pick(FLOOR_25, 25), '25', contrast=0.7)
    lay_where(bd, lambda tx, ty: PIT_25, '25', pit)
    sink_colour(bd, pit)
    rock_faces(bd, '25', PIT_FACE_25, min_rise=4)

    # The river is painted the way chapter 10's lava is, so the two caves burn
    # with the same fire: hottest down the middle, a black crust at the banks.
    crust = [BOOK.index(c) for c in CRUST_10]
    bank = (river_d >= 1.0) & (river_d < 1.35) & ~pit
    for x, y in zip(*np.nonzero(bank)):
        top = int(bd.ground[x, y])
        bd.a[x, y, max(0, top - 1):top + 1] = crust[(x + y) % 2]
    rx, ry = np.nonzero(river)
    _lava_paint(bd, rx, ry, 25, depth=2, core_bias=1.0 - river_d[rx, ry])
    enclose(bd, CAVE_TONES_25, back=170, sides=5, seed=25, ragged=8.0)

    def on_rock(cx, cy):
        return (abs(cx - _river_cx_25(np.float64(cy))) > RIVER_HW_25 * 1.8
                and _blob(cx / TILE_OUT, cy / TILE_OUT, PIT_SPEC_25, wob=0.3, seed=2.5) > 1.25)
    scatter_trees(bd, ('fire_pillar_2', 'fire_pillar_3', 'fire_pillar_4'),
                  boxes=[(170, 240, 60, 160, 0.5), (20, 90, 120, 160, 0.5)],
                  spacing=24, seed=35, scale=2, avoid=(FIGHTER_ZONE,), where=on_rock)
    scatter_trees(bd, ('fire_pillar_1',),
                  boxes=[(150, 250, 30, 150, 0.4), (10, 90, 30, 160, 0.4)],
                  spacing=26, seed=45, scale=2, avoid=(FIGHTER_ZONE,), where=on_rock)
    bd.stamp(obstacle('fire_pillar_3', 2), 196, 44)


# --------------------------------------------------------------------------- #
# chapter 26 -- the pit hall                                                  #
# --------------------------------------------------------------------------- #

# Chapter 26 is a hall floored with dark iron grating, cut through with black
# shafts so the floor is only a lattice of walkways between them -- broad ones
# down the middle and round the edges, a great diamond-shaped shaft at the
# heart of it, with raw brown rock showing in the corners.
#
# The fight is on the grating of the central walkway. Shafts drop away black
# on either side of it, the diamond yawns behind the fighters, and the rock of
# the corners rises at the back of the frame into the dark.

GRATE_26 = (75, 75, 75, 39, 4, 13, 6, 14)
SHAFT_26 = 73
SHAFT_FACE_26 = 5
ROCK_26 = (72, 74, 61, 62)
SHAFT_Z_26 = 1


def recipe_26(bd):
    turn = Turn(bd)
    xs, ys = turn.u, turn.v                # the design, laid out turned
    bd.ground[:, :] = GROUND_Z
    # The diamond behind the fight, and a shaft either side of the walkway.
    diamond = (np.abs(xs - 116) / 46.0 + np.abs(ys - 146) / 26.0) < 1.0
    sides = (((xs < 70) | (xs > 166)) & (ys > 40) & (ys < 112)
             & ((np.abs(xs - 116) / 1.0 + (ys - 40) * 0.4) > 56))
    shaft = diamond | sides
    rock = ((np.abs(xs - 116) > 86) & (ys > 132)) & ~shaft
    raise_where(bd, rock, 12)
    bd.ground[shaft] = SHAFT_Z_26

    bd.lay_ground(_pick(GRATE_26, 26), '26', contrast=0.8)
    lay_where(bd, _pick(ROCK_26, 126), '26', rock)
    lay_where(bd, lambda tx, ty: SHAFT_26, '26', shaft)
    sink_colour(bd, shaft)
    rock_faces(bd, '26', SHAFT_FACE_26, min_rise=4)
    enclose(bd, VOID, back=172, sides=3, seed=26)


# --------------------------------------------------------------------------- #
# shared by 27..30 -- the palace of light                                     #
# --------------------------------------------------------------------------- #

def step_up(bd, mask, height, axis_x=None, stair_hw=0, ramp=10):
    """Raise ``mask`` by ``height`` as a terrace, leaving a stair up its front
    on ``axis_x``: two-voxel treads over ``ramp`` rows instead of a step.

    Returns the stair's mask so the caller can lay stair tiles on it.
    """
    xs, ys = _grid(bd)
    m = np.broadcast_to(mask, bd.ground.shape)
    bd.ground = bd.ground + np.where(m, height, 0).astype(np.int16)
    if axis_x is None:
        return np.zeros(bd.ground.shape, bool)
    # The front of the terrace on the axis: the first masked row per column.
    front = np.argmax(m, axis=1).astype(np.float64)[:, None]
    lane = (np.abs(xs - axis_x) < stair_hw) & (ys >= front - ramp) & (ys < front) & m.any(axis=1)[:, None]
    t = np.clip((ys - (front - ramp)) / float(ramp), 0, 1)
    rise = (2 * np.round(t * height / 2.0)).astype(np.int16)
    bd.ground = np.where(lane, bd.ground + rise, bd.ground).astype(np.int16)
    return lane


def pillar_rows(bd, keys, xs_, y0, y1, spacing, scale=2):
    """Pillars standing in rows down the sides, alternating ``keys``."""
    for i, y in enumerate(range(y0, y1, spacing)):
        for j, x in enumerate(xs_):
            bd.stamp(obstacle(keys[(i + j) % len(keys)], scale), x, y)


# --------------------------------------------------------------------------- #
# chapter 27 -- the temple of light                                           #
# --------------------------------------------------------------------------- #

# Chapter 27 is the great temple: terraces of pale stone stepping up out of a
# floor of brown rock, posts and statues standing along every terrace, pillars
# of cold blue-white light burning down both flanks, and at its head a square
# pool of blue water with two carved steles standing in it.
#
# The fight is on the brown rock at the temple's foot. The pale terraces climb
# behind, a stair on the axis; the pillars of light line both sides, statues
# flank the stair, and the pool opens on the top terrace with its steles.

FLOOR_27 = (74, 74, 69, 52, 51, 48)
STONE_27 = (111, 111, 111, 117, 116)
STONE_FACE_27 = 132
POOL_27 = 75
STAIR_27 = 119
WATER_Z_27 = 2                          # how far the pool sits below its terrace


def recipe_27(bd):
    bd.undulate(amplitude=1.0, wavelength=70.0, seed=27)
    xs, ys = np.broadcast_arrays(*_grid(bd))
    tier1 = (ys >= 104) & (np.abs(xs - 116) < 108)
    tier2 = (ys >= 134) & (np.abs(xs - 116) < 84)
    bd.terrace(0, bd.size[0], 96, bd.size[1], GROUND_Z)
    s1 = step_up(bd, tier1, 4, axis_x=116, stair_hw=20)
    s2 = step_up(bd, tier2, 4, axis_x=116, stair_hw=16)
    pool = (np.abs(xs - 116) < 30) & (ys >= 146) & (ys < 172)
    bd.ground[pool] -= WATER_Z_27

    bd.lay_ground(_pick(FLOOR_27, 27), '27')
    lay_where(bd, _pick(STONE_27, 127), '27', tier1 | s1)
    lay_where(bd, lambda tx, ty: STAIR_27, '27', s1 | s2, contrast=1.0)
    lay_where(bd, lambda tx, ty: POOL_27, '27', pool)
    rock_faces(bd, '27', STONE_FACE_27, min_rise=3)
    enclose(bd, VOID, back=176, sides=3, seed=27)

    bd.stamp(obstacle('stone_stele_1', 2), 98, 152)
    bd.stamp(obstacle('stone_stele_1', 2), 122, 152)
    pillar_rows(bd, ('light_pillar_1', 'light_pillar_2'), (20, 202), 40, 170, 20)
    for x, y in ((84, 114), (138, 114)):
        bd.stamp(obstacle('stone_statue_1', 2), x, y)
    pillar_rows(bd, ('stone_column_3', 'stone_pillar_1'), (60, 160), 108, 132, 22)


# --------------------------------------------------------------------------- #
# chapter 28 -- the water palace                                              #
# --------------------------------------------------------------------------- #

# Chapter 28 is a palace built on water: walkways of pale stone between deep
# blue channels, a hall floored with dark iron grating, a row of square pools
# each with a carved stele and a pillar of light at either side, and the whole
# of it ringed by the moat and the brown rock beyond.
#
# The fight is on the grating. A pale walkway crosses the frame behind it, a
# channel of blue water behind that, and then the row of pools on the far
# terrace with their pillars of light, closing off into the dark.

GRATE_28 = (124, 124, 124, 125)
STONE_28 = (121,)
WATER_28 = 122
POOL_28 = 207
STONE_FACE_28 = 13
WATER_Z_28 = GROUND_Z - 4


def recipe_28(bd):
    xs, ys = np.broadcast_arrays(*_grid(bd))
    bd.ground[:, :] = GROUND_Z
    walk = (ys >= 96) & (ys < 112)
    channel = (ys >= 112) & (ys < 132)
    far = ys >= 132
    side = (xs < 36) & (ys >= 40) & (ys < 96)      # the moat opening to screen right
    bd.ground[channel | side] = WATER_Z_28
    raise_where(bd, far, 3)
    pools = np.zeros(bd.ground.shape, bool)
    for cx in (40, 92, 144, 196):
        pools |= (np.abs(xs - cx) < 18) & (ys >= 142) & (ys < 166)
    bd.ground[pools] -= 3

    bd.lay_ground(_pick(GRATE_28, 28), '28', contrast=0.8)
    lay_where(bd, _pick(STONE_28, 128), '28', walk | far)
    lay_where(bd, lambda tx, ty: WATER_28, '28', channel | side)
    lay_where(bd, lambda tx, ty: POOL_28, '28', pools)
    sink_colour(bd, channel | side)
    rock_faces(bd, '28', STONE_FACE_28, min_rise=3)
    enclose(bd, VOID, back=176, sides=3, seed=28)

    for cx in (40, 92, 144, 196):
        bd.stamp(obstacle('stone_stele_1', 2), cx - 10, 150)
        bd.stamp(obstacle('light_pillar_1', 2), cx + 18, 140)
    bd.stamp(obstacle('light_pillar_2', 2), 6, 136)
    for x in (60, 160):
        bd.stamp(obstacle('stone_pillar_1', 2), x, 98)


# --------------------------------------------------------------------------- #
# chapter 29 -- the dragons' sanctum                                          #
# --------------------------------------------------------------------------- #

# Chapter 29 is the approach to the three dragons: a long causeway of iron
# grating running up the middle of a dark hall, edged in pale stone, with
# stepped channels of blue water falling away either side and pillars of light
# standing along the stone, up to a raised dais at its head.
#
# The backdrop looks up the causeway: grating under the fighters, its pale
# kerbs converging on the dais, the water stepping down in channels to both
# sides and the pillars of light marching away along them.

GRATE_29 = (231, 231, 196, 198)
STONE_29 = (145,)
WATER_29 = 163
STONE_FACE_29 = 24
CAUSEWAY_HW_29 = 26


def recipe_29(bd):
    turn = Turn(bd)
    xs, ys = turn.u, turn.v                # the design, laid out turned
    bd.ground[:, :] = GROUND_Z
    off = np.abs(xs - 116)
    kerb = (off >= CAUSEWAY_HW_29) & (off < CAUSEWAY_HW_29 + 12)
    water1 = (off >= CAUSEWAY_HW_29 + 12) & (off < CAUSEWAY_HW_29 + 40)
    ledge = (off >= CAUSEWAY_HW_29 + 40) & (off < CAUSEWAY_HW_29 + 52)
    water2 = off >= CAUSEWAY_HW_29 + 52
    dais = (ys >= 150) & (off < CAUSEWAY_HW_29 + 52)
    bd.ground[water1 & ~dais] = GROUND_Z - 3
    bd.ground[water2 & ~dais] = GROUND_Z - 6
    bd.ground[ledge & ~dais] = GROUND_Z - 1
    # The dais, and a stair up its front the width of the causeway
    # (``step_up`` finds the front along y, which is not the way up here).
    raise_where(bd, dais, 6)
    stair = (off < CAUSEWAY_HW_29) & (ys >= 140) & (ys < 150)
    t = np.clip((ys - 140) / 10.0, 0, 1)
    bd.ground[stair] = (GROUND_Z + 2 * np.round(t * 3)).astype(np.int16)[stair]

    bd.lay_ground(_pick(GRATE_29, 29), '29', contrast=0.8)
    lay_where(bd, _pick(STONE_29, 129), '29', ((kerb | ledge) & ~dais) | (dais & ~stair))
    wet = (water1 | water2) & ~dais
    lay_where(bd, lambda tx, ty: WATER_29, '29', wet)
    sink_colour(bd, wet)
    rock_faces(bd, '29', STONE_FACE_29, min_rise=3)
    enclose(bd, VOID, back=176, sides=3, seed=29)

    keys = ('light_pillar_1', 'light_pillar_2')
    for i, y in enumerate(range(-40, 150, 26)):
        for k, x in enumerate((116 - CAUSEWAY_HW_29 - 11, 116 + CAUSEWAY_HW_29 + 1)):
            m = obstacle(keys[(i + k) % 2], 2)
            if turn.inside(x + m.shape[0] / 2.0, y + m.shape[1] / 2.0):
                turn.stamp(m, x, y)
    turn.stamp(obstacle('stone_stele_1', 2), 106, 160)


# --------------------------------------------------------------------------- #
# chapter 30 -- the last hall                                                 #
# --------------------------------------------------------------------------- #

# Chapter 30 is the end of the game: a floor of pale stone in a cavern of black
# rock, a broad stair climbing to a raised hall at its centre, and channels of
# blue water running round it in squared-off bends, with pillars of light
# standing on the stone between them and the dark rock jagged all round.
#
# The fight is on the pale floor at the foot of the stair. The channels wrap
# round behind it, the stair climbs on the axis, the pillars of light stand at
# the bends, and the black rock rises jagged on every side.

FLOOR_30 = (145, 145, 3)
WATER_30 = 163
STAIR_30 = 192
STONE_FACE_30 = 223                    # the hall's walls, in the dark grating
WORN_30 = (88, 91)                     # the blue-grey wear in the stone
ROCK_TONES_30 = ((20, 26, 24), (12, 15, 14), (27, 35, 33), (6, 8, 7))


def recipe_30(bd):
    xs, ys = np.broadcast_arrays(*_grid(bd))
    bd.ground[:, :] = GROUND_Z
    off = np.abs(xs - 116)
    # The squared-off U of the channels, wrapping round behind the fighters.
    inner = (off < 70) & (ys >= 108)
    outer = (off < 90) & (ys >= 92)
    channel = outer & ~inner & ~((off < 18) & (ys < 128))
    hall = (off < 50) & (ys >= 128)
    bd.ground[channel] = GROUND_Z - 4
    stair = step_up(bd, hall, 8, axis_x=116, stair_hw=18, ramp=14)

    bd.lay_ground(_pick(FLOOR_30, 30), '30')
    lay_where(bd, _pick(WORN_30, 130), '30',
              (_patchy(xs / TILE_OUT, ys / TILE_OUT, 30) > 0.6) & ~hall)
    lay_where(bd, lambda tx, ty: WATER_30, '30', channel)
    lay_where(bd, lambda tx, ty: STAIR_30, '30', stair, contrast=1.0)
    sink_colour(bd, channel)
    rock_faces(bd, '30', STONE_FACE_30, min_rise=3)
    enclose(bd, ROCK_TONES_30, back=168, sides=24, side_from=40, seed=30, ragged=10.0)

    for x, y in ((18, 88), (206, 88), (40, 120), (184, 120), (62, 150), (162, 150)):
        bd.stamp(obstacle(('light_pillar_1', 'light_pillar_2')[(x // 10) % 2], 2), x, y)


RECIPES = {'02': recipe_02, '03': recipe_03, '04': recipe_04, '05': recipe_05,
           '06': recipe_06, '07': recipe_07, '08': recipe_08, '09': recipe_09,
           '10': recipe_10, '11': recipe_11, '12': recipe_12, '13': recipe_13,
           '14': recipe_14, '15': recipe_15, '16': recipe_16, '17': recipe_17, '18': recipe_18, '19': recipe_19, '20': recipe_20, '21': recipe_21, '22': recipe_22, '23': recipe_23, '24': recipe_24, '25': recipe_25, '26': recipe_26,
           '27': recipe_27, '28': recipe_28, '29': recipe_29, '30': recipe_30}


# --------------------------------------------------------------------------- #
# CLI                                                                         #
# --------------------------------------------------------------------------- #

# Unity's OBJ importer negates X (right-handed OBJ into left-handed Unity), so a
# backdrop shows in the battle scene as the left-right mirror of what
# bg_preview.py used to draw. These chapters were laid out against those old
# previews and are written pre-mirrored (x -> 255 - x), so the game shows the
# composition exactly as designed. bg_preview.py now applies the importer's
# flip itself, so it draws any backdrop the way the game does.
MIRROR_FOR_GAME = {'16', '17', '18', '22', '23', '26', '29'}


def build(chapter, out_dir=None):
    nn = voxlib.nn(chapter)
    if nn not in RECIPES:
        raise SystemExit('no backdrop recipe for chapter %s (have: %s)'
                         % (nn, ', '.join(sorted(RECIPES))))
    bd = Backdrop()
    RECIPES[nn](bd)
    if nn in MIRROR_FOR_GAME:
        bd.a = bd.a[::-1, :, :].copy()
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
