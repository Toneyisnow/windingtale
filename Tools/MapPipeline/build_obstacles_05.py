"""Build the seven building models chapter 05 needs.

Run once to produce, in Resources/Remastered/Obstacles/vox/:

    red_cathedral_1  10 cols x 9 rows |  together, the hall across the top-left
    red_cathedral_2   9 cols x 9 rows |  of the map
    blue_church_1     7 cols x 8 rows |  together, the cross-topped church in
    blue_church_2     5 cols x 8 rows |  the top-right corner
    red_mansion_1    10 cols x 9 rows    the house in the middle of the village
    blue_hall_1       4 cols x 8 rows    one blue roof over one arched door
    blue_hall_2       4 cols x 8 rows    the same hall, shuttered instead of open

Chapter 05 also uses thatched_hut_1, which already exists -- the wooden barn is
the same sprite chapter 02 stands beside its churches, pixel for pixel.

The architecture is the one ``housekit`` was written for and chapter 02's blue
churches are made of: parallel wings under square-pyramid roofs, beige stone
walls on a stone plinth, arched openings, a rose window let into the slope over
the entrance. Chapter 05 adds three things to it.

**Red roofs** -- ``housekit.RED_ROOF``, sampled from Chapter-05.png.

**A long hall behind the row of front wings.** ``wing()``'s pyramid comes to a
point, which is right for one bay and wrong for the unbroken roof the art draws
across the back of the two red buildings. :func:`hall` is the same construction
with the ridge left in: its height depends on y alone.

**Buildings cut into pieces.** A ``.vox`` stores each coordinate in one byte, so
no model can be wider than 255 voxels -- 10 tiles. The cathedral is 19 tiles
wide and the church 12, so each is built as two models that stand side by side
on the board. The seam works because :func:`hall` is uniform along x: both
halves of a roof are the same section, so the ridge runs straight through the
join. The entrance porch is what decides where to cut -- it has to fall inside
one piece, not across the seam.

Heights are the art-scale ones ``housekit.squash`` flattens on the way out; see
that module for the coordinate conventions.
"""

import os

import voxlib
from housekit import (BLUE_ROOF, EAVE, OPENING, PLINTH, RED_ROOF, SHELL, WALL,
                      WALL_DARK, Model, arch, cross, roof_window,
                      rose_window_art, squash, window, wing)


def hall(m, x0, x1, y0, y1, wall_top, ridge_top, roof_colours, plinth=6,
         hip_left=0, hip_right=0):
    """A long bay with a ridged roof: the same wing, minus the hips.

    ``wing()`` takes the roof to a point over the centre, so its height depends
    on how far a voxel is from that centre in *both* axes. Here it depends on y
    only, which leaves a ridge running the full width -- and, because every
    column of the roof is then identical, lets a building be cut into pieces
    that still line up along the join.

    ``hip_left`` / ``hip_right`` put a hip back on at one end, over that many
    voxels. A piece uses them at the building's real ends and leaves the seam
    end square, which is what makes two pieces meet without a crease.
    """
    base = max(SHELL, squash(plinth, wall_top))
    top_of_wall = squash(wall_top, wall_top)
    apex = squash(ridge_top, wall_top)

    m.box(x0, x1, y0, y1, 0, base - 1, PLINTH)
    m.box(x0, x1, y0, y1, base, top_of_wall, WALL)

    cy = (y0 + y1) / 2.0
    hy = (y1 - y0) / 2.0 + EAVE
    rise = apex - top_of_wall
    for x in range(x0 - EAVE, x1 + EAVE + 1):
        for y in range(y0 - EAVE, y1 + EAVE + 1):
            t = abs(y - cy) / hy
            if hip_left and x < x0 + hip_left:
                t = max(t, (x0 + hip_left - x) / float(hip_left + EAVE))
            if hip_right and x > x1 - hip_right:
                t = max(t, (x - (x1 - hip_right)) / float(hip_right + EAVE))
            if t > 1.0:
                continue
            top = top_of_wall + int(round(rise * (1.0 - t)))
            colour = roof_colours.mid if y < cy else roof_colours.dark
            edge = (y < y0 or y > y1)
            for z in range(top - SHELL + 1, top + 1):
                m.set(x, y, z, roof_colours.edge if edge and z == top - SHELL + 1
                      else colour)


def wall_rose(m, cx, y, wall_top, z0, w=22, h=30):
    """The stained-glass arch, painted onto a facade instead of into a roof slope.

    ``roof_window`` puts the same sprite in the pitch over an entrance, which is
    where chapter 02 always has it. Chapter 05 also hangs one flat on a wall --
    on both ends of the mansion and over its door -- and a plain orange
    rectangle there reads as a hole, not a window.
    """
    art = rose_window_art()
    ah, aw = len(art), len(art[0])
    zb = squash(z0, wall_top)
    zt = squash(z0 + h, wall_top)
    m.require_wall(cx, y, (zb + zt) // 2, 'rose window')
    x0 = cx - w // 2
    for i in range(w):
        ax = min(aw - 1, i * aw // w)
        for z in range(zb, zt + 1):
            # the sprite's rows run downward and z runs up
            az = min(ah - 1, (zt - z) * ah // (zt - zb + 1))
            c = art[az][ax]
            if c is None:
                continue
            for dy in range(4):
                m.set(x0 + i, y + dy, z, c)


def canopy_window(m, roof_colours, cx, y, wall_top, z0, w=20, h=16):
    """A window with the little tiled hood the art hangs over it.

    Both red buildings have a row of these. The hood is two courses of roof tile
    stepped out over the opening, which is what stops the row reading as holes
    punched in a flat wall.
    """
    window(m, cx, y, w, h, z0, wall_top, colour=OPENING, frame=WALL_DARK)
    zt = squash(z0 + h, wall_top)
    for i, dz in enumerate((1, 2)):
        over = w // 2 + 3 - i
        for dx in range(-over, over + 1):
            for dy in range(3 + i):
                m.set(cx + dx, y - dy, zt + dz,
                      roof_colours.dark if dx < 0 else roof_colours.light)


def monument(m, cx, y, wall_top, height=54, w=34):
    """The stone tomb-like block standing against a facade.

    A pair of them flanks the entrance of both the church and the mansion. It is
    stone all the way through, so unlike a window it needs no wall behind it --
    it stands on the ground in front of one. ``y`` is its front face; the block
    runs back from there into the wall.
    """
    zt = squash(height, wall_top)
    zc = squash(int(height * 0.45), wall_top)
    m.box(cx - w // 2, cx + w // 2, y, y + 14, 0, zc, PLINTH, hollow=False)
    m.box(cx - w // 2 + 5, cx + w // 2 - 5, y + 3, y + 14, zc + 1, zt, PLINTH,
          hollow=False)
    # the pair of dark round holes in its head
    for dx in (-7, 7):
        for x in range(cx + dx - 3, cx + dx + 4):
            for z in range(zt - 6, zt - 1):
                for dy in range(4):
                    m.set(x, y + 3 + dy, z, OPENING)


# --------------------------------------------------------------------------
# the cathedral across the top-left -- 19 tiles of it, in two pieces
# --------------------------------------------------------------------------

CATHEDRAL_HALL = (118, 212, 60, 150)      # y0, y1, wall_top, ridge
CATHEDRAL_BAY = (48, 150, 68, 128)        # the gabled bays along the front


def _cathedral_bay(m, x0, x1, door=True):
    """One front bay: a gable, a doorway under it, a hooded window over that."""
    y0, y1, wall_top, ridge = CATHEDRAL_BAY
    wing(m, x0, x1, y0, y1, wall_top, ridge, roof_colours=RED_ROOF)
    cx = (x0 + x1) // 2
    if door:
        arch(m, cx, y0, 26, 42, wall_top)
    canopy_window(m, RED_ROOF, cx, y0, wall_top, 44)


def red_cathedral_1():
    """Left half of the cathedral, tiles X 1..10: four bays and the entrance.

    The fifth bay carries the porch and so has no door of its own -- it would be
    walled in behind one.
    """
    m = Model(10, 9, 104)
    hall(m, 0, 239, *CATHEDRAL_HALL, roof_colours=RED_ROOF, hip_left=44)
    for x0, x1 in ((4, 43), (52, 91), (100, 139), (148, 187)):
        _cathedral_bay(m, x0, x1)
    wing(m, 196, 235, *CATHEDRAL_BAY, roof_colours=RED_ROOF)
    porch = wing(m, 194, 236, 6, 58, 80, 158, roof_colours=RED_ROOF)
    arch(m, 215, 6, 30, 56, 80)
    roof_window(m, porch, 20)
    return m


def red_cathedral_2():
    """Right half of the cathedral, tiles X 11..19: four more bays and the end.

    The end is a single narrow tile of wall with two small stained lights in it,
    and it is also the only part of this model chapter 05 shows twice -- the
    building running off the left edge of the map is this same right-hand end.
    """
    m = Model(9, 9, 104)
    hall(m, 0, 215, *CATHEDRAL_HALL, roof_colours=RED_ROOF, hip_right=44)
    for x0, x1 in ((4, 43), (52, 91), (100, 139), (148, 187)):
        _cathedral_bay(m, x0, x1)
    y0, _y1, wall_top, _ridge = CATHEDRAL_BAY
    wing(m, 196, 211, y0, 150, wall_top, 118, roof_colours=RED_ROOF)
    window(m, 203, y0, 14, 12, 50, wall_top)
    window(m, 203, y0, 8, 16, 28, wall_top)
    return m


# --------------------------------------------------------------------------
# the church in the top-right corner -- 12 tiles, in two pieces
# --------------------------------------------------------------------------

CHURCH_HALL = (100, 186, 58, 128)
CHURCH_BAY = (24, 170, 62, 112)


def blue_church_1():
    """Left part of the church, tiles X 26..32, and the half with the cross.

    Two rows of it hang off the top of the map, so the model is 8 rows against
    the 6 the art shows.
    """
    m = Model(7, 8, 124)
    hall(m, 0, 167, *CHURCH_HALL, roof_colours=BLUE_ROOF, hip_left=40)
    for x0, x1 in ((4, 43), (48, 115)):
        wing(m, x0, x1, *CHURCH_BAY)
    porch = wing(m, 122, 164, 4, 64, 78, 152)
    arch(m, 143, 4, 30, 56, 78)
    roof_window(m, porch, 22)
    cross(m, porch, 20)

    y0, _y1, wall_top, _ridge = CHURCH_BAY
    window(m, 14, y0, 16, 16, 40, wall_top)          # the square light on the end
    for cx in (36, 62):
        window(m, cx, y0, 12, 22, 34, wall_top)
    monument(m, 98, y0 - 14, wall_top)
    return m


def blue_church_2():
    """Right part of the church, tiles X 33..37 -- the mirror of the other half."""
    m = Model(5, 8, 124)
    hall(m, 0, 119, *CHURCH_HALL, roof_colours=BLUE_ROOF, hip_right=40)
    for x0, x1 in ((0, 67), (72, 115)):
        wing(m, x0, x1, *CHURCH_BAY)

    y0, _y1, wall_top, _ridge = CHURCH_BAY
    monument(m, 22, y0 - 14, wall_top)
    for cx in (58, 84):
        window(m, cx, y0, 12, 22, 34, wall_top)
    window(m, 106, y0, 16, 16, 40, wall_top)
    return m


# --------------------------------------------------------------------------
# the mansion in the middle of the village -- 10 tiles, one piece
# --------------------------------------------------------------------------

def red_mansion_1():
    """The house in the middle of the village: 10 cols x 9 rows.

    The cathedral's plan at a fifth of the width: a ridged hall, an outer pair of
    bays set back with a rose window each, an inner pair pushed forward under the
    two tall gables the art draws, and the porch between them.

    It is used twice. The second is the building running off the right edge of
    the map, which shows one tile of itself and is cleaned to match.
    """
    m = Model(10, 9, 100)
    hall(m, 0, 239, 110, 212, 60, 132, roof_colours=RED_ROOF,
         hip_left=44, hip_right=44)

    for x0, x1 in ((4, 43), (196, 235)):             # outer bays, set back
        wing(m, x0, x1, 24, 190, 70, 124, roof_colours=RED_ROOF)
    for x0, x1 in ((52, 91), (148, 187)):            # inner bays, pushed forward
        wing(m, x0, x1, 48, 190, 66, 152, roof_colours=RED_ROOF)
    # the porch stands on a bay of its own; without it the roof would fall away
    # into a well between the two inner gables
    wing(m, 100, 139, 48, 190, 66, 128, roof_colours=RED_ROOF)
    wing(m, 100, 139, 16, 64, 74, 130, roof_colours=RED_ROOF)

    for cx in (24, 216):
        wall_rose(m, cx, 24, 70, 28, w=22, h=30)
    for cx in (71, 167):
        canopy_window(m, RED_ROOF, cx, 48, 66, 42)
        monument(m, cx, 34, 66, height=44)
    wall_rose(m, 119, 16, 74, 44, w=20, h=26)
    monument(m, 119, 2, 74, height=40)
    return m


# --------------------------------------------------------------------------
# the two small halls
# --------------------------------------------------------------------------

def blue_hall_1():
    """One hall under one blue roof, with the door standing open: 4 cols x 8 rows.

    Chapter 02's blue_house_5 is the same idea, but that one is a chapel -- rose
    window, cross. This is a house: nothing on the roof, nothing in the gable.
    """
    m = Model(4, 8, 100)
    wing(m, 4, 91, 6, 186, 72, 156)
    arch(m, 47, 6, 30, 58, 72)
    return m


def blue_hall_2():
    """The same hall shuttered: two hooded windows instead of the door."""
    m = Model(4, 8, 100)
    wing(m, 4, 91, 6, 186, 72, 156)
    for cx in (28, 66):
        canopy_window(m, BLUE_ROOF, cx, 6, 72, 14, w=18, h=14)
    return m


BUILDERS = {
    'red_cathedral_1': red_cathedral_1,
    'red_cathedral_2': red_cathedral_2,
    'blue_church_1': blue_church_1,
    'blue_church_2': blue_church_2,
    'red_mansion_1': red_mansion_1,
    'blue_hall_1': blue_hall_1,
    'blue_hall_2': blue_hall_2,
}


def main():
    root = voxlib.workspace_root()
    out_dir = voxlib.obstacles_vox_dir(root)
    if not os.path.isdir(out_dir):
        os.makedirs(out_dir)
    for name, build in sorted(BUILDERS.items()):
        m = build()
        m.solidify()
        voxels = m.voxels()
        path = os.path.join(out_dir, name + '.vox')
        voxlib.write_vox(path, (m.w, m.d, m.h), voxels)
        print('%-16s SIZE %3dx%3dx%-4d  %d cols x %d rows  %7d voxels  -> %s'
              % (name, m.w, m.d, m.h, m.w // voxlib.TILE, m.d // voxlib.TILE,
                 len(voxels), path))


if __name__ == '__main__':
    main()
