"""Render an .obj (+ .mtl + palette .png) to a shaded contact sheet.

``vox_preview.py`` splats voxels, which answers "is this model the right
shape". It cannot answer "does this model look good once a light hits it",
because it never builds the mesh and never uses a normal. That is the only
question worth asking about a rounded/smooth-shaded export, so this tool
rasterises the actual triangles with the actual vertex normals:

    python obj_preview.py ../../Resources/Remastered/Icons/001/Icon_001_01.obj
    python obj_preview.py a.obj b.obj --labels before,after -o out/cmp.png

Each input becomes one row of views; several inputs stack into a comparison
sheet, which is the point -- a rounding tweak is only ever judged against
the thing it replaced.

Shading is deliberately close to what the game does: one key directional
light, a hemisphere ambient (sky above, bounce below) and a weak rim, so a
model that reads well here reads well in the scene.
"""

import argparse
import math
import os

import numpy as np
from PIL import Image, ImageDraw

# Views are (yaw, pitch) in degrees. Yaw spins the model about its up axis,
# pitch is how far the camera looks down at it. The battle camera sits about
# 25 degrees above the horizon, so that is the default row.
VIEWS = [
    ('front', 0.0, 22.0),
    ('3/4', 35.0, 22.0),
    ('side', 90.0, 22.0),
    ('back', 180.0, 22.0),
    ('top', 20.0, 65.0),
]

BACKDROP = (40, 42, 50)
KEY_DIR = (-0.45, 0.82, 0.36)      # key light, pointing FROM the surface TO the light
KEY_COLOR = (1.0, 0.96, 0.84)      # the field scene's directional light
SKY_COLOR = (0.46, 0.52, 0.62)     # ambient from above
GROUND_COLOR = (0.28, 0.25, 0.22)  # bounce from below

# 'soft' mirrors Custom/VoxelCreature; keep the numbers in step with the
# shader's property defaults or this preview stops predicting anything.
LIGHT_WRAP = 0.45
SHADOW_COLOR = (0.55, 0.62, 0.82)
SHADOW_FILL = 0.18
RIM_COLOR = (0.72, 0.80, 1.0)
RIM_STRENGTH = 0.48
RIM_POWER = 9.0
SHEEN = 0.10
SHEEN_POWER = 14.0


def load_obj(path):
    """--> (positions, uvs, normals, tris) where tris is (n, 3, 3) of indices."""
    verts, uvs, norms, tris = [], [], [], []
    with open(path, 'r') as handle:
        for line in handle:
            parts = line.split()
            if not parts:
                continue
            tag = parts[0]
            if tag == 'v':
                verts.append([float(x) for x in parts[1:4]])
            elif tag == 'vt':
                uvs.append([float(x) for x in parts[1:3]])
            elif tag == 'vn':
                norms.append([float(x) for x in parts[1:4]])
            elif tag == 'f':
                corners = []
                for token in parts[1:]:
                    bits = (token.split('/') + ['', ''])[:3]
                    corners.append([int(b) - 1 if b else -1 for b in bits])
                # fan-triangulate (quads and the odd n-gon)
                for i in range(1, len(corners) - 1):
                    tris.append([corners[0], corners[i], corners[i + 1]])
    return (np.array(verts, dtype=np.float64),
            np.array(uvs, dtype=np.float64) if uvs else np.zeros((0, 2)),
            np.array(norms, dtype=np.float64) if norms else np.zeros((0, 3)),
            np.array(tris, dtype=np.int64))


def load_palette(obj_path):
    """--> HxWx3 float array of the texture named by the sibling .mtl, or None."""
    mtl = os.path.splitext(obj_path)[0] + '.mtl'
    png = None
    if os.path.isfile(mtl):
        with open(mtl, 'r') as handle:
            for line in handle:
                if line.strip().startswith('map_Kd'):
                    png = line.split(None, 1)[1].strip()
    if png is None:
        png = os.path.splitext(obj_path)[0] + '.png'
    if not os.path.isabs(png):
        png = os.path.join(os.path.dirname(obj_path), png)
    if not os.path.isfile(png):
        return None
    return np.asarray(Image.open(png).convert('RGB'), dtype=np.float64) / 255.0


def sample(texture, uv):
    """Nearest-neighbour texture fetch for an (n, 2) array of UVs."""
    if texture is None:
        return np.ones((len(uv), 3)) * 0.75
    h, w = texture.shape[:2]
    x = np.clip((uv[:, 0] * w).astype(np.int64), 0, w - 1)
    y = np.clip(((1.0 - uv[:, 1]) * h).astype(np.int64), 0, h - 1)
    return texture[y, x]


def view_matrix(yaw_deg, pitch_deg):
    """Rows are the camera's right / up / forward axes in model space (Y up)."""
    yaw, pitch = math.radians(yaw_deg), math.radians(pitch_deg)
    # camera position on a sphere looking at the origin
    forward = np.array([
        -math.sin(yaw) * math.cos(pitch),
        -math.sin(pitch),
        -math.cos(yaw) * math.cos(pitch),
    ])
    right = np.array([math.cos(yaw), 0.0, -math.sin(yaw)])
    up = np.cross(right, forward)
    # third row points TOWARD the camera, so a bigger z is nearer and the
    # depth test can keep the maximum.
    return np.stack([right, up, -forward])


def shade(normals, albedo, model='soft'):
    """Shade camera-space normals. `model` is 'standard' (plain Lambert plus
    hemisphere ambient, i.e. what the creatures get out of the box) or 'soft'
    (Custom/VoxelCreature)."""
    key = np.array(KEY_DIR, dtype=np.float64)
    key /= np.linalg.norm(key)
    ndl = normals @ key
    # hemisphere ambient: lerp ground->sky by the normal's up component
    t = (normals[:, 1] * 0.5 + 0.5)[:, None]
    ambient = np.array(GROUND_COLOR) * (1.0 - t) + np.array(SKY_COLOR) * t

    if model == 'standard':
        diffuse = np.clip(ndl, 0.0, 1.0)[:, None] * np.array(KEY_COLOR)
        return np.clip(albedo * (diffuse + ambient), 0.0, 1.0)

    wrapped = np.clip((ndl + LIGHT_WRAP) / (1.0 + LIGHT_WRAP), 0.0, 1.0)[:, None]
    half = key + np.array([0.0, 0.0, 1.0])       # view dir is +Z in camera space
    half /= np.linalg.norm(half)
    sheen = (np.clip(normals @ half, 0.0, 1.0)[:, None] ** SHEEN_POWER) * SHEEN * wrapped
    rim = (np.clip(1.0 - np.abs(normals[:, 2]), 0.0, 1.0)[:, None] ** RIM_POWER)
    lit = (albedo * wrapped * np.array(KEY_COLOR)
           + np.array(KEY_COLOR) * sheen
           + albedo * ambient
           + albedo * np.array(SHADOW_COLOR) * (1.0 - wrapped) * SHADOW_FILL
           + np.array(RIM_COLOR) * rim * RIM_STRENGTH)
    return np.clip(lit, 0.0, 1.0)


def render(obj_path, yaw, pitch, size, margin=0.08, model='soft'):
    verts, uvs, norms, tris = load_obj(obj_path)
    texture = load_palette(obj_path)

    m = view_matrix(yaw, pitch)
    cam = verts @ m.T                       # x right, y up, z toward the camera
    lo, hi = cam.min(axis=0), cam.max(axis=0)
    centre = (lo + hi) / 2.0
    extent = max((hi - lo)[:2].max(), 1e-6)
    scale = size * (1.0 - 2 * margin) / extent

    px = (cam[:, 0] - centre[0]) * scale + size / 2.0
    py = size / 2.0 - (cam[:, 1] - centre[1]) * scale
    depth = cam[:, 2]

    color = np.zeros((size, size, 3)) + np.array(BACKDROP) / 255.0
    zbuf = np.full((size, size), -1e30)

    has_n = len(norms) > 0
    has_t = len(uvs) > 0
    # per-corner shaded colour, computed once for the whole mesh
    corner_n = (norms[tris[:, :, 2]] if has_n
                else np.zeros((len(tris), 3, 3)))
    if has_n:
        corner_n = corner_n @ m.T           # normals into camera space
    corner_uv = (uvs[tris[:, :, 1]] if has_t else np.zeros((len(tris), 3, 2)))
    flat = corner_uv.reshape(-1, 2)
    corner_albedo = sample(texture, flat).reshape(len(tris), 3, 3)
    if not has_n:
        # flat shade off the geometric normal
        a, b, c = (cam[tris[:, 0, 0]], cam[tris[:, 1, 0]], cam[tris[:, 2, 0]])
        gn = np.cross(b - a, c - a)
        gn /= np.maximum(np.linalg.norm(gn, axis=1, keepdims=True), 1e-12)
        corner_n = np.repeat(gn[:, None, :], 3, axis=1)
    corner_rgb = shade(corner_n.reshape(-1, 3),
                       corner_albedo.reshape(-1, 3),
                       model).reshape(len(tris), 3, 3)

    for t in range(len(tris)):
        i0, i1, i2 = tris[t, :, 0]
        x0, y0, x1, y1, x2, y2 = px[i0], py[i0], px[i1], py[i1], px[i2], py[i2]
        area = (x1 - x0) * (y2 - y0) - (x2 - x0) * (y1 - y0)
        if abs(area) < 1e-9:
            continue
        xlo = max(int(math.floor(min(x0, x1, x2))), 0)
        xhi = min(int(math.ceil(max(x0, x1, x2))) + 1, size)
        ylo = max(int(math.floor(min(y0, y1, y2))), 0)
        yhi = min(int(math.ceil(max(y0, y1, y2))) + 1, size)
        if xlo >= xhi or ylo >= yhi:
            continue
        ys, xs = np.mgrid[ylo:yhi, xlo:xhi]
        xs = xs + 0.5
        ys = ys + 0.5
        w0 = ((x1 - xs) * (y2 - ys) - (x2 - xs) * (y1 - ys)) / area
        w1 = ((x2 - xs) * (y0 - ys) - (x0 - xs) * (y2 - ys)) / area
        w2 = 1.0 - w0 - w1
        inside = (w0 >= 0) & (w1 >= 0) & (w2 >= 0)
        if not inside.any():
            continue
        z = w0 * depth[i0] + w1 * depth[i1] + w2 * depth[i2]
        block = zbuf[ylo:yhi, xlo:xhi]
        win = inside & (z > block)
        if not win.any():
            continue
        rgb = (w0[..., None] * corner_rgb[t, 0]
               + w1[..., None] * corner_rgb[t, 1]
               + w2[..., None] * corner_rgb[t, 2])
        block[win] = z[win]
        color[ylo:yhi, xlo:xhi][win] = np.clip(rgb[win], 0.0, 1.0)

    return Image.fromarray((color * 255).astype(np.uint8))


def main():
    parser = argparse.ArgumentParser(description=__doc__.split('\n')[0])
    parser.add_argument('obj', nargs='+', help='.obj files, one row each')
    parser.add_argument('-o', '--out', default=None, help='output PNG')
    parser.add_argument('--size', type=int, default=320, help='px per view')
    parser.add_argument('--labels', default=None,
                        help='comma separated row labels')
    parser.add_argument('--lighting', default='soft',
                        help="'soft' (Custom/VoxelCreature), 'standard', or one "
                             'per row, comma separated')
    args = parser.parse_args()

    labels = (args.labels.split(',') if args.labels
              else [os.path.basename(p) for p in args.obj])
    pad = 22
    sheet = Image.new('RGB', (args.size * len(VIEWS),
                              (args.size + pad) * len(args.obj)), BACKDROP)
    draw = ImageDraw.Draw(sheet)
    for row, obj in enumerate(args.obj):
        top = row * (args.size + pad)
        verts, _, norms, tris = load_obj(obj)
        models = args.lighting.split(',')
        model = models[row] if row < len(models) else models[-1]
        draw.text((6, top + 6), '%s   %d verts  %d tris  %s normals  %s light' %
                  (labels[row] if row < len(labels) else '', len(verts),
                   len(tris), 'smooth' if len(norms) > 6 else 'flat', model),
                  fill=(230, 230, 240))
        for col, (name, yaw, pitch) in enumerate(VIEWS):
            sheet.paste(render(obj, yaw, pitch, args.size, model=model),
                        (col * args.size, top + pad))
            draw.text((col * args.size + 6, top + pad + 4), name,
                      fill=(200, 200, 210))
    out = args.out or os.path.splitext(args.obj[0])[0] + '_preview.png'
    sheet.save(out)
    print('wrote', out)


if __name__ == '__main__':
    main()
