"""
tai_to_vox.py
-------------

Convert a side-view oblique photo of a cylindrical battle-platform
(``Tai-XX.png``, e.g. grass-on-top / dirt-on-side) into a 3D voxel
cylinder ``.vox`` file.

Usage
~~~~~

    python tai_to_vox.py <id>             # e.g.  python tai_to_vox.py 01
    python tai_to_vox.py <id> -d 40       # custom diameter
    python tai_to_vox.py <id> -H 8        # custom side-wall height

The script reads ``Resources/Original/Tais/Tai-<id>.png`` and writes
``Resources/Remastered/Tais/Tai_<id>.vox``.

Algorithm (concise)
~~~~~~~~~~~~~~~~~~~

1. **Silhouette**: The source PNG uses pure black (R+G+B < BLACK_THRESH) as
   the "outside the platform" colour. Per-row scan finds the platform's
   X extent and the global Y bounds (y_top, y_bot).

2. **Top vs side split**: We classify every pixel as "green-ish" (grass top)
   or "brown-ish" (dirt side) and find the first row from the top where
   brown dominates green. That's ``y_split``. Rows ``[y_top, y_split)`` are
   the top-face ellipse, rows ``[y_split, y_bot]`` are the side rim.

3. **Top face**: At Z = ``H-1`` we fill a circle of diameter ``D`` (the
   target voxel diameter). For each ``(vx, vy)`` inside the circle we map
   to the image's top region (un-squishing the ellipse to a circle):
        im_x = vx * img_w / D
        im_y = y_split - 1 - vy * top_h_image / D     # vy=0 = front
   so that the visible image-top maps to the model's BACK row and
   image-bottom-of-top-region maps to the FRONT row of the top face.

4. **Side wall + fill**: All voxels in the cylinder below the top face are
   filled. Surface voxels (those on the circle's perimeter) sample colour
   from the source image's side region using the voxel's X position:
        im_x = vx * img_w / D
        im_y = y_split + (1 - (vz + 0.5)/top_z) * side_h_image
   Interior voxels (not visible) get the average brown fallback colour.

5. **MagicaVoxel-compatible .vox**: SIZE / XYZI / RGBA chunks, palette
   built incrementally, capped at 255 colours.

Coordinate convention matches png4_to_vox.py / the rest of the pipeline:
+X = right, +Y = into-the-scene (depth), +Z = up. The top face sits at
the highest Z. After --y-up in vox_to_obj_exporter the model lands
flat on Y=0 in Unity.
"""

import math
import os
import struct
import sys
from PIL import Image


# --------------------------------------------------------------------------- #
# defaults / tunables                                                         #
# --------------------------------------------------------------------------- #

SRC_DIR = r'D:\SourceCode\Git\toneyisnow\windingtale\Resources\Original\Tais'
OUT_DIR = r'D:\SourceCode\Git\toneyisnow\windingtale\Resources\Remastered\Tais'

BLACK_THRESH      = 20      # R+G+B below this -> "outside the platform"
TARGET_DIAMETER   = 64      # voxel diameter of the output cylinder
TARGET_SIDE_H     = 4       # side-wall height in voxels (final VOX Z = this+1)
GREEN_RED_MARGIN  = 5       # green > red + this  -> classify as grass
BROWN_BRIGHTNESS  = 50      # required min R+G for a brown classification
GREEN_VS_BROWN_RATIO = 3    # find the y_split where green_count <= brown/RATIO


# --------------------------------------------------------------------------- #
# helpers                                                                     #
# --------------------------------------------------------------------------- #

def is_inside(c):
    return c[0] + c[1] + c[2] > BLACK_THRESH


def classify(c):
    r, g, b = c[0], c[1], c[2]
    if g > r + GREEN_RED_MARGIN:
        return 'green'
    if r > g + GREEN_RED_MARGIN and r + g > BROWN_BRIGHTNESS:
        return 'brown'
    return 'other'


def nearest_inside(px, x, y, w, h, max_radius=6):
    """Spiral search for the nearest opaque-platform pixel around (x, y)."""
    if 0 <= x < w and 0 <= y < h and is_inside(px[x, y][:3]):
        return px[x, y][:3]
    for r in range(1, max_radius + 1):
        for dx in range(-r, r + 1):
            for dy in range(-r, r + 1):
                if abs(dx) != r and abs(dy) != r:
                    continue
                nx, ny = x + dx, y + dy
                if 0 <= nx < w and 0 <= ny < h and is_inside(px[nx, ny][:3]):
                    return px[nx, ny][:3]
    return None


def find_split_y(px, img_w, img_h):
    """Find the Y row where the top-face (grass) yields to side-rim (dirt)."""
    g_per_row = [0] * img_h
    b_per_row = [0] * img_h
    for y in range(img_h):
        for x in range(img_w):
            c = px[x, y][:3]
            if not is_inside(c):
                continue
            k = classify(c)
            if k == 'green':
                g_per_row[y] += 1
            elif k == 'brown':
                b_per_row[y] += 1
    for y in range(img_h):
        if b_per_row[y] > 0 and g_per_row[y] * GREEN_VS_BROWN_RATIO <= b_per_row[y]:
            return y
    return img_h * 3 // 4   # safe fallback


# --------------------------------------------------------------------------- #
# core                                                                        #
# --------------------------------------------------------------------------- #

def make_tai_vox(image_path, out_path,
                 target_diameter=TARGET_DIAMETER,
                 target_side_h=TARGET_SIDE_H):
    im = Image.open(image_path).convert('RGBA')
    img_w, img_h = im.size
    px = im.load()

    # 1. silhouette bounds
    rows_extent = []
    for y in range(img_h):
        xs = [x for x in range(img_w) if is_inside(px[x, y][:3])]
        rows_extent.append((min(xs), max(xs)) if xs else None)
    if not any(rows_extent):
        raise SystemExit('image has no platform pixels (entirely black/transparent)')
    y_top   = next(y for y, e in enumerate(rows_extent) if e)
    y_bot   = max(y for y in range(img_h) if rows_extent[y])
    img_diameter_px = max(e[1] - e[0] + 1 for e in rows_extent if e)

    y_split = find_split_y(px, img_w, img_h)

    top_h_image  = max(1, y_split - y_top)
    side_h_image = max(1, y_bot - y_split + 1)
    print(f'image {img_w}x{img_h}: platform y={y_top}..{y_bot}, '
          f'split={y_split} (top {top_h_image}px, side {side_h_image}px), '
          f'platform max width {img_diameter_px}px')

    # 2. average brown for interior + fallback
    side_pixels = []
    for y in range(y_split, y_bot + 1):
        e = rows_extent[y]
        if not e:
            continue
        for x in range(e[0], e[1] + 1):
            c = px[x, y][:3]
            if is_inside(c) and classify(c) == 'brown':
                side_pixels.append(c)
    if not side_pixels:
        side_pixels = [(90, 60, 30)]
    avg_brown = tuple(int(sum(c[i] for c in side_pixels) / len(side_pixels))
                      for i in range(3))

    # 3. output dims
    D = target_diameter
    H = target_side_h + 1     # +1 layer for the top face
    print(f'output VOX: {D}x{D}x{H}  (side wall z=0..{H-2}, top face z={H-1})')

    cx = cy = (D - 1) / 2.0
    radius = D / 2.0           # circle test against voxel centre (vx+0.5, vy+0.5)
    top_z  = H - 1

    voxel_color = {}

    # 4a. TOP FACE -- sample from the image's top region, un-squished
    #     vy=0 (model FRONT) <- image's bottom-of-top-region (y_split - 1)
    #     vy=D-1 (model BACK) <- image's top of region (y_top)
    for vx in range(D):
        for vy in range(D):
            dx = vx + 0.5 - cx
            dy = vy + 0.5 - cy
            if dx * dx + dy * dy > radius * radius:
                continue
            im_x = int((vx + 0.5) * img_w / D)
            # invert vy so the model's front matches the image's near-edge of top
            im_y = int(y_split - 1 - (vy + 0.5) * top_h_image / D)
            im_x = max(0, min(img_w - 1, im_x))
            im_y = max(y_top, min(y_split - 1, im_y))
            c = px[im_x, im_y][:3]
            if not is_inside(c):
                c = nearest_inside(px, im_x, im_y, img_w, img_h) or avg_brown
            voxel_color[(vx, vy, top_z)] = c

    # 4b. SIDE WALL + INTERIOR FILL
    for vz in range(top_z):
        # bottom of cylinder (vz=0)  <-> image's bottom row (y_bot)
        # top    of wall    (vz=top_z-1) <-> image's y_split
        rim_frac = 1.0 - (vz + 0.5) / top_z
        im_y = int(y_split + rim_frac * side_h_image)
        im_y = max(y_split, min(y_bot, im_y))

        for vx in range(D):
            for vy in range(D):
                dx = vx + 0.5 - cx
                dy = vy + 0.5 - cy
                d2 = dx * dx + dy * dy
                if d2 > radius * radius:
                    continue
                # surface voxel = at the very edge of the circle (< 1.0 from rim)
                edge_dist = radius - math.sqrt(d2)
                if edge_dist < 1.0:
                    im_x = int((vx + 0.5) * img_w / D)
                    im_x = max(0, min(img_w - 1, im_x))
                    c = px[im_x, im_y][:3]
                    if not is_inside(c) or classify(c) == 'green':
                        c = avg_brown
                else:
                    c = avg_brown  # interior, never visible
                voxel_color[(vx, vy, vz)] = c

    # 5. palette + write .vox
    palette = []
    color_to_idx = {}

    def palette_index(c):
        key = (c[0], c[1], c[2], 255)
        idx = color_to_idx.get(key)
        if idx is not None:
            return idx
        if len(palette) < 255:
            palette.append(key)
            color_to_idx[key] = len(palette)
            return len(palette)
        # palette full: snap to nearest existing colour
        best_i, best_d = 1, 10 ** 12
        for i, p in enumerate(palette):
            d = (p[0] - key[0]) ** 2 + (p[1] - key[1]) ** 2 + (p[2] - key[2]) ** 2
            if d < best_d:
                best_d, best_i = d, i + 1
        return best_i

    voxels = [(x, y, z, palette_index(c)) for (x, y, z), c in voxel_color.items()]

    def chunk(cid, content, children=b''):
        return cid + struct.pack('<ii', len(content), len(children)) + content + children

    size_chunk = chunk(b'SIZE', struct.pack('<iii', D, D, H))

    xyzi_body = bytearray(struct.pack('<i', len(voxels)))
    for (x, y, z, c) in voxels:
        xyzi_body += bytes([x, y, z, c])
    xyzi_chunk = chunk(b'XYZI', bytes(xyzi_body))

    rgba_body = bytearray()
    for i in range(256):
        if i < len(palette):
            r, g, b, _ = palette[i]
        else:
            r, g, b = 0, 0, 0
        rgba_body += bytes([r, g, b, 255])
    rgba_chunk = chunk(b'RGBA', bytes(rgba_body))

    main_chunk = chunk(b'MAIN', b'', size_chunk + xyzi_chunk + rgba_chunk)
    blob = b'VOX ' + struct.pack('<i', 150) + main_chunk
    with open(out_path, 'wb') as f:
        f.write(blob)

    return len(voxels), len(palette), D, H


# --------------------------------------------------------------------------- #
# CLI                                                                         #
# --------------------------------------------------------------------------- #

def _parse_args(argv):
    import argparse
    p = argparse.ArgumentParser(
        prog='tai_to_vox.py',
        description='Convert Tai-XX.png (oblique cylinder photo) into a 3D voxel platform.')
    p.add_argument('id', help='Tai id, e.g. "01" -> Tai-01.png')
    p.add_argument('-d', '--diameter', type=int, default=TARGET_DIAMETER,
                   help=f'voxel diameter of the cylinder (default {TARGET_DIAMETER})')
    p.add_argument('-H', '--height', type=int, default=TARGET_SIDE_H,
                   help=f'side-wall height in voxels, top face adds +1 '
                        f'(default {TARGET_SIDE_H})')
    p.add_argument('--src-dir', default=SRC_DIR,
                   help='override source folder (default: %(default)s)')
    p.add_argument('--out-dir', default=OUT_DIR,
                   help='override output folder (default: %(default)s)')
    return p.parse_args(argv)


def main(argv=None):
    args = _parse_args(argv if argv is not None else sys.argv[1:])
    tai_id = args.id.zfill(2)
    src = os.path.join(args.src_dir, f'Tai-{tai_id}.png')
    if not os.path.isfile(src):
        raise SystemExit(f'source not found: {src}')
    os.makedirs(args.out_dir, exist_ok=True)
    dst = os.path.join(args.out_dir, f'Tai_{tai_id}.vox')
    n_vox, n_col, D, H = make_tai_vox(src, dst,
                                      target_diameter=args.diameter,
                                      target_side_h=args.height)
    print(f'wrote {dst}: {n_vox} voxels, {n_col} palette colors, dims {D}x{D}x{H}')


if __name__ == '__main__':
    main()
