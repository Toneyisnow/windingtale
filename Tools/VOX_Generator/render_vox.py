"""Render a generated VOX from cardinal + 3/4 perspective angles for QA."""

import struct
import os
import math
from PIL import Image


DIM = 24


def read_vox(path):
    with open(path, 'rb') as f:
        data = f.read()
    off = 8
    assert data[off:off + 4] == b'MAIN'
    main_sz = struct.unpack('<i', data[off + 4:off + 8])[0]
    off += 12 + main_sz
    voxels = []
    palette = None
    while off < len(data):
        cid = data[off:off + 4]
        sz = struct.unpack('<i', data[off + 4:off + 8])[0]
        cs = struct.unpack('<i', data[off + 8:off + 12])[0]
        body = data[off + 12:off + 12 + sz]
        if cid == b'XYZI':
            n = struct.unpack('<i', body[:4])[0]
            for i in range(n):
                voxels.append(tuple(body[4 + i * 4:4 + i * 4 + 4]))
        elif cid == b'RGBA':
            palette = body
        off += 12 + sz + cs
    return voxels, palette


def rgba_of(palette, idx):
    return (palette[(idx - 1) * 4],
            palette[(idx - 1) * 4 + 1],
            palette[(idx - 1) * 4 + 2], 255)


def render_axis(voxels, palette, view):
    """Cardinal-axis reprojection (no perspective)."""
    grid = {(x, y, z): c for (x, y, z, c) in voxels}
    img = Image.new('RGBA', (DIM, DIM), (255, 255, 255, 0))
    px = img.load()
    for u in range(DIM):
        for v in range(DIM):
            z = (DIM - 1) - v
            hit = None
            if view == 'front':
                for y in range(DIM):
                    if (u, y, z) in grid:
                        hit = grid[(u, y, z)]; break
            elif view == 'back':
                x = (DIM - 1) - u
                for y in range(DIM - 1, -1, -1):
                    if (x, y, z) in grid:
                        hit = grid[(x, y, z)]; break
            elif view == 'side_lo':
                y = u
                for x in range(DIM):
                    if (x, y, z) in grid:
                        hit = grid[(x, y, z)]; break
            elif view == 'side_hi':
                y = (DIM - 1) - u
                for x in range(DIM - 1, -1, -1):
                    if (x, y, z) in grid:
                        hit = grid[(x, y, z)]; break
            if hit is not None:
                px[u, v] = rgba_of(palette, hit)
    return img


def render_perspective(voxels, palette, yaw_deg=30, pitch_deg=15, scale=14, bg=(40, 40, 40, 255)):
    """Orthographic 3/4 render with z-buffered voxel cubes + Lambert shading."""
    grid = {(x, y, z): c for (x, y, z, c) in voxels}

    cx = cy = cz = (DIM - 1) / 2.0
    yaw = math.radians(yaw_deg)
    pitch = math.radians(pitch_deg)
    cy_, sy_ = math.cos(yaw),   math.sin(yaw)
    cp_, sp_ = math.cos(pitch), math.sin(pitch)

    def rotate(dx, dy, dz):
        rx =  cy_ * dx + sy_ * dy
        ry = -sy_ * dx + cy_ * dy
        rz =  dz
        ry2 =  cp_ * ry - sp_ * rz
        rz2 =  sp_ * ry + cp_ * rz
        return rx, ry2, rz2

    # camera forward (pointing from camera into scene) is +ry after rotation,
    # i.e. world vector reached by rotating +Y.
    fx_w, fy_w, fz_w = rotate(0, 1, 0)
    # but we want "world-space camera-forward" for normal-shading, so:
    # the unit vector pointing from camera at origin into world: rotate (0,1,0)
    # back; what we already computed is the local Y axis of the rotation, fine.

    W = int(DIM * scale * 1.8)
    H = int(DIM * scale * 1.8)
    img = Image.new('RGBA', (W, H), bg)
    z_buf = [[1e9] * H for _ in range(W)]
    px = img.load()

    # surface voxels only (one missing 6-neighbour is enough)
    surface = []
    for (x, y, z, c) in voxels:
        for dx, dy, dz in ((-1, 0, 0), (1, 0, 0), (0, -1, 0), (0, 1, 0), (0, 0, -1), (0, 0, 1)):
            if (x + dx, y + dy, z + dz) not in grid:
                # accumulate outward direction
                nx = 0; ny = 0; nz = 0; cnt = 0
                for dx2, dy2, dz2 in ((-1, 0, 0), (1, 0, 0), (0, -1, 0), (0, 1, 0), (0, 0, -1), (0, 0, 1)):
                    if (x + dx2, y + dy2, z + dz2) not in grid:
                        nx += dx2; ny += dy2; nz += dz2; cnt += 1
                if cnt:
                    L = math.sqrt(nx * nx + ny * ny + nz * nz) or 1.0
                    surface.append((x, y, z, c, nx / L, ny / L, nz / L))
                break

    # paint each surface voxel as a small square at its projected centre
    light = (-0.5, -0.7, 0.6)
    Llen = math.sqrt(sum(l * l for l in light))
    light = tuple(l / Llen for l in light)

    for (x, y, z, c, nx, ny, nz) in surface:
        rx, ry, rz = rotate(x + 0.5 - cx, y + 0.5 - cy, z + 0.5 - cz)
        sx_p = int(round(rx * scale + W / 2))
        sy_p = int(round(-rz * scale + H / 2))
        depth = ry

        ldot = nx * light[0] + ny * light[1] + nz * light[2]
        shade = 0.45 + 0.55 * max(0.0, ldot)

        col = rgba_of(palette, c)
        col = (int(col[0] * shade), int(col[1] * shade), int(col[2] * shade), 255)

        s = max(1, int(round(scale * 0.55)))
        for du in range(-s, s + 1):
            for dv in range(-s, s + 1):
                u = sx_p + du
                v = sy_p + dv
                if 0 <= u < W and 0 <= v < H:
                    if depth < z_buf[u][v]:
                        z_buf[u][v] = depth
                        px[u, v] = col
    return img


def main(frame=2):
    here = os.path.dirname(os.path.abspath(__file__))
    vox = os.path.join(here, 'output', f'Icon_006_{frame:02d}.vox')
    voxels, palette = read_vox(vox)
    out_dir = os.path.join(here, 'output')
    tag = f'_f{frame:02d}'

    # 4 cardinal axis renders
    for v in ('front', 'back', 'side_lo', 'side_hi'):
        img = render_axis(voxels, palette, v)
        img.resize((192, 192), Image.NEAREST).save(
            os.path.join(out_dir, f'rendered_{v}{tag}.png'))

    # 8 perspective angles around the model at pitch 15 + an extra pitch 30 view
    angles = [(yaw, 15) for yaw in (0, 45, 90, 135, 180, 225, 270, 315)]
    angles += [(0, 35), (45, 35), (-45, -15), (0, -30)]

    tiles = []
    for yaw, pitch in angles:
        img = render_perspective(voxels, palette, yaw_deg=yaw, pitch_deg=pitch, scale=10)
        tiles.append(img)

    tw, th = tiles[0].size
    cols = 4
    rows = (len(tiles) + cols - 1) // cols
    sheet = Image.new('RGBA', (tw * cols, th * rows), (24, 24, 24, 255))
    for i, img in enumerate(tiles):
        r, c = i // cols, i % cols
        sheet.paste(img, (c * tw, r * th))
    sheet.save(os.path.join(out_dir, f'perspective_grid{tag}.png'))

    # 4-view side-by-side comparison with originals (front=f, left=f+3, back=f+6, right=f+9)
    orig_dir = os.path.join(here, 'original')
    pairs = [
        (f'Icon-006-{frame:02d}.png',     f'rendered_front{tag}.png'),
        (f'Icon-006-{3+frame:02d}.png',   f'rendered_side_lo{tag}.png'),
        (f'Icon-006-{6+frame:02d}.png',   f'rendered_back{tag}.png'),
        (f'Icon-006-{9+frame:02d}.png',   f'rendered_side_hi{tag}.png'),
    ]
    cell = 200
    sheet = Image.new('RGBA', (cell * 2 + 20, cell * 4 + 20), (60, 60, 60, 255))
    y = 10
    for orig, rend in pairs:
        a = Image.open(os.path.join(orig_dir, orig)).resize((192, 192), Image.NEAREST)
        b = Image.open(os.path.join(out_dir, rend))
        sheet.paste(a, (10, y))
        sheet.paste(b, (cell + 10, y))
        y += cell
    sheet.save(os.path.join(out_dir, f'compare_original_vs_generated{tag}.png'))

    print(f'rendered frame {frame}: {len(voxels)} voxels to {out_dir}')


if __name__ == '__main__':
    import sys
    if len(sys.argv) > 1:
        frames = [int(a) for a in sys.argv[1:]]
    else:
        frames = [1, 2]
    for f in frames:
        main(f)
