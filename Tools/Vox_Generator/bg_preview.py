"""Render a backdrop the way the battle camera sees it.

``vox_preview.py`` shows a model from six flat angles, which is what you want
when you are checking that a house is house-shaped. A backdrop is not a shape,
though -- it is a composition, and the only question about it is what the player
sees from the one camera that ever looks at it. So this puts that camera where
the battle scene puts it and takes the shot, with the two fighter sprites boxed
in so you can see what they will be read against.

    python bg_preview.py ../../Resources/Remastered/BG/BG_03.vox
    python bg_preview.py BG_04.vox -o out/ --no-marks

The camera comes from the prefab transform in GameBattleScene.unity, worked back
into the model's own voxel coordinates: standing at (109, 0, 62), looking up +Y,
tilted about 23 degrees down, 60 degrees of vertical field. Note the frame is
mirrored against the model -- screen right is -X -- so a thing at a large X
appears on the LEFT of the picture.

Every voxel is splatted as a square the size it subtends, through a z-buffer.
Both halves matter: without the square the near ground is a dot screen you can
see the sky through, and without the z-buffer a far voxel's centre lands inside
a near voxel's square and punches a hole in it.
"""

import argparse
import math
import os
import sys

import numpy as np
from PIL import Image, ImageDraw

_HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, os.path.join(_HERE, '..', 'MapPipeline'))

import voxlib  # noqa: E402  (needs the path above)


CAM = (109.0, 0.0, 62.0)           # the battle camera, in backdrop voxels
PITCH = math.radians(23.0)         # how far it looks down
VFOV = math.radians(60.0)
OUT_W, OUT_H = 960, 540
SS = 2                             # supersample, then box-filter down
SKY = (120, 170, 220)

# Where the two fighter sprites hang, and how tall they are: they float well
# above the terrain, so the backdrop is scenery behind them, not ground they
# stand on. Boxed in the picture as "L" (local) and "F" (foreign).
FIGHTERS = ((112, 38, 45, 'L'), (118, 38, 45, 'F'))
FIGHTER_H = 16


def project(p, W, H):
    d = p - np.array(CAM)
    r = np.array([-1.0, 0.0, 0.0])
    up = np.array([0.0, math.sin(PITCH), math.cos(PITCH)])
    f = np.array([0.0, math.cos(PITCH), -math.sin(PITCH)])
    a, b, c = d @ r, d @ up, d @ f
    ty = math.tan(VFOV / 2)
    tx = ty * W / H
    sx = (W / 2) * (1 + (a / np.maximum(c, 1e-6)) / tx)
    sy = (H / 2) * (1 - (b / np.maximum(c, 1e-6)) / ty)
    return sx, sy, c, ty


def render(path, out, marks=True):
    W, H = OUT_W * SS, OUT_H * SS
    m = voxlib.read_vox(path)
    v = np.asarray(m.voxels, np.int32)
    # Drop everything buried: only voxels with an empty neighbour have a face,
    # and that is what the OBJ exporter meshes too. Splatting the interior as
    # well lets a far soil voxel land between two nearer grass ones and stripe
    # the whole field brown.
    dense = np.zeros(m.size, bool)
    dense[v[:, 0], v[:, 1], v[:, 2]] = True
    # Only faces the camera could see: the -Y face, the top, and the two sides.
    # The underside and the +Y face are never visible from in front, and
    # splatting them lets the soil under the field paint over the field.
    shown = np.zeros(m.size, bool)
    for ax, lo in ((1, True), (2, False), (0, True), (0, False)):
        pad = [(0, 0)] * 3
        pad[ax] = (1, 0) if lo else (0, 1)
        sh = np.pad(dense, pad, constant_values=False)
        sl = [slice(None)] * 3
        sl[ax] = slice(0, m.size[ax]) if lo else slice(1, m.size[ax] + 1)
        shown |= ~sh[tuple(sl)]
    inner = ~shown
    keep_surface = dense & ~inner
    v = v[keep_surface[v[:, 0], v[:, 1], v[:, 2]]].astype(np.float64)
    pal = np.array([c[:3] for c in m.palette], np.uint8)
    cols = pal[v[:, 3].astype(int) - 1]

    sx, sy, c, ty = project(v[:, :3] + 0.5, W, H)
    keep = c > 1.0
    sx, sy, c, cols = sx[keep], sy[keep], c[keep], cols[keep]

    # how many pixels one voxel spans at that depth
    span = (H / 2) / (ty * c)
    rad = np.clip(np.ceil(span / 2).astype(int), 0, 60)

    order = np.argsort(-c)                    # far first
    sx, sy, cols, rad = sx[order], sy[order], cols[order], rad[order]
    ix, iy = np.round(sx).astype(int), np.round(sy).astype(int)

    img = np.empty((H, W, 3), np.uint8)
    img[:, :] = SKY
    depth = np.full(H * W, np.inf)
    flat = img.reshape(-1, 3)

    def paint(px, py, pc, pz):
        ok = (px >= 0) & (px < W) & (py >= 0) & (py < H)
        px, py, pc, pz = px[ok], py[ok], pc[ok], pz[ok]
        at = py * W + px
        nearer = pz < depth[at]
        at, pc, pz = at[nearer], pc[nearer], pz[nearer]
        depth[at] = pz          # sorted far to near, so the last write is nearest
        flat[at] = pc

    # A z-buffer, not just painter's order: a voxel is splatted as a square, and
    # without one a far voxel's centre lands in the middle of a near voxel's
    # square and punches a hole in it -- which is how the grass behind chapter
    # 05's cathedral ended up striped across its walls.
    for k in range(rad.max(), -1, -1):
        sel = rad >= k
        if not sel.any():
            continue
        bx, by, bc, bz = ix[sel], iy[sel], cols[sel], c[sel]
        offs = ([(dx, dy) for dx in range(-k, k + 1) for dy in (-k, k)] +
                [(dx, dy) for dx in (-k, k) for dy in range(-k + 1, k)]
                if k else [(0, 0)])
        for dx, dy in offs:
            paint(bx + dx, by + dy, bc, bz)

    # patch the pinholes the cull leaves between splats
    sky = np.all(img == np.array(SKY, np.uint8), axis=2)
    for dy, dx in ((1, 0), (-1, 0), (0, 1), (0, -1)):
        nb = np.roll(np.roll(img, dy, 0), dx, 1)
        nbs = np.roll(np.roll(sky, dy, 0), dx, 1)
        hole = sky & ~nbs
        hole[:4] = hole[-4:] = False
        img[hole] = nb[hole]
        sky &= ~hole
    im = Image.fromarray(img).resize((OUT_W, OUT_H), Image.BOX)
    if marks:
        dr = ImageDraw.Draw(im)
        for (fx, fy, fz, lbl) in FIGHTERS:
            X, Y, cc, tyy = project(np.array([[fx + .5, fy + .5, fz + .5]]),
                                    OUT_W, OUT_H)
            X, Y, cc = X[0], Y[0], cc[0]
            hh = (OUT_H / 2) * (float(FIGHTER_H) / cc) / tyy
            dr.rectangle([X - hh * .35, Y - hh / 2, X + hh * .35, Y + hh / 2],
                         outline=(255, 0, 255), width=2)
            dr.text((X - 4, Y - hh / 2 - 12), lbl, fill=(255, 0, 255))
    im.save(out)
    print(out)


def main(argv=None):
    p = argparse.ArgumentParser(
        prog='bg_preview.py',
        description='Render a backdrop VOX from the battle camera.')
    p.add_argument('vox', nargs='+', help='paths to BG_<nn>.vox files')
    p.add_argument('-o', '--out-dir', default=None,
                   help='where the PNGs go (default: beside each VOX)')
    p.add_argument('--no-marks', action='store_true',
                   help='leave the fighter boxes off')
    args = p.parse_args(argv if argv is not None else sys.argv[1:])
    for path in args.vox:
        out_dir = args.out_dir or os.path.dirname(os.path.abspath(path))
        if not os.path.isdir(out_dir):
            os.makedirs(out_dir)
        out = os.path.join(out_dir,
                           os.path.basename(path).replace('.vox', '_cam.png'))
        render(path, out, marks=not args.no_marks)


if __name__ == '__main__':
    main()
