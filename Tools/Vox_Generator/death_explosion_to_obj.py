"""
death_explosion_to_obj.py -- the map death explosion, 2D sprite -> 3D voxel frames.

Source: the original game's blow-up sprite, OriginData/Other/040-04.bmp .. 040-11.bmp
(eight frames: white-hot fireball -> fireball bursting open -> ring of fire -> red embers
-> the last dying sparks).
Output: WindingTale2/Assets/Resources/Animations/exploration/explosion_00..07.obj + .mtl,
played by CreatureDying / ExplosionPlayer.

The sprite is drawn TOP-DOWN (the original map is seen from above), so each frame is
the explosion's footprint on the ground plane and the height is inferred:

    laplace(h) = -2 inside the lit pixels, h = 0 outside;  half = sqrt(h) - 0.5

gives every blob a circular cross-section -- the filled fireball becomes a ball, the
ring becomes a fire torus with fins where the spikes are, the thin ember wisps stay
thin. Each column keeps its pixel's colour, so from straight above every frame is the
original sprite. The mesh reaches further above y = 0 than below (flames lean up; the
explosion floats round the creature's body centre) and late frames lift a little, like rising embers.

1 voxel = 1 original pixel = 1 OBJ unit. All frames share one origin (the centre of
the 72 x 72 canvas); CreatureDying scales it to the tile size (64 px per tile).
One material per exact sprite colour (m_RRGGBB), read back by CreatureDying.

Usage:  python death_explosion_to_obj.py [source_dir] [out_dir]
"""

import os
import sys

import numpy as np
from PIL import Image
from scipy import ndimage
from scipy.sparse import coo_matrix
from scipy.sparse.linalg import spsolve

HERE = os.path.dirname(os.path.abspath(__file__))
SRC_DIR = r"F:\GameMaterials\炎龙骑士团2\OriginData\Other"
OUT_DIR = os.path.normpath(os.path.join(
    HERE, "..", "..", "WindingTale2", "Assets", "Resources", "Animations", "exploration"))

FIRST, LAST = 4, 11            # 040-04 .. 040-11 -> explosion_00 .. explosion_07
CANVAS = 72                    # widest frame (040-11 is 72 x 72)
UP_SCALE = 1.4                 # height above the mid layer vs. a circular cross-section
DOWN_SCALE = 0.8               # depth below it (flames lean upward)
LIFT = [0, 0, 0, 0, 1, 2, 3, 4]  # per-frame rise, voxels (embers drift up)
# Centre fill: from frame 1 on the sprite is a hollow ring, which reads as an empty hole in
# 3D. Pixels of the ring are mirrored inward across its inner edge, kept with a clumpy
# probability DENSITY[k] * (0.3 + 0.7 exp(-depth / FILL_FALLOFF)): dense near the ring, sparse embers in the middle.
DENSITY = [0.0, 0.5, 0.55, 0.55, 0.5, 0.45, 0.4, 0.35]
FILL_FALLOFF = 9.0             # px: how fast the fill thins out toward the centre
FILL_SEED = 1234
# The opening (user 2026-09-28): frame 0 is a closed fireball at ~80% of the widest frame
# (040-04 is 51 px against 040-08's 72, so it is enlarged by FRAME_SCALE); frame 1 is still
# almost closed, only just opening: 040-05's hollow middle is filled with the white-hot
# fireball of 040-04, except a small opening where the hole is deeper than OPEN_DEPTH px.
FRAME_SCALE = [1.13, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0]
OPEN_DEPTH = 3.0


def close_opening(rgb, fireball):
    """Frame 1: fill the hollow middle with the fireball, leaving a small opening."""
    mask = rgb.any(axis=2)
    hole = ndimage.binary_fill_holes(mask) & ~mask
    depth = ndimage.distance_transform_edt(~mask)
    fill = hole & (depth <= OPEN_DEPTH) & fireball.any(axis=2)
    out = rgb.copy()
    out[fill] = fireball[fill]
    return out


def scale_frame(rgb, s):
    """Enlarge a frame about the canvas centre by s (nearest pixel)."""
    if abs(s - 1.0) < 1e-6:
        return rgb
    c = (CANVAS - 1) / 2.0
    yy, xx = np.mgrid[0:CANVAS, 0:CANVAS]
    sy = np.rint(c + (yy - c) / s).astype(int)
    sx = np.rint(c + (xx - c) / s).astype(int)
    ok = (sy >= 0) & (sy < CANVAS) & (sx >= 0) & (sx < CANVAS)
    out = np.zeros_like(rgb)
    out[ok] = rgb[sy[ok], sx[ok]]
    return out


def fill_centre(rgb, k):
    """Add mirrored ring pixels into the hollow middle of frame k."""
    if DENSITY[k] <= 0:
        return rgb
    mask = rgb.reshape(-1, 3).any(axis=1).reshape(CANVAS, CANVAS)
    closed = ndimage.binary_closing(mask, iterations=4)
    hull = ndimage.binary_fill_holes(ndimage.binary_dilation(closed, iterations=2))
    # the centre is always "inside" even when the ring has gaps
    yy, xx = np.mgrid[0:CANVAS, 0:CANVAS]
    c = (CANVAS - 1) / 2.0
    r = np.hypot(yy - c, xx - c)
    hull |= r < CANVAS * 0.30
    hole = hull & ~mask
    depth, (iy, ix) = ndimage.distance_transform_edt(~mask, return_indices=True)
    rng = np.random.default_rng(FILL_SEED + k)
    noise = ndimage.gaussian_filter(rng.random((CANVAS, CANVAS)), 1.5)
    noise = (noise - noise.min()) / max(noise.max() - noise.min(), 1e-6)
    keep = hole & (noise < DENSITY[k] * (0.3 + 0.7 * np.exp(-depth / FILL_FALLOFF)))
    out = rgb.copy()
    ys, xs = np.nonzero(keep)
    ny, nx = iy[ys, xs], ix[ys, xs]
    my = np.clip(2 * ny - ys, 0, CANVAS - 1)   # mirror across the ring's inner edge
    mx = np.clip(2 * nx - xs, 0, CANVAS - 1)
    src_ok = mask[my, mx]
    sy = np.where(src_ok, my, ny)
    sx = np.where(src_ok, mx, nx)
    out[ys, xs] = rgb[sy, sx]
    return out


def load(i, src_dir):
    rgb = np.asarray(Image.open(os.path.join(src_dir, "040-%02d.bmp" % i)).convert("RGB"))
    h, w, _ = rgb.shape
    canvas = np.zeros((CANVAS, CANVAS, 3), np.uint8)
    oy, ox = (CANVAS - h) // 2, (CANVAS - w) // 2
    canvas[oy:oy + h, ox:ox + w] = rgb
    return canvas


def inflate(mask):
    """Solve laplace(h) = -2 on the mask (Dirichlet 0 outside); return sqrt(h) - 0.5."""
    ys, xs = np.nonzero(mask)
    n = len(ys)
    half = np.zeros(mask.shape, np.float64)
    if n == 0:
        return half
    index = -np.ones(mask.shape, np.int64)
    index[ys, xs] = np.arange(n)
    rows, cols, vals = [np.arange(n)], [np.arange(n)], [np.full(n, 4.0)]
    h, w = mask.shape
    for dy, dx in ((0, 1), (0, -1), (1, 0), (-1, 0)):
        ny, nx = ys + dy, xs + dx
        ok = (ny >= 0) & (ny < h) & (nx >= 0) & (nx < w)
        nb = np.full(n, -1)
        nb[ok] = index[ny[ok], nx[ok]]
        inside = nb >= 0
        rows.append(np.arange(n)[inside])
        cols.append(nb[inside])
        vals.append(np.full(inside.sum(), -1.0))
    a = coo_matrix((np.concatenate(vals), (np.concatenate(rows), np.concatenate(cols))),
                   shape=(n, n)).tocsr()
    sol = spsolve(a, np.full(n, 2.0))
    half[ys, xs] = np.maximum(np.sqrt(np.maximum(sol, 0.0)) - 0.5, 0.0)
    return half


def build_voxels(rgb, lift):
    """{(x, y, z): colour key}; x = image column, z = -image row, y up."""
    mask = rgb.reshape(-1, 3).any(axis=1).reshape(CANVAS, CANVAS)
    half = inflate(mask)
    voxels = {}
    c = CANVAS // 2
    for r, col in zip(*np.nonzero(mask)):
        up = int(round(half[r, col] * UP_SCALE))
        down = int(round(half[r, col] * DOWN_SCALE))
        key = "%02x%02x%02x" % tuple(int(v) for v in rgb[r, col])
        x, z = int(col) - c, c - 1 - int(r)
        for y in range(-down, up + 1):
            voxels[(x, y + lift, z)] = key
    return voxels


# face: neighbour offset, 4 corners (counter-clockwise seen from outside)
FACES = [
    ((1, 0, 0), [(1, 0, 0), (1, 1, 0), (1, 1, 1), (1, 0, 1)]),
    ((-1, 0, 0), [(0, 0, 0), (0, 0, 1), (0, 1, 1), (0, 1, 0)]),
    ((0, 1, 0), [(0, 1, 0), (0, 1, 1), (1, 1, 1), (1, 1, 0)]),
    ((0, -1, 0), [(0, 0, 0), (1, 0, 0), (1, 0, 1), (0, 0, 1)]),
    ((0, 0, 1), [(0, 0, 1), (1, 0, 1), (1, 1, 1), (0, 1, 1)]),
    ((0, 0, -1), [(0, 0, 0), (0, 1, 0), (1, 1, 0), (1, 0, 0)]),
]


def write_frame(name, voxels, out_dir):
    by_key = {}
    for p, key in voxels.items():
        by_key.setdefault(key, []).append(p)

    vindex, verts, groups = {}, [], []
    for key in sorted(by_key, key=lambda k: -sum(bytes.fromhex(k))):  # hottest first
        faces = []
        for (x, y, z) in by_key[key]:
            for (dx, dy, dz), corners in FACES:
                if (x + dx, y + dy, z + dz) in voxels:
                    continue
                f = []
                for cx, cy, cz in corners:
                    # y centred on the voxel so the mid layer straddles y = 0
                    v = (x + cx, y + cy - 0.5, z + cz)
                    if v not in vindex:
                        vindex[v] = len(verts) + 1
                        verts.append(v)
                    f.append(vindex[v])
                faces.append(f)
        groups.append((key, faces))

    with open(os.path.join(out_dir, name + ".mtl"), "w", newline="\n") as m:
        for key, _ in groups:
            r, g, b = bytes.fromhex(key)
            m.write("newmtl m_%s\nKd %.4f %.4f %.4f\nKa 0 0 0\nKs 0 0 0\nillum 1\n\n"
                    % (key, r / 255, g / 255, b / 255))
    with open(os.path.join(out_dir, name + ".obj"), "w", newline="\n") as o:
        o.write("# %s  death explosion, 1 unit = 1 original pixel (death_explosion_to_obj.py)\n"
                % name)
        o.write("mtllib %s.mtl\no %s\n" % (name, name))
        for v in verts:
            o.write("v %g %g %g\n" % v)
        for key, faces in groups:
            o.write("g m_%s\nusemtl m_%s\n" % (key, key))
            for f in faces:
                o.write("f %d %d %d %d\n" % tuple(f))
    return len(verts), sum(len(f) for _, f in groups), len(groups)


def main():
    src_dir = sys.argv[1] if len(sys.argv) > 1 else SRC_DIR
    out_dir = sys.argv[2] if len(sys.argv) > 2 else OUT_DIR
    os.makedirs(out_dir, exist_ok=True)
    fireball = load(FIRST, src_dir)
    for k, i in enumerate(range(FIRST, LAST + 1)):
        rgb = load(i, src_dir)
        rgb = close_opening(rgb, fireball) if k == 1 else fill_centre(rgb, k)
        vox = build_voxels(scale_frame(rgb, FRAME_SCALE[k]), LIFT[k])
        name = "explosion_%02d" % k
        nv, nf, nm = write_frame(name, vox, out_dir)
        ys = [p[1] for p in vox]
        print("%s <- 040-%02d: %d voxels, y %d..%d, %d verts, %d faces, %d colours"
              % (name, i, len(vox), min(ys), max(ys), nv, nf, nm))


if __name__ == "__main__":
    main()
