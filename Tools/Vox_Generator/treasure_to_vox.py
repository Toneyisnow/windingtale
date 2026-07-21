"""
treasure_to_vox.py
------------------

Generate a treasure-chest ``.vox`` model (procedural, no source PNG needed).
Colours come from a named theme in THEMES -- '01' is the navy + gold chest,
'02' the lighter blue + white/silver one.

Shape (defaults)
~~~~~~~~~~~~~~~~

    footprint      24 x 24  (X = width, Y = depth)
    z = 0 .. 7     8-voxel tall rectangular base
    z = 8 .. 13    barrel lid, half-cylinder whose axis runs along +X,
                   half-width 12 in Y, squashed to 6 tall in Z
    total height   14

The lid is an ellipse (12 wide x LID_H tall), not a true circle, so it can be
flattened independently of the footprint. Pass ``--semicircle`` for the exact
radius-12 circular profile (lid height = 12).

The base is hollow (walls WALL_T thick, floor FLOOR_T thick) so an opened lid
reveals an empty interior.

The decoration (trim arcs, lock plate, eyes) is positioned from base_h/lid_h
via decor_levels(), so re-proportioning the box keeps it laid out sensibly.

Usage
~~~~~

    python treasure_to_vox.py                    # TreasureBox_01.vox
    python treasure_to_vox.py --open             # TreasureBox_01_Opened.vox
    python treasure_to_vox.py -t 02              # TreasureBox_02.vox
    python treasure_to_vox.py -t 02 --open       # TreasureBox_02_Opened.vox
    python treasure_to_vox.py --semicircle       # true r=12 arc
    python treasure_to_vox.py -o some/path.vox

To build every variant and push them to Unity, use build_treasures.py.

Coordinate convention matches tai_to_vox.py / png4_to_vox.py:
+X = right, +Y = into-the-scene (depth), +Z = up. The chest "faces" -Y.
"""

import math
import os
import struct
import sys

# --------------------------------------------------------------------------- #
# defaults / tunables                                                          #
# --------------------------------------------------------------------------- #

OUT_DIR = (r'D:\SourceCode\Git\toneyisnow\windingtale\Resources'
           r'\Remastered\Others')

WIDTH      = 24     # X
DEPTH      = 24     # Y
BASE_H     = 8      # z = 0 .. BASE_H-1
LID_H      = 6      # z = BASE_H .. BASE_H+LID_H-1   (12 with --semicircle)

# Colour themes. All blues are held at hue ~210-215 deg (azure) rather than
# ~217+ (navy): at these values, next to a warm trim, 217 reads purple to the
# eye. Keeping green comfortably above red is what stops the purple cast.
#
#   body / body_lit / body_dark  planks, crown highlight, shadow + inner wall
#   trim / trim_dark             straps, rim, lock  (+ its shadow)
#   eye / eye_core               glowing eye, hot centre
THEMES = {
    # navy + gold
    '01': dict(
        body      = (14, 42, 74),      # hue 212
        body_lit  = (26, 72, 118),     # hue 210
        body_dark = (7, 22, 42),       # hue 214
        trim      = (240, 168, 40),
        trim_dark = (168, 106, 20),
        eye       = (255, 206, 74),
        eye_core  = (255, 240, 170),
    ),
    # lighter blue + white/silver
    '02': dict(
        body      = (34, 84, 152),     # hue 215
        body_lit  = (60, 118, 196),    # hue 214
        body_dark = (16, 46, 86),      # hue 214
        trim      = (222, 230, 240),
        trim_dark = (142, 156, 176),
        eye       = (214, 240, 255),
        eye_core  = (255, 255, 255),
    ),
}
DEFAULT_THEME = '01'

# eye sprite, 5 wide x 4 tall, drawn top row first (this is the LEFT eye;
# the right eye is the mirror image). 'X' = eye, '#' = hot core.
EYE = [
    '..XX.',
    '.X##X',
    'X####',
    '.XXX.',
]
EYE_X0 = 5          # left eye starts at this X

# gold straps: two arcs on the front face. They start wide apart at the crown
# and bow inward on the way down, meeting at the lock near the bottom.
STRAP_DX_MAX = 10.0     # half-separation at the crown, in voxels
STRAP_CURVE  = 0.5      # exponent; <1 = stays wide up top, snaps in at the end

LOCK_X0, LOCK_X1 = 10, 13   # lock plate X span (inclusive)

# the base is a real container, so an open lid shows an empty interior
WALL_T  = 2         # side wall thickness
FLOOR_T = 2         # floor thickness

# Everything below is derived from base_h / lid_h so the decoration keeps its
# proportions when the box is re-proportioned (see decor_levels()).
STRAP_Z_LOCK_FRAC = 0.30    # convergence height, as a fraction of base_h
LOCK_Z0_FRAC      = 0.25    # lock plate bottom, as a fraction of base_h


# --------------------------------------------------------------------------- #
# geometry                                                                     #
# --------------------------------------------------------------------------- #

def lid_half_depth(z, base_h, lid_h, depth):
    """Half-extent in Y of the lid arc at height ``z`` (voxel index)."""
    a = depth / 2.0                      # 12 -- horizontal radius
    b = float(lid_h)                     # vertical radius
    t = (z + 0.5 - base_h) / b           # 0 at the lid seam, ~1 at the top
    if t >= 1.0:
        return 0.0
    return a * math.sqrt(1.0 - t * t)


def decor_levels(base_h, lid_h):
    """Z levels for the decoration, derived from the box proportions.

    Returns (z_lock, lock_z0, lock_z1, eye_z_top):
      z_lock    -- where the two gold arcs converge
      lock_z0/1 -- lock plate span, rising from there to the lid seam
      eye_z_top -- top row of the eye sprite, one below the crown so the
                   4-row sprite still lands on the lid however flat it is
    """
    z_lock = base_h * STRAP_Z_LOCK_FRAC
    lock_z0 = max(1, int(round(base_h * LOCK_Z0_FRAC)))
    lock_z1 = base_h
    top_z = base_h + lid_h - 1
    eye_z_top = min(top_z - 1, base_h + lid_h - 2)
    return z_lock, lock_z0, lock_z1, eye_z_top


def strap_dx(z, top_z, z_lock):
    """Half-separation of the two gold arcs at height ``z``.

    Widest at the crown, curving inward as it descends until the two sides
    meet over the lock at ``z_lock``.
    """
    span = top_z - z_lock
    t = (z - z_lock) / span if span > 0 else 0.0
    t = max(0.0, min(1.0, t))
    return STRAP_DX_MAX * (t ** STRAP_CURVE)


def paint_front(vox, x, z, color):
    """Paint the front-most (lowest Y) existing voxel of column (x, z)."""
    for y in range(256):
        if (x, y, z) in vox:
            vox[(x, y, z)] = color
            return True
        if y > 64:
            break
    return False


def open_lid(vox, width, depth, base_h, angle_deg=45.0):
    """Hinge the lid open by ``angle_deg`` about the back-bottom edge.

    The base (z < base_h) is left exactly as-is; everything at or above the
    seam is rotated about the X-aligned hinge line at (y = depth-0.5,
    z = base_h-0.5), i.e. the lid's back-bottom edge.

    Rotation is done by *inverse* sampling -- for every candidate destination
    voxel we rotate backwards and look up the source -- so the result has no
    holes, which a forward splat at 45 degrees would leave.
    """
    th = math.radians(angle_deg)
    cos_t, sin_t = math.cos(th), math.sin(th)
    y0 = depth - 0.5
    z0 = base_h - 0.5

    base = {k: v for k, v in vox.items() if k[2] < base_h}
    lid = {k: v for k, v in vox.items() if k[2] >= base_h}
    if not lid:
        return dict(base)

    # how far the swung lid can reach, from the corners of the lid's bbox
    lid_y = [k[1] for k in lid]
    lid_z = [k[2] for k in lid]
    reach = []
    for y in (min(lid_y), max(lid_y)):
        for z in (min(lid_z), max(lid_z)):
            dy, dz = y + 0.5 - y0, z + 0.5 - z0
            reach.append((y0 + dy * cos_t + dz * sin_t,
                          z0 - dy * sin_t + dz * cos_t))
    y_lo = int(math.floor(min(r[0] for r in reach))) - 1
    y_hi = int(math.ceil(max(r[0] for r in reach))) + 1
    z_hi = int(math.ceil(max(r[1] for r in reach))) + 1

    out = dict(base)
    for x in range(width):
        for yt in range(y_lo, y_hi + 1):
            for zt in range(base_h, z_hi + 1):
                dyt, dzt = yt + 0.5 - y0, zt + 0.5 - z0
                dy = dyt * cos_t - dzt * sin_t          # inverse rotation
                dz = dyt * sin_t + dzt * cos_t
                sy = int(round(y0 + dy - 0.5))
                sz = int(round(z0 + dz - 0.5))
                c = lid.get((x, sy, sz))
                if c is not None:
                    out[(x, yt, zt)] = c

    # the swung lid overhangs the back, so shift everything back into >= 0
    shift_y = -min(k[1] for k in out)
    if shift_y:
        out = {(x, y + shift_y, z): c for (x, y, z), c in out.items()}
    return out


def build_voxels(width, depth, base_h, lid_h, pal):
    """Return {(x, y, z): (r, g, b)}. ``pal`` is one of THEMES' dicts."""
    vox = {}
    cy = (depth - 1) / 2.0
    top_z = base_h + lid_h - 1
    z_lock, lock_z0, lock_z1, eye_z_top = decor_levels(base_h, lid_h)

    # ---- base (hollow: walls + floor, open on top) --------------------- #
    def in_cavity(x, y, z):
        return (WALL_T <= x < width - WALL_T
                and WALL_T <= y < depth - WALL_T
                and z >= FLOOR_T)

    for z in range(base_h):
        for x in range(width):
            for y in range(depth):
                if in_cavity(x, y, z):
                    continue                         # empty box interior
                edge_x = x < 2 or x >= width - 2
                edge_y = y < 2 or y >= depth - 2
                # a voxel touching the cavity is inner wall / cavity floor
                inner = any(in_cavity(x + dx, y + dy, z + dz)
                            for dx, dy, dz in ((1, 0, 0), (-1, 0, 0),
                                               (0, 1, 0), (0, -1, 0),
                                               (0, 0, 1)))
                if z == base_h - 1:
                    c = pal['trim']                       # gold rim around the mouth
                elif z == 0:
                    c = pal['trim_dark']                  # gold foot rim
                elif inner:
                    c = pal['body_dark']                  # shadowed inner wall
                elif edge_x and edge_y:
                    c = pal['trim_dark']                  # corner straps
                else:
                    c = pal['body']
                vox[(x, y, z)] = c

    # ---- lid (half cylinder, axis along X) ---------------------------- #
    for z in range(base_h, base_h + lid_h):
        half = lid_half_depth(z, base_h, lid_h, depth)
        if half <= 0.0:
            continue
        # how far up the arc we are: 0 at the seam, 1 at the crown
        up = (z + 0.5 - base_h) / float(lid_h)
        for x in range(width):
            for y in range(depth):
                dy = y + 0.5 - cy
                if abs(dy) > half:
                    continue
                surface = (abs(dy) > half - 1.0) or (z == top_z)
                if not surface:
                    vox[(x, y, z)] = pal['body_dark']
                    continue
                vox[(x, y, z)] = pal['body_lit'] if up > 0.72 else pal['body']

    # ---- gold arcs ----------------------------------------------------- #
    # Two bands sweeping down the front face from the crown, bowing inward
    # until they converge on the lock. Each band is 2 voxels wide.
    cx = (width - 1) / 2.0
    for z in range(1, top_z + 1):
        dx = strap_dx(z, top_z, z_lock)
        shade = pal['trim'] if z >= base_h else pal['trim_dark']
        for side in (-1, 1):
            x0 = int(round(cx + side * dx - 0.5))
            for x in (x0, x0 + 1):
                if 0 <= x < width:
                    paint_front(vox, x, z, shade)
        # let the band crest the lid: at the top layer, run it right over
        if z == top_z:
            for side in (-1, 1):
                x0 = int(round(cx + side * dx - 0.5))
                for x in (x0, x0 + 1):
                    for y in range(depth):
                        if 0 <= x < width and (x, y, top_z) in vox:
                            vox[(x, y, top_z)] = pal['trim']

    # ---- front lock plate + keyhole ------------------------------------ #
    for x in range(LOCK_X0, LOCK_X1 + 1):
        for z in range(lock_z0, lock_z1 + 1):
            paint_front(vox, x, z, pal['trim'])
    keyhole_z = (lock_z0 + lock_z1) // 2         # centred in the plate
    for x in range(LOCK_X0 + 1, LOCK_X1):
        for z in (keyhole_z, keyhole_z + 1):
            paint_front(vox, x, z, pal['body_dark'])      # keyhole

    # ---- eyes on the front of the lid ---------------------------------- #
    for mirror in (False, True):
        for row, pattern in enumerate(EYE):
            z = eye_z_top - row
            if not (base_h <= z < base_h + lid_h):
                continue
            for col, ch in enumerate(pattern):
                if ch == '.':
                    continue
                col_i = len(pattern) - 1 - col if mirror else col
                x = (width - 1 - EYE_X0 - col_i) if mirror else (EYE_X0 + col_i)
                if not (0 <= x < width):
                    continue
                # paint the front-most existing voxel of this (x, z) column
                for y in range(depth):
                    if (x, y, z) in vox:
                        vox[(x, y, z)] = pal['eye_core'] if ch == '#' else pal['eye']
                        break

    return vox


# --------------------------------------------------------------------------- #
# .vox writer (same layout as tai_to_vox.py)                                   #
# --------------------------------------------------------------------------- #

def write_vox(path, vox, dims):
    palette = []
    color_to_idx = {}

    def palette_index(c):
        key = (c[0], c[1], c[2], 255)
        idx = color_to_idx.get(key)
        if idx is not None:
            return idx
        if len(palette) >= 255:
            best_i, best_d = 1, 10 ** 12
            for i, p in enumerate(palette):
                d = sum((p[k] - key[k]) ** 2 for k in range(3))
                if d < best_d:
                    best_d, best_i = d, i + 1
            return best_i
        palette.append(key)
        color_to_idx[key] = len(palette)
        return len(palette)

    voxels = [(x, y, z, palette_index(c)) for (x, y, z), c in sorted(vox.items())]

    def chunk(cid, content, children=b''):
        return cid + struct.pack('<ii', len(content), len(children)) + content + children

    size_chunk = chunk(b'SIZE', struct.pack('<iii', *dims))

    xyzi_body = bytearray(struct.pack('<i', len(voxels)))
    for (x, y, z, c) in voxels:
        xyzi_body += bytes([x, y, z, c])
    xyzi_chunk = chunk(b'XYZI', bytes(xyzi_body))

    rgba_body = bytearray()
    for i in range(256):
        r, g, b = palette[i][:3] if i < len(palette) else (0, 0, 0)
        rgba_body += bytes([r, g, b, 255])
    rgba_chunk = chunk(b'RGBA', bytes(rgba_body))

    main_chunk = chunk(b'MAIN', b'', size_chunk + xyzi_chunk + rgba_chunk)
    os.makedirs(os.path.dirname(path), exist_ok=True)
    with open(path, 'wb') as f:
        f.write(b'VOX ' + struct.pack('<i', 150) + main_chunk)

    return len(voxels), len(palette)


# --------------------------------------------------------------------------- #
# CLI                                                                          #
# --------------------------------------------------------------------------- #

def main(argv=None):
    import argparse
    p = argparse.ArgumentParser(
        prog='treasure_to_vox.py',
        description='Generate a voxel treasure chest (.vox).')
    p.add_argument('-o', '--out', default=None,
                   help='output .vox path (default: OUT_DIR/TreasureBox_'
                        '<theme>[_Opened].vox)')
    p.add_argument('-t', '--theme', default=DEFAULT_THEME, choices=sorted(THEMES),
                   help=f'colour theme (default {DEFAULT_THEME})')
    p.add_argument('-w', '--width', type=int, default=WIDTH)
    p.add_argument('-d', '--depth', type=int, default=DEPTH)
    p.add_argument('-b', '--base-height', type=int, default=BASE_H)
    p.add_argument('-l', '--lid-height', type=int, default=LID_H,
                   help=f'lid arc height (default {LID_H}; total = base + lid)')
    p.add_argument('--semicircle', action='store_true',
                   help='use a true half-cylinder (lid height = depth/2), '
                        'making the model 1 voxel taller')
    p.add_argument('--open', dest='open_angle', nargs='?', type=float,
                   const=45.0, default=None, metavar='DEG',
                   help='hinge the lid open by DEG degrees (default 45); '
                        'the base is untouched')
    args = p.parse_args(argv if argv is not None else sys.argv[1:])

    lid_h = args.depth // 2 if args.semicircle else args.lid_height
    out = args.out or os.path.join(
        OUT_DIR,
        'TreasureBox_{}{}.vox'.format(
            args.theme, '_Opened' if args.open_angle is not None else ''))

    vox = build_voxels(args.width, args.depth, args.base_height, lid_h,
                       THEMES[args.theme])
    if args.open_angle is not None:
        vox = open_lid(vox, args.width, args.depth,
                       args.base_height, args.open_angle)

    dims = (args.width,
            max(k[1] for k in vox) + 1,
            max(k[2] for k in vox) + 1)
    n_vox, n_col = write_vox(out, vox, dims)
    state = (f'lid open {args.open_angle:g} deg'
             if args.open_angle is not None else 'closed')
    print(f'wrote {out}: {n_vox} voxels, {n_col} palette colors, '
          f'dims {dims[0]}x{dims[1]}x{dims[2]} '
          f'(base {args.base_height} + lid {lid_h}, theme {args.theme}, {state})')


if __name__ == '__main__':
    main()
