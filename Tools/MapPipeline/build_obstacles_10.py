"""Build the three fire pillars chapter 10 adds -- two animation frames each.

Run once to produce, in Resources/Remastered/Obstacles/vox/:

    fire_pillar_1       1 x 1   the low fire bowl standing on the cave floor (tile 31)
    fire_pillar_2       1 x 1   the tall bright pillar: white-hot column (tiles 56 over 69)
    fire_pillar_3       1 x 1   the tall dim pillar: orange column (tiles 52 over 68)

and beside each one a ``<key>_f2.vox`` -- the second frame of its flame.

**The flame is animated with two frames.** ObstaclesLayer loads ``<key>`` as
the first frame and then every ``<key>_f2``, ``_f3``, ... it finds, and shows
them in turn at ObstacleAnimation.FramesPerSecond. Both frames share the dish
and the column exactly; only the flame tongues differ -- each tongue is tall in
one frame and short in the other, and leans the other way -- so the fire
flickers while the pillar itself stands still. Keeping the dish the widest
part of both frames also keeps their bounding boxes identical, which the
exporter's centring and ObstaclesLayer's anchoring both rely on.

**A tall pillar is painted over two rows and stands on one.** The art draws it
in elevation: the flame on the upper tile, the column and its dish on the
lower one. The model is 1 x 1 standing on the lower tile and the obstacle list
clears both rows. The bowl is a single tile.

Colours are the art's own fire ramp -- white heart, yellow, the oranges, red,
and the near-black red of the tongue tips -- resolved to the palette. The dim
pillar's ramp simply starts at orange instead of white.

Heights: the tall pillars are 47 voxels for a two-row painting, a little over
the 40 the statues were cut to, because a column of fire is meant to stand
over the cave; the bowl is 20, most of its one tile.

Model space is the obstacle convention (formats.md): x runs left to right along
the map, y = 0 is the map row nearest the camera (the front of the footprint)
and y = 23 the back, z is up.
"""

import math
import os

import numpy as np

import voxlib

TILE = voxlib.TILE


def P(rgb):
    return voxlib.palette_index(rgb)


# the art's fire ramp, brightest first
WHITE = P((252, 252, 252))
YELLOW = P((252, 252, 0))
LIGHT_YELLOW = P((252, 220, 0))
GOLD = P((252, 192, 0))
ORANGE = P((252, 164, 0))
ORANGE2 = P((252, 136, 0))
DEEP_ORANGE = P((252, 108, 0))
RED_ORANGE = P((252, 80, 0))
RED = P((252, 0, 0))
DARK_RED = P((188, 0, 0))
DARKER_RED = P((116, 0, 0))
EMBER = P((68, 0, 0))

# the ramp up a flame, as (fraction of the flame's height, colour)
BRIGHT_RAMP = ((0.20, WHITE), (0.32, YELLOW), (0.44, ORANGE), (0.56, DEEP_ORANGE),
               (0.68, RED_ORANGE), (0.80, RED), (0.90, DARK_RED), (1.01, DARKER_RED))
DIM_RAMP = ((0.22, ORANGE), (0.40, DEEP_ORANGE), (0.56, RED_ORANGE), (0.72, RED),
            (0.86, DARK_RED), (1.01, DARKER_RED))

CX = CY = 11.5          # the pillar stands in the middle of its tile

DISH_TOP = 5            # z where the column, or the bowl's flame, starts
DISH_R = 8.5            # the dish is the widest part of every frame

COLUMN_R = 6.5
COLUMN_H = 26           # the tall pillars' column, dish top to flame base

# The flame: a central cone plus six tongues on a ring around it, as
# (angle in degrees, height in frame 1, height in frame 2). Every tongue is
# tall in one frame and short in the other.
TONGUES = ((0, 16, 10), (60, 10, 15), (120, 14, 9), (180, 9, 13), (240, 12, 8), (300, 8, 12))
TONGUE_RING_R = 4.0
TONGUE_R = 2.6
BODY_R = 7.0
BODY_H = (11, 9)        # the central cone's height, per frame
TONGUE_LEAN = (1.0, -1.0)   # how far a tongue's tip drifts sideways, per frame
FLAME_H = 16            # the tallest tongue: the ramp is scaled to this


def coords(h):
    xs = np.arange(TILE)[:, None]
    ys = np.arange(TILE)[None, :]
    return xs, ys


def disc_mask(cx, cy, r):
    xs, ys = coords(0)
    return (xs - cx) ** 2 + (ys - cy) ** 2 <= r * r


def disc(g, z0, z1, r, colour, cx=CX, cy=CY):
    m = disc_mask(cx, cy, r)
    for z in range(max(z0, 0), min(z1, g.shape[2])):
        g[:, :, z][m] = colour


def ramp_colour(ramp, t):
    for limit, colour in ramp:
        if t < limit:
            return colour
    return ramp[-1][1]


# --------------------------------------------------------------------------
# parts
# --------------------------------------------------------------------------

def dish(g):
    """The stone-and-fire bowl every pillar stands in: an orange rim round a
    white-hot heart, on a narrower shadowed foot."""
    disc(g, 0, 2, 6.5, RED_ORANGE)
    disc(g, 2, 4, 8.0, ORANGE2)
    disc(g, 4, DISH_TOP, DISH_R, ORANGE)
    disc(g, 4, DISH_TOP, 7.0, LIGHT_YELLOW)
    disc(g, 4, DISH_TOP, 5.5, WHITE)


def column(g, z0, z1, core, edge, streak=None):
    """A round column from z0 to z1: ``edge`` on the outside, ``core`` within,
    and optionally ``streak`` flecks running up the outside."""
    disc(g, z0, z1, COLUMN_R, edge)
    disc(g, z0, z1, COLUMN_R - 1.0, core)
    if streak is None:
        return
    for k, angle in enumerate(range(0, 360, 45)):
        a = math.radians(angle)
        x = int(round(CX + (COLUMN_R - 0.6) * math.cos(a)))
        y = int(round(CY + (COLUMN_R - 0.6) * math.sin(a)))
        for z in range(z0 + 1 + (k * 3) % 5, z1 - 1, 6):
            g[x, y, z:min(z + 3, z1 - 1)] = streak


def flame(g, z0, frame, ramp):
    """The fire above z0: a central cone and the six tongues, coloured by
    height alone so the tongues and the body share one gradient."""
    tall = max(max(t[1], t[2]) for t in TONGUES)

    def paint(cx, cy, height, r0, lean):
        for k in range(height):
            z = z0 + k
            if z >= g.shape[2]:
                break
            t = k / float(height)
            r = r0 * (1.0 - t) ** 0.75
            if r < 0.5:
                r = 0.5
            colour = ramp_colour(ramp, (z - z0) / float(tall))
            m = disc_mask(cx + lean[0] * t, cy + lean[1] * t, r)
            g[:, :, z][m] = colour

    paint(CX, CY, BODY_H[frame], BODY_R, (0.0, 0.0))
    for angle, h1, h2 in TONGUES:
        a = math.radians(angle)
        cx = CX + TONGUE_RING_R * math.cos(a)
        cy = CY + TONGUE_RING_R * math.sin(a)
        lean = TONGUE_LEAN[frame]
        paint(cx, cy, (h1, h2)[frame], TONGUE_R, (lean * math.cos(a), lean * math.sin(a)))


# --------------------------------------------------------------------------
# the three pillars
# --------------------------------------------------------------------------

BOWL_H = 24
PILLAR_H = 48


def fire_pillar_1(frame):
    """The low bowl: the dish with the flame straight on it."""
    g = np.zeros((TILE, TILE, BOWL_H), dtype=np.uint8)
    dish(g)
    flame(g, DISH_TOP, frame, BRIGHT_RAMP)
    return g


def fire_pillar_2(frame):
    """The bright pillar: a white-hot column flecked with yellow.

    The art edges the column in yellow, but in 3D the edge is the silhouette
    and the only thing on screen is the surface -- so the surface is white and
    the yellow goes on as flecks, with a yellow band where the column meets
    the dish."""
    g = np.zeros((TILE, TILE, PILLAR_H), dtype=np.uint8)
    dish(g)
    column(g, DISH_TOP, DISH_TOP + COLUMN_H, WHITE, WHITE, streak=LIGHT_YELLOW)
    disc(g, DISH_TOP, DISH_TOP + 2, COLUMN_R, YELLOW)             # the foot, in the dish's glow
    flame(g, DISH_TOP + COLUMN_H, frame, BRIGHT_RAMP)
    return g


def fire_pillar_3(frame):
    """The dim pillar: an orange column flecked with gold."""
    g = np.zeros((TILE, TILE, PILLAR_H), dtype=np.uint8)
    dish(g)
    column(g, DISH_TOP, DISH_TOP + COLUMN_H, ORANGE, DEEP_ORANGE, streak=GOLD)
    flame(g, DISH_TOP + COLUMN_H, frame, DIM_RAMP)
    return g


BUILDERS = {
    'fire_pillar_1': fire_pillar_1,
    'fire_pillar_2': fire_pillar_2,
    'fire_pillar_3': fire_pillar_3,
}

FRAMES = 2


def frame_name(name, frame):
    """``<key>`` for the first frame, ``<key>_f2``, ``_f3``, ... after that --
    the names ObstaclesLayer looks for."""
    return name if frame == 0 else '%s_f%d' % (name, frame + 1)


def main():
    root = voxlib.workspace_root()
    out_dir = voxlib.obstacles_vox_dir(root)
    if not os.path.isdir(out_dir):
        os.makedirs(out_dir)
    for name, build in sorted(BUILDERS.items()):
        for frame in range(FRAMES):
            g = build(frame)
            xs, ys, zs = np.nonzero(g)
            voxels = list(zip(xs.tolist(), ys.tolist(), zs.tolist(), g[xs, ys, zs].tolist()))
            path = os.path.join(out_dir, frame_name(name, frame) + '.vox')
            voxlib.write_vox(path, g.shape, voxels)
            print('%-18s SIZE %3dx%3dx%-4d  %d cols x %d rows  x %d..%d y %d..%d z ..%d  %6d voxels  -> %s'
                  % (frame_name(name, frame), g.shape[0], g.shape[1], g.shape[2],
                     g.shape[0] // TILE, g.shape[1] // TILE,
                     xs.min(), xs.max(), ys.min(), ys.max(), zs.max(), len(voxels), path))


if __name__ == '__main__':
    main()
