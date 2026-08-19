"""Build the five blue-roofed church models chapter 02 needs.

Run once to produce Resources/Remastered/Obstacles/vox/blue_house_{1..5}.vox.
Chapter 02 also uses thatched_hut_1 and barrel_group_1, which already exist.

The 2D art draws these buildings as a set of parallel gabled wings seen from
the front: a tall gable over the entrance flanked by lower ones, beige stone
walls, a stone plinth, and arched openings. That is what ``wing()`` builds, so
each house here is just a list of wings plus its openings.

Model space (see references/formats.md):
    x  0..W-1   map X, left -> right      W = cols * 24
    y  0..D-1   larger y = SMALLER map row, so y = 0 is the FRONT facade
    z  0..H-1   up

Colours are the palette entries the chapter 02 artwork actually resolves to.
"""

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
GLASS = voxlib.palette_index((196, 84, 28))         # stained glass
CROSS = voxlib.palette_index((232, 232, 240))

SHELL = 4          # wall / roof thickness in voxels


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


def wing(m, x0, x1, y0, y1, wall_top, ridge_top, plinth=6):
    """One gabled wing: stone walls with a pitched roof, ridge running front-back.

    The ridge sits over the wing's centre line in x, so the gable triangle
    faces the viewer at y = y0 -- which is how the 2D art draws them.
    """
    # plinth + walls
    m.box(x0, x1, y0, y1, 0, plinth - 1, PLINTH)
    m.box(x0, x1, y0, y1, plinth, wall_top, WALL)
    # shade the left third of every wall face, like the art does
    for x in range(x0, x0 + (x1 - x0) // 3):
        for y in range(y0, y1 + 1):
            for z in range(plinth, wall_top + 1):
                if (x, y, z) in m.v:
                    m.set(x, y, z, WALL_DARK)

    cx = (x0 + x1) / 2.0
    half = (x1 - x0) / 2.0
    rise = ridge_top - wall_top
    eave = 3                      # roof overhang past the wall

    for x in range(x0 - eave, x1 + eave + 1):
        d = abs(x - cx)
        if d > half + eave:
            continue
        top = wall_top + int(round(rise * max(0.0, 1.0 - d / (half + eave))))
        colour = ROOF_LIGHT if x > cx else ROOF_DARK
        if abs(x - cx) <= 1:
            colour = ROOF_MID
        for y in range(y0 - eave, y1 + eave + 1):
            edge = (y < y0 or y > y1 or x < x0 or x > x1)
            for z in range(top - SHELL + 1, top + 1):
                m.set(x, y, z, ROOF_EDGE if edge and z == top - SHELL + 1 else colour)
        # gable infill: the triangle of wall under the front and back slopes
        for y in (y0, y1):
            for z in range(wall_top + 1, top - SHELL + 1):
                m.set(x, y, z, WALL if x <= x1 and x >= x0 else ROOF_EDGE)


def arch(m, cx, y, w, h, z0=0, colour=OPENING, depth=8):
    """A round-topped opening punched into the facade at y (facing front)."""
    r = w // 2
    for dx in range(-r, r + 1):
        for z in range(z0, z0 + h + 1):
            over = z - (z0 + h - r)
            if over > 0 and dx * dx + over * over > r * r:
                continue
            for dy in range(depth):
                m.set(cx + dx, y + dy, z, colour)


def window(m, cx, y, w, h, z0, colour=GLASS, frame=WALL_DARK):
    for dx in range(-w // 2 - 1, w // 2 + 2):
        for z in range(z0 - 1, z0 + h + 2):
            inside = abs(dx) <= w // 2 and z0 <= z <= z0 + h
            for dy in range(4):
                m.set(cx + dx, y + dy, z, colour if inside else frame)


def cross(m, cx, y, z0, size=14):
    for z in range(z0, z0 + size):
        for dy in range(3):
            m.set(cx, y + dy, z, CROSS)
            m.set(cx + 1, y + dy, z, CROSS)
    for dx in range(-size // 3, size // 3 + 1):
        for dy in range(3):
            m.set(cx + dx, y + dy, z0 + size - size // 3, CROSS)
            m.set(cx + dx, y + dy, z0 + size - size // 3 + 1, CROSS)


# --------------------------------------------------------------------------
# the five houses
# --------------------------------------------------------------------------

def blue_house_1():
    """Top-left cathedral: 10 cols x 9 rows, the biggest on the map.

    Four flanking wings with two tall gabled halls between them, a central
    entrance and a stone gatehouse in front.
    """
    m = Model(10, 9, 180)
    wing(m, 6, 57, 40, 190, 66, 118)
    wing(m, 58, 105, 40, 200, 74, 150)
    wing(m, 106, 153, 40, 200, 74, 150)
    wing(m, 154, 205, 40, 190, 66, 118)
    wing(m, 90, 149, 12, 60, 80, 158)          # entrance porch, pushed forward
    arch(m, 119, 12, 30, 54)                   # main door
    window(m, 119, 12, 20, 26, 92)             # rose window over the door
    cross(m, 119, 12, 150, 20)
    arch(m, 47, 40, 24, 46)
    arch(m, 192, 40, 24, 46)
    window(m, 20, 40, 16, 14, 40)
    window(m, 219, 40, 16, 14, 40)
    return m


def blue_house_2():
    """Repeated twice (top-right and bottom-left): 7 cols x 7 rows."""
    m = Model(7, 7, 150)
    wing(m, 4, 55, 30, 150, 62, 112)
    wing(m, 56, 111, 24, 156, 70, 140)
    wing(m, 112, 163, 30, 150, 62, 112)
    arch(m, 83, 24, 28, 52)
    cross(m, 40, 30, 116, 16)
    window(m, 28, 30, 18, 14, 36)
    window(m, 138, 30, 18, 14, 36)
    return m


def blue_house_3():
    """Centre church: 8 cols x 8 rows, tall nave with a cross."""
    m = Model(8, 8, 168)
    wing(m, 4, 59, 40, 170, 66, 112)
    wing(m, 60, 131, 20, 180, 78, 156)
    wing(m, 132, 187, 40, 170, 66, 112)
    arch(m, 95, 20, 32, 58)
    window(m, 95, 20, 22, 28, 96)
    cross(m, 95, 20, 156, 18)
    for cx in (24, 44, 148, 168):
        window(m, cx, 40, 14, 20, 44)
    return m


def blue_house_4():
    """Right-hand house: 6 cols x 7 rows, runs off the right edge of the map."""
    m = Model(6, 7, 150)
    wing(m, 4, 55, 36, 156, 64, 114)
    wing(m, 56, 111, 28, 162, 72, 142)
    wing(m, 112, 143, 36, 156, 64, 110)
    arch(m, 128, 28, 26, 50)
    window(m, 26, 36, 16, 22, 42)
    window(m, 83, 36, 20, 16, 40)
    return m


def blue_house_5():
    """Bottom-centre house: 4 cols x 8 rows, runs off the bottom of the map."""
    m = Model(4, 8, 156)
    wing(m, 4, 27, 60, 180, 60, 100)
    wing(m, 28, 67, 30, 186, 70, 146)
    wing(m, 68, 91, 60, 180, 60, 100)
    arch(m, 47, 30, 26, 52)
    window(m, 47, 30, 18, 24, 92)
    cross(m, 47, 30, 146, 14)
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
