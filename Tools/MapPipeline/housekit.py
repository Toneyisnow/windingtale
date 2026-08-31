"""Shared machinery for the voxel buildings the chapters stand on their maps.

Lifted unchanged out of ``build_obstacles_02.py``, which built the first five of
them and is now one caller among several. Nothing here knows about a particular
chapter: a building is a list of :func:`wing` calls plus the openings punched
into them, and every chapter's builder is written in those terms.

The one thing that was generalised on the way out is roof colour. Chapter 02's
churches are blue; chapter 05's cathedral and mansion are red, and are otherwise
the same architecture. So ``wing()`` takes a :class:`RoofColours`, defaulting to
the blue one it has always used -- chapter 02's models are unchanged, to the byte.

Model space (see map-2d-to-3d/references/formats.md):
    x  0..W-1   map X, left -> right      W = cols * 24
    y  0..D-1   larger y = SMALLER map row, so y = 0 is the FRONT facade
    z  0..H-1   up

Heights are written at the scale the 2D art implies and squashed on the way out
-- see BODY_SCALE / ROOF_SCALE. Only z is touched, so the footprints, and with
them the cleaned map, are unaffected by retuning them.
"""

import collections
import math
import os

import voxlib

# sampled from Chapter-02.png
ROOF_DARK = voxlib.palette_index((16, 36, 100))     # shaded roof slope
ROOF_MID = voxlib.palette_index((40, 68, 136))      # roof body
ROOF_LIGHT = voxlib.palette_index((92, 124, 188))   # lit roof slope
ROOF_EDGE = voxlib.palette_index((4, 24, 80))       # eaves / ridge line
WALL = voxlib.palette_index((152, 124, 88))         # beige stone
WALL_DARK = voxlib.palette_index((136, 108, 72))    # shaded wall
PLINTH = voxlib.palette_index((96, 96, 104))        # stone base course
OPENING = voxlib.palette_index((28, 24, 20))        # doorway / dark window
GLASS = voxlib.palette_index((196, 84, 28))         # small side windows
CROSS = voxlib.palette_index((232, 232, 240))

# A roof's four tones: the two slopes, the body between them, and the dark line
# the eaves and hips are drawn with.
RoofColours = collections.namedtuple('RoofColours', 'dark mid light edge')

BLUE_ROOF = RoofColours(ROOF_DARK, ROOF_MID, ROOF_LIGHT, ROOF_EDGE)

# sampled from Chapter-05.png -- the cathedral and the mansion
RED_ROOF = RoofColours(
    voxlib.palette_index((88, 12, 4)),      # shaded slope
    voxlib.palette_index((116, 32, 12)),    # roof body
    voxlib.palette_index((168, 72, 32)),    # lit slope
    voxlib.palette_index((72, 4, 0)),       # eaves / hip line
)

# The rose window is not one colour, so it does not get a constant. In the art
# it is an arched opening -- round head, straight sides, flat sill -- filled
# with a stained-glass sun rising over waves, and all three churches that have
# one are painted with the identical sprite. It is read straight out of
# Chapter-02.png rather than approximated, since a disc in a single orange is
# exactly what it does not look like.
ROSE_ART = (376, 218, 18, 20)     # x, y, w, h of that sprite in Chapter-02.png

# The beige stone the arch is cut into. Pixels in these tones are the wall
# around the window rather than the window, and are what makes the sprite an
# arch rather than a rectangle: where they fall the roof is left alone.
STONE_TONES = frozenset([
    (0x98, 0x7c, 0x58), (0x88, 0x6c, 0x48), (0x78, 0x60, 0x3c),
    (0x68, 0x50, 0x30), (0x5c, 0x44, 0x28), (0xa8, 0x8c, 0x64),
    (0xb8, 0x9c, 0x78), (0xc8, 0xac, 0x88),
])

# The arch's stone frame. Its browns are the one part of the sprite the
# MagicaVoxel palette has nothing near -- the 6-level colour cube turns them
# olive, which puts a pair of yellow ears on the shoulders of the arch -- so
# they are pinned to the same near-black the doorways use.
ROSE_FRAME_TONES = frozenset([(0x3c, 0x24, 0x08), (0x30, 0x1c, 0x04)])

_rose_art = None


def rose_window_art():
    """The rose window sprite, as rows of palette index, or None for the wall.

    Row 0 is the top of the arch, column 0 its left edge. Cached: it is stamped
    onto three models and the art only has to be read once.
    """
    global _rose_art
    if _rose_art is None:
        from PIL import Image
        x0, y0, w, h = ROSE_ART
        art = Image.open(
            voxlib.map_png_path(voxlib.workspace_root(), '02')).convert('RGB')
        _rose_art = [[_rose_pixel(art.getpixel((x, y)))
                      for x in range(x0, x0 + w)]
                     for y in range(y0, y0 + h)]
    return _rose_art


def _rose_pixel(rgb):
    if rgb in STONE_TONES:
        return None
    if rgb in ROSE_FRAME_TONES:
        return OPENING
    return voxlib.palette_index(rgb)

SHELL = 4          # wall / roof thickness in voxels

# The churches were first built at the height the 2D art implies, which reads as
# a tower once they stand up on the board next to 40-voxel tiles. The walls are
# cut to half, the roof to two thirds of its art pitch. The numbers in the
# builders below are still the original, art-derived ones -- scaling happens in
# one place, so the models stay readable as descriptions of the buildings and
# the ratio can be retuned here.
BODY_SCALE = 1.0 / 2      # plinth + walls + everything standing on them
ROOF_SCALE = 2.0 / 3      # eaves -> apex


def squash(z, wall_top):
    """Original z -> built z, for a feature on a wing with this wall top.

    Piecewise about the eaves: below them the body scale, above them the roof
    scale. The roof is carried down with the wall it stands on and then has its
    own rise flattened by ROOF_SCALE.
    """
    body = int(round(wall_top * BODY_SCALE))
    if z <= wall_top:
        return int(round(z * BODY_SCALE))
    return body + int(round((z - wall_top) * ROOF_SCALE))


class Model(object):
    """Sparse voxel volume; later writes win, so detail can be layered on."""

    def __init__(self, cols, rows, height):
        self.w = cols * voxlib.TILE
        self.d = rows * voxlib.TILE
        self.h = height
        self.v = {}

    def set(self, x, y, z, c):
        if 0 <= x < self.w and 0 <= y < self.d and 0 <= z < self.h:
            self.v[(x, y, z)] = c

    def clear(self, x, y, z):
        self.v.pop((x, y, z), None)

    def box(self, x0, x1, y0, y1, z0, z1, c, hollow=True):
        for x in range(x0, x1 + 1):
            for y in range(y0, y1 + 1):
                for z in range(z0, z1 + 1):
                    if hollow and not (
                            x < x0 + SHELL or x > x1 - SHELL or
                            y < y0 + SHELL or y > y1 - SHELL or
                            z < z0 + SHELL or z > z1 - SHELL):
                        continue
                    self.set(x, y, z, c)

    def require_wall(self, x, y, z, what):
        """Fail loudly when a door or window is placed off the building.

        Openings are painted onto a facade, so if there is no geometry at the
        spot they are asked for they end up hanging in mid-air next to the
        house -- which is exactly what happened to blue_house_1's right-hand
        window, and it is invisible in every check except looking at it.
        """
        if (x, y, z) not in self.v:
            raise AssertionError(
                '%s at x=%d y=%d z=%d has no wall behind it: it would float '
                'beside the building. Move it onto a wing, or build the wing '
                'before the opening.' % (what, x, y, z))

    def carve(self, x0, x1, y0, y1, z0, z1):
        for x in range(x0, x1 + 1):
            for y in range(y0, y1 + 1):
                for z in range(z0, z1 + 1):
                    self.clear(x, y, z)

    def solidify(self):
        """Pack the hollow interior of every column between its lowest and
        highest voxel.

        The walls and roofs are built as thin shells, which is cheap to author
        but terrible to export: the exporter emits a face wherever a voxel
        touches empty space, so a hollow model pays for its *inside* surface
        too -- blue_house_1 came out a 63 MB OBJ against 9 MB for the
        comparable hand-built dwelling_house_1. The packed voxels are sealed
        inside the shell and never rendered.

        Only the gap between an existing floor and an existing roof is filled,
        never below the lowest voxel: a column under a roof overhang has
        nothing but roof in it, and growing that down to the ground would wrap
        the whole building in a blue skin.
        """
        columns = {}
        for (x, y, z) in self.v:
            columns.setdefault((x, y), []).append(z)
        for (x, y), zs in columns.items():
            lo, hi = min(zs), max(zs)
            if hi - lo < 2:
                continue
            fill = self.v[(x, y, lo)]
            for z in range(lo + 1, hi):
                if (x, y, z) in self.v:
                    fill = self.v[(x, y, z)]
                else:
                    self.v[(x, y, z)] = fill

    def voxels(self):
        return [(x, y, z, c) for (x, y, z), c in sorted(self.v.items())]


EAVE = 3               # roof overhang past the wall


class Roof(object):
    """The pyramid a wing was built under, so things can be set into its surface.

    Anything that has to sit on the roof -- a cross on the apex, a window let
    into a slope -- needs the same height formula the roof was built from. It
    lives here rather than being written out a second time at each call site.
    """

    def __init__(self, x0, x1, y0, y1, top_of_wall, apex):
        self.x0, self.x1, self.y0, self.y1 = x0, x1, y0, y1
        self.cx = (x0 + x1) / 2.0
        self.cy = (y0 + y1) / 2.0
        self.hx = (x1 - x0) / 2.0 + EAVE
        self.hy = (y1 - y0) / 2.0 + EAVE
        self.top_of_wall = top_of_wall
        self.apex = apex

    @property
    def rise(self):
        return self.apex - self.top_of_wall

    def t(self, x, y):
        """Chebyshev distance to the centre, 0 at the apex and 1 at the eaves."""
        return max(abs(x - self.cx) / self.hx, abs(y - self.cy) / self.hy)

    def top(self, x, y):
        return self.top_of_wall + int(round(self.rise * (1.0 - self.t(x, y))))

    def on_front_slope(self, x, y):
        return y < self.cy and abs(y - self.cy) / self.hy >= abs(x - self.cx) / self.hx


def wing(m, x0, x1, y0, y1, wall_top, ridge_top, plinth=6, roof_colours=None):
    """One bay: stone walls under a square-pyramid roof.

    All four slopes climb from the eaves to a single apex over the centre of the
    bay, so there is no ridge and no gable triangle -- the roof reads as a point
    from every side. ``ridge_top`` is that apex.

    ``wall_top`` and ``ridge_top`` are the original art heights; the wing is
    built at the scaled ones. Only z is scaled -- the footprint, the shell
    thickness and the eave overhang are unchanged.

    ``roof_colours`` is the :class:`RoofColours` the pyramid is tiled with, and
    defaults to the blue one chapter 02's churches are roofed in.

    Returns the :class:`Roof`, for whatever stands on or sits in it.
    """
    rc = roof_colours or BLUE_ROOF
    base = max(SHELL, squash(plinth, wall_top))
    top_of_wall = squash(wall_top, wall_top)
    apex = squash(ridge_top, wall_top)
    roof = Roof(x0, x1, y0, y1, top_of_wall, apex)

    # plinth + walls
    m.box(x0, x1, y0, y1, 0, base - 1, PLINTH)
    m.box(x0, x1, y0, y1, base, top_of_wall, WALL)
    # shade the left third of every wall face, like the art does
    for x in range(x0, x0 + (x1 - x0) // 3):
        for y in range(y0, y1 + 1):
            for z in range(base, top_of_wall + 1):
                if (x, y, z) in m.v:
                    m.set(x, y, z, WALL_DARK)

    cx, cy = roof.cx, roof.cy
    hx, hy = roof.hx, roof.hy
    eave = EAVE
    rise = roof.rise

    for x in range(x0 - eave, x1 + eave + 1):
        for y in range(y0 - eave, y1 + eave + 1):
            # Distance to the centre in units of the half-footprint, taken as a
            # max over the two axes: the level sets are rectangles shrinking to a
            # point, which is the pyramid. Using |x - cx| alone is what made the
            # old roof a ridge, since then y did not constrain the height.
            tx = abs(x - cx) / hx
            ty = abs(y - cy) / hy
            t = max(tx, ty)
            if t > 1.0:
                continue
            top = top_of_wall + int(round(rise * (1.0 - t)))
            if tx >= ty:
                colour = rc.light if x > cx else rc.dark
            else:
                colour = rc.mid if y < cy else rc.dark
            edge = (y < y0 or y > y1 or x < x0 or x > x1)
            for z in range(top - SHELL + 1, top + 1):
                m.set(x, y, z, rc.edge if edge and z == top - SHELL + 1 else colour)

    return roof


def arch(m, cx, y, w, h, wall_top, z0=0, colour=OPENING, depth=8):
    """A round-topped opening punched into the facade at y (facing front).

    ``h`` and ``z0`` are original heights on a wing whose wall top is
    ``wall_top``; the opening is cut at the squashed ones. Its round head is an
    ellipse rather than a circle, because x is not squashed and z is -- a
    circular head would poke out of the top of the squashed opening.
    """
    zb = squash(z0, wall_top)
    zt = squash(z0 + h, wall_top)
    m.require_wall(cx, y, (zb + zt) // 2, 'arch')
    rx = max(1, w // 2)
    rz = max(1, min(rx, zt - zb))
    for dx in range(-rx, rx + 1):
        for z in range(zb, zt + 1):
            over = z - (zt - rz)
            if over > 0 and (dx / float(rx)) ** 2 + (over / float(rz)) ** 2 > 1.0:
                continue
            for dy in range(depth):
                m.set(cx + dx, y + dy, z, colour)


def window(m, cx, y, w, h, z0, wall_top, colour=GLASS, frame=WALL_DARK):
    zb = squash(z0, wall_top)
    zt = squash(z0 + h, wall_top)
    m.require_wall(cx, y, (zb + zt) // 2, 'window')
    for dx in range(-w // 2 - 1, w // 2 + 2):
        for z in range(zb - 1, zt + 2):
            inside = abs(dx) <= w // 2 and zb <= z <= zt
            for dy in range(4):
                m.set(cx + dx, y + dy, z, colour if inside else frame)


def roof_window(m, roof, w, up=0.5):
    """The rose window, let into the front slope flush with the roof.

    The pyramid has no gable to hang one on, so the window lies in the slope
    itself. The roof voxels under the sprite are recoloured rather than a hole
    cut, which keeps the surface unbroken and costs the exporter nothing; the
    pixels the sprite marks as wall are skipped, so what is left is the arched
    outline the art draws -- round head, straight sides, flat sill -- and not a
    disc.

    ``w`` is the window's width across the slope; its height follows from the
    sprite's aspect. The extent in y is shortened by the pitch, so the window
    reads as itself to someone looking at the sloping face rather than as a
    shape seen from directly above -- on the steep porch of blue_house_1 that
    is the difference between a window and a thin band. ``up`` places the
    centre along the slope: 0 at the eave, 1 at the apex.
    """
    art = rose_window_art()
    ah, aw = len(art), len(art[0])
    # One step in y climbs rise/hy in z, so a length lying on the slope projects
    # to 1/sqrt(1 + pitch^2) of itself when measured in plan.
    pitch = roof.rise / roof.hy
    sx = w / float(aw)                                   # art pixel -> voxels
    sy = max(0.4, sx / math.sqrt(1.0 + pitch * pitch))   # ... measured in plan
    h = ah * sy
    yc = roof.cy - (1.0 - up) * (roof.cy - (roof.y0 - EAVE))
    x0 = roof.cx - w / 2.0
    y0 = yc - h / 2.0

    painted = 0
    for x in range(int(math.floor(x0)), int(math.ceil(x0 + w))):
        ax = int((x - x0) / sx)
        if not 0 <= ax < aw:
            continue
        for y in range(int(math.floor(y0)), int(math.ceil(y0 + h))):
            # y grows up the slope but the sprite's rows grow downward, so the
            # rows are stamped in reverse or the window comes out upside down.
            ay = ah - 1 - int((y - y0) / sy)
            if not 0 <= ay < ah:
                continue
            c = art[ay][ax]
            if c is None or not roof.on_front_slope(x, y):
                continue
            top = roof.top(x, y)
            for z in range(top - SHELL + 1, top + 1):
                if (x, y, z) in m.v:
                    m.set(x, y, z, c)
                    painted += 1
    if not painted:
        raise AssertionError(
            'rose window at x=%.1f y=%.1f found no roof to sit in -- it would '
            'hang beside the building' % (roof.cx, yc))
    return painted


def cross(m, roof, size=14):
    """Stood on a roof's apex -- pass the :class:`Roof` ``wing()`` returned.

    A ridge let a cross stand anywhere along it, and these used to be placed on
    the front gable. A pyramid has exactly one place to put one.
    """
    cx, cy, z0 = int(round(roof.cx)), int(round(roof.cy)), roof.apex
    arm = max(1, size // 3)
    for z in range(z0, z0 + size):
        for dy in (-1, 0, 1):
            m.set(cx, cy + dy, z, CROSS)
            m.set(cx + 1, cy + dy, z, CROSS)
    for dx in range(-arm, arm + 1):
        for dy in (-1, 0, 1):
            m.set(cx + dx, cy + dy, z0 + size - arm, CROSS)
            m.set(cx + dx, cy + dy, z0 + size - arm + 1, CROSS)
