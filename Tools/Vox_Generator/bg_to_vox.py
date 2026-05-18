"""
bg_to_vox.py
------------

Turn a 2D battle-background PNG (Resources/Original/BG/<id>.png) into a
3D voxel terrain (Resources/Remastered/BG/BG_<id>.vox). The top portion
of the source (sky + far mountains) is cropped out; the remaining
ground/water area is treated as a quasi top-down map.

Pipeline
~~~~~~~~

1. **Crop**: drop the top ``--top-skip`` fraction of the image (default 0.32)
   so only the ground/water/sand region remains.
2. **Resize**: scale down so the cropped image fits ``--width`` voxels wide
   (default 200, keep aspect ratio).
3. **Quantize**: collapse the resized image to <= 255 colours via PIL's
   median-cut quantiser, so we stay inside the MagicaVoxel palette limit
   without banding.
4. **Classify**: every pixel is tagged ``water | sand | grass | dirt |
   other`` from its RGB; each tag has a base column height. Small random
   noise (+/-1 voxel) is added for organic variation.
5. **Build voxels**: each (x, y) of the resized image becomes a full
   column from z=0 up to its height. Image bottom row maps to vox Y=0
   (foreground in MagicaVoxel / OBJ +Y after --y-up = Unity forward).

Coordinate system follows the rest of the pipeline:
  +X = right         (matches image X)
  +Y = into scene    (image bottom -> Y=0 foreground)
  +Z = up            (terrain rises here)

After running through vox_to_obj_exporter.py with --y-up, the model
stands flat on the Y=0 plane in Unity, ready to drop characters on top.
"""

import argparse
import math
import os
import random
import struct
import sys
from PIL import Image


SRC_DIR = r'D:\SourceCode\Git\toneyisnow\windingtale\Resources\Original\BG'
OUT_DIR = r'D:\SourceCode\Git\toneyisnow\windingtale\Resources\Remastered\BG'

DEFAULT_TOP_SKIP_FRAC = 0.32
DEFAULT_TARGET_WIDTH  = 200
DEFAULT_HEIGHT_NOISE  = 1     # +/- voxels of random per-column variation
DEFAULT_SEED          = 42

# Base column heights per classified terrain type
TYPE_HEIGHT = {
    'water': 1,
    'sand':  3,
    'grass': 4,
    'dirt':  4,
    'other': 2,
}


# --------------------------------------------------------------------------- #
# classification                                                              #
# --------------------------------------------------------------------------- #

def classify_pixel(r, g, b):
    """Coarse terrain classifier from an RGB triple."""
    # Water = clearly blue
    if b >= 110 and b > r + 10 and b > g - 5:
        return 'water'
    # Sand = warm, light, red ~ green > blue
    if r > 150 and g > 120 and b < g and (r + g) > 280:
        return 'sand'
    # Grass = green wins
    if g > r and g > b and g > 70:
        return 'grass'
    # Dirt = red wins over green, not too dark
    if r > g and g > b and (r + g + b) > 200:
        return 'dirt'
    return 'other'


# --------------------------------------------------------------------------- #
# build                                                                       #
# --------------------------------------------------------------------------- #

def make_bg_vox(image_path, out_path,
                target_width=DEFAULT_TARGET_WIDTH,
                top_skip_frac=DEFAULT_TOP_SKIP_FRAC,
                height_noise=DEFAULT_HEIGHT_NOISE,
                seed=DEFAULT_SEED):
    rnd = random.Random(seed)
    im = Image.open(image_path).convert('RGB')
    w, h = im.size

    # 1. crop off sky + far mountains
    top_skip = int(h * top_skip_frac)
    cropped = im.crop((0, top_skip, w, h))
    cw, ch = cropped.size

    # 2. scale to target width preserving aspect ratio
    scale = target_width / cw
    nw = target_width
    nh = max(1, round(ch * scale))
    if nw > 256 or nh > 256:
        raise SystemExit(f'target dims {nw}x{nh} exceed MagicaVoxel single-model'
                         f' limit (256). Lower --width.')
    resized = cropped.resize((nw, nh), Image.LANCZOS)

    # 3. quantize to a 255-colour palette (slot 0 reserved by MagicaVoxel)
    quantized = resized.quantize(colors=255, method=Image.MEDIANCUT,
                                 dither=Image.NONE).convert('RGB')
    qpx = quantized.load()

    # 4. classify + heights
    height_at = {}
    color_at  = {}
    for ix in range(nw):
        for iy in range(nh):
            r, g, b = qpx[ix, iy]
            kind = classify_pixel(r, g, b)
            base = TYPE_HEIGHT[kind]
            jitter = rnd.randint(-height_noise, height_noise) if height_noise else 0
            col_h = max(1, base + jitter)
            world_x = ix
            world_y = (nh - 1) - iy        # image bottom -> world Y=0
            height_at[(world_x, world_y)] = col_h
            color_at [(world_x, world_y)] = (r, g, b)

    sx = nw
    sy = nh
    sz = max(height_at.values()) + 0      # max column height
    print(f'source {w}x{h} -> crop {cw}x{ch} -> scaled {nw}x{nh}')
    print(f'output VOX: {sx} x {sy} x {sz}  (palette <= 255)')

    # 5. emit filled columns
    voxel_color = {}
    for (x, y), top_h in height_at.items():
        c = color_at[(x, y)]
        for z in range(top_h):
            voxel_color[(x, y, z)] = c

    write_vox(voxel_color, sx, sy, sz, out_path)
    return len(voxel_color), sx, sy, sz


# --------------------------------------------------------------------------- #
# .vox writer                                                                 #
# --------------------------------------------------------------------------- #

def write_vox(voxel_color, sx, sy, sz, out_path):
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
        # palette full -> snap to nearest existing (shouldn't fire if quantized)
        best_i, best_d = 1, 10 ** 12
        for i, p in enumerate(palette):
            d = (p[0] - key[0]) ** 2 + (p[1] - key[1]) ** 2 + (p[2] - key[2]) ** 2
            if d < best_d:
                best_d, best_i = d, i + 1
        return best_i

    voxels = [(x, y, z, palette_index(c)) for (x, y, z), c in voxel_color.items()]

    def chunk(cid, content, children=b''):
        return cid + struct.pack('<ii', len(content), len(children)) + content + children

    size_chunk = chunk(b'SIZE', struct.pack('<iii', sx, sy, sz))

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

    main = chunk(b'MAIN', b'', size_chunk + xyzi_chunk + rgba_chunk)
    blob = b'VOX ' + struct.pack('<i', 150) + main
    with open(out_path, 'wb') as f:
        f.write(blob)


# --------------------------------------------------------------------------- #
# CLI                                                                         #
# --------------------------------------------------------------------------- #

def main(argv=None):
    p = argparse.ArgumentParser(
        prog='bg_to_vox.py',
        description='Convert a battle background PNG into a 3D voxel terrain.')
    p.add_argument('id', help='background id, e.g. "07" -> 07.png')
    p.add_argument('-w', '--width', type=int, default=DEFAULT_TARGET_WIDTH,
                   help=f'target VOX X dimension (default {DEFAULT_TARGET_WIDTH}, max 256)')
    p.add_argument('--top-skip', type=float, default=DEFAULT_TOP_SKIP_FRAC,
                   help=f'fraction of source height to crop from the top '
                        f'(default {DEFAULT_TOP_SKIP_FRAC})')
    p.add_argument('--noise', type=int, default=DEFAULT_HEIGHT_NOISE,
                   help=f'per-column random height variation in voxels '
                        f'(default {DEFAULT_HEIGHT_NOISE}, 0 disables)')
    p.add_argument('--seed', type=int, default=DEFAULT_SEED,
                   help=f'random seed for height noise (default {DEFAULT_SEED})')
    p.add_argument('--src-dir', default=SRC_DIR)
    p.add_argument('--out-dir', default=OUT_DIR)
    args = p.parse_args(argv if argv is not None else sys.argv[1:])

    bg_id = args.id.zfill(2)
    src = os.path.join(args.src_dir, f'{bg_id}.png')
    if not os.path.isfile(src):
        raise SystemExit(f'source not found: {src}')
    os.makedirs(args.out_dir, exist_ok=True)
    dst = os.path.join(args.out_dir, f'BG_{bg_id}.vox')
    n_vox, sx, sy, sz = make_bg_vox(
        src, dst,
        target_width=args.width,
        top_skip_frac=args.top_skip,
        height_noise=args.noise,
        seed=args.seed)
    print(f'wrote {dst}: {n_vox} voxels, dims {sx}x{sy}x{sz}')


if __name__ == '__main__':
    main()
