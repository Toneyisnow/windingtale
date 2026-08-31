"""Build the five blue-roofed church models chapter 02 needs.

Run once to produce Resources/Remastered/Obstacles/vox/blue_house_{1..5}.vox.
Chapter 02 also uses thatched_hut_1 and barrel_group_1, which already exist.

The 2D art draws these buildings as a set of parallel wings seen from the front:
a tall peak over the entrance flanked by lower ones, beige stone walls, a stone
plinth, and arched openings. That is what ``wing()`` builds, so each house here
is just a list of wings plus its openings.

Each wing is roofed with a square pyramid rather than a ridge, so it comes to a
point from every direction instead of running back as a long hall.

``wing()`` and the rest of the machinery now live in ``housekit``, shared with
the other chapters' builders; the coordinate conventions and the height squash
are documented there.
"""

import os

import voxlib
from housekit import Model, arch, cross, roof_window, window, wing

# --------------------------------------------------------------------------
# the five houses
# --------------------------------------------------------------------------

def blue_house_1():
    """Top-left cathedral: 10 cols x 9 rows, the biggest on the map.

    Four flanking wings with two taller ones between them, a central entrance
    and a stone gatehouse in front. The cross stands on the porch's peak, the
    highest point of the model.
    """
    m = Model(10, 9, 140)
    wing(m, 6, 57, 40, 190, 66, 118)
    wing(m, 58, 105, 40, 200, 74, 150)
    wing(m, 106, 153, 40, 200, 74, 150)
    wing(m, 154, 205, 40, 190, 66, 118)
    porch = wing(m, 90, 149, 12, 60, 80, 158)  # entrance porch, pushed forward
    arch(m, 119, 12, 30, 54, 80)               # main door
    roof_window(m, porch, 20)                  # rose window, set into the slope
    cross(m, porch, 20)
    # The four wings run x 6..205, so they are centred on 105 rather than on the
    # 240-wide box. Mirror the end-wing openings about that, not about the box,
    # or the right-hand pair lands past the wall.
    arch(m, 47, 40, 24, 46, 66)
    arch(m, 164, 40, 24, 46, 66)
    window(m, 20, 40, 16, 14, 40, 66)
    window(m, 191, 40, 16, 14, 40, 66)
    return m


def blue_house_2():
    """Repeated twice (top-right and bottom-left): 7 cols x 7 rows."""
    m = Model(7, 7, 107)
    left = wing(m, 4, 55, 30, 150, 62, 112)
    wing(m, 56, 111, 24, 156, 70, 140)
    wing(m, 112, 163, 30, 150, 62, 112)
    arch(m, 83, 24, 28, 52, 70)
    cross(m, left, 16)
    window(m, 28, 30, 18, 14, 36, 62)
    window(m, 138, 30, 18, 14, 36, 62)
    return m


def blue_house_3():
    """Centre church: 8 cols x 8 rows, tall nave with a cross."""
    m = Model(8, 8, 137)
    wing(m, 4, 59, 40, 170, 66, 112)
    nave = wing(m, 60, 131, 20, 180, 78, 156)
    wing(m, 132, 187, 40, 170, 66, 112)
    arch(m, 95, 20, 32, 58, 78)
    roof_window(m, nave, 22)
    cross(m, nave, 18)
    for cx in (24, 44, 148, 168):
        window(m, cx, 40, 14, 20, 44, 66)
    return m


def blue_house_4():
    """Right-hand house: 6 cols x 7 rows, runs off the right edge of the map."""
    m = Model(6, 7, 108)
    wing(m, 4, 55, 36, 156, 64, 114)
    wing(m, 56, 111, 28, 162, 72, 142)
    wing(m, 112, 143, 36, 156, 64, 110)
    # The centre wing is pushed forward, so its openings go on y = 28 and the
    # side wings' on y = 36. Mixing the two leaves them floating in the gap.
    arch(m, 83, 28, 26, 50, 72)
    window(m, 26, 36, 16, 22, 42, 64)
    window(m, 127, 36, 16, 22, 42, 64)
    return m


def blue_house_5():
    """Bottom-centre house: 4 cols x 8 rows, runs off the bottom of the map.

    One hall under one roof -- the art shows a single peak over the entrance, so
    this must not be built from flanking wings the way the wider houses are.
    """
    m = Model(4, 8, 121)
    hall = wing(m, 4, 91, 30, 186, 74, 140)
    arch(m, 47, 30, 26, 52, 74)
    roof_window(m, hall, 18)
    cross(m, hall, 16)
    return m


BUILDERS = {
    'blue_house_1': blue_house_1,
    'blue_house_2': blue_house_2,
    'blue_house_3': blue_house_3,
    'blue_house_4': blue_house_4,
    'blue_house_5': blue_house_5,
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
