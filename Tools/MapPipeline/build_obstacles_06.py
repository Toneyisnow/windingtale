"""Build the two models chapter 06 needs that no earlier chapter already has.

Run once to produce, in Resources/Remastered/Obstacles/vox/:

    red_church_1     10 cols x 8 rows    the cross-topped church across the
                                         bottom-left of the map
    wooden_crate_1    2 cols x 2 rows    one of the eight cargo crates stacked
                                         on the quay along the top

Chapter 06 is the same architecture chapter 05 stood up, so this is a thin
script: it imports ``housekit`` and chapter 05's ``hall`` rather than restating
either. The church on the right-hand side of the map is not built here at all
-- it is chapter 05's ``red_cathedral_2`` placed again, which is why only one
church is listed above; see obstacles/obstacles_06.json.

**What is different about chapter 06's church.** Chapter 05's cathedral is
nineteen tiles of narrow bays with an open arch in each. This one is ten tiles
of *three* wide bays, each with a stained-glass arch hung flat on its facade,
separated by narrow strips of wall with a single slit light in them. The
entrance is in the middle rather than at one end: a stone monument standing
against the centre bay, under the cross. Ten tiles is exactly the widest a
``.vox`` may be (255 voxels), so unlike the cathedral it still fits in one
piece.

Heights are the art-scale ones ``housekit.squash`` flattens on the way out; see
that module for the coordinate conventions.
"""

import os

import voxlib
from build_obstacles_05 import hall, monument, wall_rose
from housekit import (GLASS, RED_ROOF, Model, cross, window, wing)

# --------------------------------------------------------------------------
# the church across the bottom-left -- 10 tiles, one piece
# --------------------------------------------------------------------------

# y grows toward the BACK of the model, so these read front-to-back: the
# monument stands clear of the bays, the bays clear of the strips between them,
# and the long ridged roof runs across the back of all of it.
CHURCH_HALL = (112, 188, 60, 140)       # y0, y1, wall_top, ridge
CHURCH_BAY = (28, 140, 68, 132)         # the three gabled bays along the front
CHURCH_STRIP = (52, 140, 62, 118)       # the narrow wall between them

# tiles 2..3 in ShapePanel06: the bay whose facade carries the door
CENTRE_BAY = (96, 143)
SIDE_BAYS = ((24, 71), (168, 215))
STRIPS = ((0, 23), (72, 95), (144, 167), (216, 239))


def red_church_1():
    """The whole church, tiles X 1..10 of rows 17..24.

    Three bays of stained glass over a ridged hall, with the entrance in the
    middle bay instead of at an end. The narrow strips between the bays are
    their own low wings: in the art they stand a tile further back than the
    bays and carry one slit light each, which is what stops the facade reading
    as one flat wall ten tiles long.
    """
    m = Model(10, 8, 104)
    hall(m, 0, 239, *CHURCH_HALL, roof_colours=RED_ROOF, hip_left=44, hip_right=44)

    bay_y0, _bay_y1, bay_wall, _bay_ridge = CHURCH_BAY
    strip_y0, _s_y1, strip_wall, _s_ridge = CHURCH_STRIP

    for x0, x1 in STRIPS:
        wing(m, x0, x1, *CHURCH_STRIP, roof_colours=RED_ROOF)
        window(m, (x0 + x1) // 2, strip_y0, 8, 26, 30, strip_wall, colour=GLASS)

    for x0, x1 in SIDE_BAYS:
        wing(m, x0, x1, *CHURCH_BAY, roof_colours=RED_ROOF)
        wall_rose(m, (x0 + x1) // 2, bay_y0, bay_wall, 26, w=30, h=38)

    x0, x1 = CENTRE_BAY
    centre = wing(m, x0, x1, *CHURCH_BAY, roof_colours=RED_ROOF)
    cx = (x0 + x1) // 2
    wall_rose(m, cx, bay_y0, bay_wall, 30, w=30, h=38)
    # the entrance: the same stone block chapter 05 stands beside its doors,
    # here alone and in the middle, which is where the art puts it
    monument(m, cx, bay_y0 - 22, bay_wall, height=52, w=38)
    cross(m, centre, 18)
    return m


# --------------------------------------------------------------------------
# the crates on the quay
# --------------------------------------------------------------------------

# sampled from ShapePanel06's crate tiles (220 / 222 / 276) and then nudged
# onto the palette: the art's #78603c falls on the cube's (102, 102, 51), which
# is olive, so the plank seam is asked for as the brown next to it instead.
WOOD = voxlib.palette_index((136, 108, 72))         # #886c48 -> (153, 102, 51)
WOOD_SHADE = voxlib.palette_index((102, 51, 0))     # the groove between boards
FRAME = voxlib.palette_index((60, 50, 10))          # the beams round the box

CRATE_TOP = 29          # lid height in voxels; a tile is 24 across


def _rim(m, z0, z1, colour, t=3):
    """A band of frame around the four sides, from z0 to z1."""
    for z in range(z0, z1 + 1):
        for x in range(m.w):
            for y in range(m.d):
                if x < t or x >= m.w - t or y < t or y >= m.d - t:
                    m.set(x, y, z, colour)


def wooden_crate_1():
    """One wooden cargo crate: 2 cols x 2 rows on the ground.

    The 2D art gives each crate three tile rows -- one of lid seen from above,
    two of front face standing over the tile in front of it -- so the box on
    the ground is only the back two. The obstacle list cleans the full 2 x 3
    the art covers and stands this 2 x 2 model on the back of it.
    """
    m = Model(2, 2, 40)
    w, d = m.w - 1, m.d - 1

    m.box(0, w, 0, d, 0, CRATE_TOP, WOOD, hollow=False)

    # plank seams: a groove every eight voxels down the sides, and the same
    # boards carried across the lid
    for x in range(m.w):
        if x % 8 == 0:
            for z in range(CRATE_TOP):
                m.set(x, 0, z, WOOD_SHADE)
                m.set(x, d, z, WOOD_SHADE)
    for y in range(m.d):
        if y % 8 == 0:
            for z in range(CRATE_TOP):
                m.set(0, y, z, WOOD_SHADE)
                m.set(w, y, z, WOOD_SHADE)
            for x in range(m.w):
                m.set(x, y, CRATE_TOP, WOOD_SHADE)

    # the frame: a beam round the foot, another under the lid, a post on each
    # corner. Without them the planks read as a solid block of wood.
    _rim(m, 0, 2, FRAME)
    _rim(m, CRATE_TOP - 2, CRATE_TOP, FRAME)
    for x0, x1 in ((0, 3), (w - 3, w)):
        for y0, y1 in ((0, 3), (d - 3, d)):
            m.box(x0, x1, y0, y1, 0, CRATE_TOP, FRAME, hollow=False)
    return m


BUILDERS = {
    'red_church_1': red_church_1,
    'wooden_crate_1': wooden_crate_1,
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
