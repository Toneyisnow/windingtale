"""Build the three models chapter 08 adds.

Run once to produce, in Resources/Remastered/Obstacles/vox/:

    castle_wall_1    29 cols x 8 rows   the royal castle across the top of the map
    stone_statue_1    1 col  x 1 row    the helmed knight bust on a pedestal
    stone_statue_2    1 col  x 1 row    the hooded figure on the same pedestal

The two red-roofed houses at the bottom of the map are chapter 05's
``red_mansion_1`` standing again and are not built here.

**The castle wall is one obstacle.** The art paints it as a single elevation
29 tiles wide -- curtain wall, a dark tower at each side of the gate, the gate
itself, a second dark tower at the right edge -- and it is modelled as one
piece so it reads as one building and fades as one when the cursor goes behind
it. At 696 voxels it is wider than a .vox part can be, so ``voxlib.write_vox``
splits it into three and ``vox_batch_to_obj`` exports it through ``voxmesh``,
which merges the flat stone faces instead of writing one quad per voxel.

Only the *shell* of the wall is written -- every voxel with a face on the
outside. A solid block this size would be 14 million voxels and a 56 MB file;
the shell is under a million. ``voxmesh`` seals the hollow before meshing, so
the exported OBJ has no inside surface.

Model space is the obstacle convention (formats.md): x runs left to right along
the map, y = 0 is the map row nearest the camera (row 8 of the footprint) and
y = 191 the top edge of the map, z is up. A map row r of the footprint covers
y = (8 - r) * 24 .. + 23.

Heights are at the scale the other obstacles were cut down to: the curtain
wall is 84 (a house is ~100), the gate towers 132 with their merlons.
"""

import os

import numpy as np

import voxlib

TILE = voxlib.TILE


def P(rgb):
    return voxlib.palette_index(rgb)


# sampled from Chapter-08.png
LIGHT_SHADES = (P((176, 196, 180)), P((128, 148, 128)), P((116, 132, 112)))
LIGHT_MORTAR = P((92, 96, 80))
LIGHT_BODY = P((128, 148, 128))
DARK_SHADES = (P((80, 80, 64)), P((64, 60, 48)), P((48, 44, 36)))
DARK_MORTAR = P((36, 32, 24))
DARK_BODY = P((64, 60, 48))
WALK = P((104, 116, 96))          # the flagstones on top of the wall
WALK_SEAM = P((80, 80, 64))
OPENING = P((28, 24, 20))          # the inside of the gate passage
STATUE = (P((116, 136, 136)), P((100, 120, 120)), P((72, 92, 92)), P((44, 64, 64)))
STATUE_DARK = P((0, 0, 0))

# materials, before colouring
LIGHT, DARK, LINING = 1, 2, 3

BRICK_W, COURSE_H = 12, 6


# --------------------------------------------------------------------------
# the castle wall
# --------------------------------------------------------------------------

COLS, ROWS, HEIGHT = 29, 8, 144
CURTAIN_H = 84
INNER_H = 108
GATE_H = 120
TOWER_H = 132
MERLON = 12          # merlon height, and the width of a merlon and of a gap
PARAPET = 6          # how deep the merlons are, front to back


def row_y(r):
    """y range (start, end exclusive) of footprint row r (1 = top of the map)."""
    return (ROWS - r) * TILE, (ROWS - r + 1) * TILE


def col_x(c):
    return (c - 1) * TILE, c * TILE


def merlons(mat, x0, x1, y0, y1, z0, z1, m, along_x=True):
    """A crenellated parapet: alternate MERLON-wide blocks and gaps."""
    for x in range(x0, x1):
        for y in range(y0, y1):
            u = x - x0 if along_x else y - y0
            if (u // MERLON) % 2 == 0:
                mat[x, y, z0:z1] = m


def castle_wall_1():
    W, D = COLS * TILE, ROWS * TILE
    mat = np.zeros((W, D, HEIGHT), dtype=np.uint8)

    # curtain wall across the whole width, rows 1..6, with a walkway on top and
    # merlons along its front edge
    y0, _ = row_y(6)
    mat[:, y0:D, 0:CURTAIN_H] = LIGHT
    merlons(mat, 0, W, y0, y0 + PARAPET, CURTAIN_H, CURTAIN_H + MERLON, LIGHT)

    # a taller inner wall along the back two rows, so the castle has depth
    # behind the walkway rather than a flat top six tiles deep
    yi, _ = row_y(2)
    mat[:, yi:D, 0:INNER_H] = LIGHT
    merlons(mat, 0, W, yi, yi + PARAPET, INNER_H, INNER_H + MERLON, LIGHT)

    # the gatehouse, tiles X 8..22, comes forward one row (its face is on row 7)
    gx0, _ = col_x(8)
    _, gx1 = col_x(22)
    gy0, _ = row_y(7)
    mat[gx0:gx1, gy0:D, 0:GATE_H] = LIGHT
    merlons(mat, gx0, gx1, gy0, gy0 + PARAPET, GATE_H, GATE_H + MERLON, LIGHT)

    # the two dark towers flanking the gate, X 8..11 and X 19..22, taller again;
    # their front bays stand on row 8 (X 9..11 and X 19..21) either side of the
    # gate floor, which is where the art draws the gate's pillars
    for c0, c1, p0, p1 in ((8, 11, 9, 11), (19, 22, 19, 21)):
        tx0, _ = col_x(c0)
        _, tx1 = col_x(c1)
        mat[tx0:tx1, gy0:D, 0:TOWER_H] = DARK
        px0, _ = col_x(p0)
        _, px1 = col_x(p1)
        mat[px0:px1, 0:gy0, 0:TOWER_H] = DARK
        # merlons along the front of the bay and along the step beside it
        merlons(mat, px0, px1, 0, PARAPET, TOWER_H, TOWER_H + MERLON, DARK)
        step = (tx0, px0) if c0 < p0 else (px1, tx1)
        merlons(mat, step[0], step[1], gy0, gy0 + PARAPET, TOWER_H, TOWER_H + MERLON, DARK)
        outer = tx0 if c0 < p0 else tx1 - PARAPET
        merlons(mat, outer, outer + PARAPET, gy0, y0, TOWER_H, TOWER_H + MERLON, DARK,
                along_x=False)

    # the dark tower at the right edge of the map, X 28..29 (it runs off the board)
    rx0, _ = col_x(28)
    mat[rx0:W, y0:D, 0:INNER_H] = DARK
    merlons(mat, rx0, W, y0, y0 + PARAPET, INNER_H, INNER_H + MERLON, DARK)

    # the gate: an arched passage through the gatehouse over tiles X 12..18. Its
    # walls and ceiling are lined dark, and it ends in darkness a few rows in
    # rather than opening onto the back of the model.
    ax0, _ = col_x(12)
    _, ax1 = col_x(18)
    cx = (ax0 + ax1 - 1) / 2.0
    half = (ax1 - ax0) / 2.0
    ys = np.arange(D)
    xs = np.arange(ax0 - 4, ax1 + 4)
    zs = np.arange(HEIGHT)
    X, Z = np.meshgrid(xs, zs, indexing='ij')

    def arch(rx, straight, rise):
        head = (Z - straight) / float(rise)
        side = (X - cx) / float(rx)
        return (np.abs(side) <= 1.0) & ((Z < straight) | (side ** 2 + head ** 2 <= 1.0))

    lining = arch(half + 4, 56, 24)
    void = arch(half, 52, 20)
    yend, _ = row_y(2)
    for y in range(gy0, D):
        sl = mat[ax0 - 4:ax1 + 4, y, :]
        sl[lining & (sl != 0)] = LINING
        if y < yend:
            sl[void] = 0

    return colour(mat)


def brick_field(u_len, z_len):
    """0 = mortar, 1..3 = the brick shade, for a face parametrised by (u, z)."""
    u = np.arange(u_len)[:, None]
    z = np.arange(z_len)[None, :]
    course = z // COURSE_H
    uu = u + np.where(course % 2 == 1, BRICK_W // 2, 0)
    mortar = (z % COURSE_H == 0) | (uu % BRICK_W == 0)
    shade = 1 + ((uu // BRICK_W) * 7 + course * 13) % 3
    return np.where(mortar, 0, shade).astype(np.uint8)


def colour(mat):
    """Turn a material grid into palette indices on its exposed faces.

    Vertical faces are laid in brick, the tops in flagstone, the gate lining is
    plain dark. Anything with no face on the outside is dropped: the file holds
    the shell and the mesher fills the inside back in.
    """
    W, D, H = mat.shape
    solid = mat != 0
    out = np.zeros_like(mat)

    def exposed(axis, positive):
        shifted = np.zeros_like(solid)
        src = [slice(None)] * 3
        dst = [slice(None)] * 3
        if positive:
            src[axis], dst[axis] = slice(1, None), slice(0, -1)
        else:
            src[axis], dst[axis] = slice(0, -1), slice(1, None)
        shifted[tuple(dst)] = solid[tuple(src)]
        return solid & ~shifted

    ex_x = exposed(0, True) | exposed(0, False)
    ex_y = exposed(1, True) | exposed(1, False)
    ex_top = exposed(2, True)
    ex_bot = exposed(2, False)
    any_face = ex_x | ex_y | ex_top | ex_bot

    # bricks on faces with a y normal are keyed by (x, z); x-normal faces by (y, z)
    by = brick_field(W, H)                # [x, z]
    bx = brick_field(D, H)                # [y, z]
    walk = np.ones((W, D), dtype=np.uint8)
    xs = np.arange(W)[:, None]
    ys = np.arange(D)[None, :]
    walk[((xs % 8 == 0) | (ys % 8 == 0))] = 0

    for material, shades, mortar, body in ((LIGHT, LIGHT_SHADES, LIGHT_MORTAR, LIGHT_BODY),
                                           (DARK, DARK_SHADES, DARK_MORTAR, DARK_BODY)):
        here = mat == material
        lut = np.array([mortar] + list(shades), dtype=np.uint8)
        out[here & (ex_top | ex_bot)] = np.where(walk, WALK, WALK_SEAM)[:, :, None].repeat(H, axis=2)[here & (ex_top | ex_bot)]
        sel = here & ex_x
        out[sel] = lut[bx[None, :, :].repeat(W, axis=0)[sel]]
        sel = here & ex_y
        out[sel] = lut[by[:, None, :].repeat(D, axis=1)[sel]]
    out[(mat == LINING) & any_face] = OPENING

    out[~any_face] = 0
    return out


# --------------------------------------------------------------------------
# the statues
# --------------------------------------------------------------------------

STATUE_H = 40


def pedestal(g):
    light, mid, dark, deep = STATUE
    g[3:21, 3:21, 0:3] = dark             # plinth
    g[5:19, 5:19, 3:13] = mid             # shaft
    g[4:20, 4:20, 12:14] = light          # cap
    g[9:15, 5:6, 6:11] = deep             # the inscription on the front
    return g


def stone_statue_1():
    """The helmed knight: shoulders, a full helm with a dark visor, a crest."""
    light, mid, dark, deep = STATUE
    g = pedestal(np.zeros((TILE, TILE, STATUE_H), dtype=np.uint8))
    g[6:18, 8:17, 14:24] = mid            # torso and shoulders
    g[5:19, 9:16, 20:24] = light          # pauldrons
    g[8:16, 8:16, 24:36] = light          # helm
    g[9:15, 8:9, 28:31] = deep            # visor slit
    g[11:13, 6:18, 34:40] = dark          # crest, front to back
    g[7:8, 9:15, 30:36] = dark            # wings on the helm
    g[16:17, 9:15, 30:36] = dark
    return g


def stone_statue_2():
    """The hooded figure: a cloak falling from a round head, hands clasped."""
    light, mid, dark, deep = STATUE
    g = pedestal(np.zeros((TILE, TILE, STATUE_H), dtype=np.uint8))
    for z in range(14, 30):               # the cloak widens toward the base
        r = 7 - (z - 14) * 3 // 16
        g[12 - r:12 + r, 12 - r:12 + r, z] = mid
    g[9:15, 7:9, 20:24] = light           # hands
    xs = np.arange(TILE)[:, None, None]
    ys = np.arange(TILE)[None, :, None]
    zs = np.arange(STATUE_H)[None, None, :]
    head = (xs - 11.5) ** 2 + (ys - 11.5) ** 2 + (zs - 33) ** 2 <= 5.5 ** 2
    g[head] = light
    g[head & (zs >= 34) & (ys >= 11)] = dark      # the hood over the back of the head
    g[9:15, 8:9, 31:33] = deep            # eyes in shadow
    return g


BUILDERS = {
    'castle_wall_1': castle_wall_1,
    'stone_statue_1': stone_statue_1,
    'stone_statue_2': stone_statue_2,
}


def main():
    root = voxlib.workspace_root()
    out_dir = voxlib.obstacles_vox_dir(root)
    if not os.path.isdir(out_dir):
        os.makedirs(out_dir)
    for name, build in sorted(BUILDERS.items()):
        g = build()
        xs, ys, zs = np.nonzero(g)
        voxels = list(zip(xs.tolist(), ys.tolist(), zs.tolist(), g[xs, ys, zs].tolist()))
        path = os.path.join(out_dir, name + '.vox')
        voxlib.write_vox(path, g.shape, voxels)
        print('%-16s SIZE %3dx%3dx%-4d  %d cols x %d rows  %8d voxels  -> %s'
              % (name, g.shape[0], g.shape[1], g.shape[2], g.shape[0] // TILE,
                 g.shape[1] // TILE, len(voxels), path))


if __name__ == '__main__':
    main()
