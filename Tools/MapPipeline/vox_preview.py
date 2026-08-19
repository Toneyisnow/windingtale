"""Render a .vox to a PNG contact sheet so a model can be eyeballed.

There is no way to "look at" a VOX from a terminal, which makes it very easy to
generate an obstacle that is the right size and completely wrong shape. This
renders six orthographic views plus two isometric ones onto a single sheet.

    python vox_preview.py path/to/dwelling_house_1.vox
    python vox_preview.py ../../Resources/Remastered/Obstacles/vox/*.vox -o out/
    python vox_preview.py Shape_1_43.vox --scale 6 --views front,iso

The header line under each view gives the model's SIZE in voxels and, for
obstacles, the footprint in tiles (SIZE / 24).
"""

import argparse
import os

from PIL import Image, ImageDraw

import voxlib

BACKDROP = (28, 28, 34)
GRID = (58, 58, 70)

# name -> (u_axis, v_axis, depth_axis) with a sign on each. v points DOWN in the
# image, so "up" axes are negated. The depth axis must increase TOWARD the
# camera: _paint sorts ascending and draws in that order, so the nearest voxels
# have to come last or the model renders inside-out.
ORTHO_VIEWS = {
    'front': ((0, 1), (2, -1), (1, -1)),   # camera at -Y: X right, Z up
    'back': ((0, -1), (2, -1), (1, 1)),    # camera at +Y
    'left': ((1, 1), (2, -1), (0, -1)),    # camera at -X
    'right': ((1, -1), (2, -1), (0, 1)),   # camera at +X
    'top': ((0, 1), (1, -1), (2, 1)),      # camera above: X right, Y up-screen
    'bottom': ((0, 1), (1, 1), (2, -1)),   # camera below
}


def _project_ortho(voxels, view):
    (ua, us), (va, vs), (da, ds) = ORTHO_VIEWS[view]
    out = []
    for v in voxels:
        out.append((v[ua] * us, v[va] * vs, v[da] * ds, v[3]))
    return out


def _project_iso(voxels, turn=0):
    """Classic 2:1 isometric. ``turn`` rotates the model 90 degrees per step."""
    out = []
    for x, y, z, c in voxels:
        for _ in range(turn):
            x, y = y, -x
        u = (x - y) * 2
        v = -(x + y) - z * 2
        # These screen axes put the view axis along (1, 1, -1), so the camera
        # sits at (-1, -1, +1): above, and in front of the y = 0 facade.
        out.append((u, v, z - x - y, c))
    return out


def _paint(points, palette, scale, cell):
    """Painter's algorithm: sort back-to-front, draw a ``cell``-sized square."""
    if not points:
        return Image.new('RGB', (scale, scale), BACKDROP)
    us = [p[0] for p in points]
    vs = [p[1] for p in points]
    w = (max(us) - min(us) + 1) * scale + cell
    h = (max(vs) - min(vs) + 1) * scale + cell
    img = Image.new('RGB', (w, h), BACKDROP)
    draw = ImageDraw.Draw(img)
    u0, v0 = min(us), min(vs)
    for u, v, _d, c in sorted(points, key=lambda p: p[2]):
        rgb = palette[c - 1][:3]
        x, y = (u - u0) * scale, (v - v0) * scale
        draw.rectangle([x, y, x + cell - 1, y + cell - 1], fill=rgb)
    return img


def render(model, views, scale, label=None):
    tiles = []
    for name in views:
        if name in ('iso', 'iso2'):
            pts = _project_iso(model.voxels, 1 if name == 'iso2' else 0)
            # the isometric projection places voxels 2 units apart horizontally,
            # so the squares have to be twice as wide or the model looks perforated
            cell = scale * 2 + 1
        else:
            pts = _project_ortho(model.voxels, name)
            cell = scale + 1
        tiles.append((name, _paint(pts, model.palette, scale, cell)))

    pad, header = 12, 16
    cols = min(4, len(tiles))
    rows = (len(tiles) + cols - 1) // cols
    # size each column/row to its own content instead of the global maximum,
    # otherwise one big isometric view inflates the whole sheet
    col_w = [0] * cols
    row_h = [0] * rows
    for i, (_n, img) in enumerate(tiles):
        col_w[i % cols] = max(col_w[i % cols], img.width + pad * 2)
        row_h[i // cols] = max(row_h[i // cols], img.height + pad * 2 + header)

    sheet = Image.new('RGB', (sum(col_w), sum(row_h) + header + pad), BACKDROP)
    draw = ImageDraw.Draw(sheet)
    if label:
        draw.text((pad, pad // 2), label, fill=(220, 220, 230))
    for i, (name, img) in enumerate(tiles):
        c, r = i % cols, i // cols
        cx, cy = sum(col_w[:c]), sum(row_h[:r]) + header + pad
        draw.rectangle([cx + 2, cy + 2, cx + col_w[c] - 4, cy + row_h[r] - 4], outline=GRID)
        draw.text((cx + pad, cy + 3), name, fill=(150, 200, 150))
        sheet.paste(img, (cx + pad, cy + header))
    return sheet


def main():
    p = argparse.ArgumentParser(description=__doc__,
                                formatter_class=argparse.RawDescriptionHelpFormatter)
    p.add_argument('vox', nargs='+')
    p.add_argument('-o', '--out', help='output PNG, or a directory for several inputs')
    p.add_argument('--scale', type=int, default=4, help='pixels per voxel (default 4)')
    p.add_argument('--views', default='front,right,back,left,top,iso,iso2',
                   help='comma-separated: %s, iso, iso2'
                        % ', '.join(sorted(ORTHO_VIEWS)))
    args = p.parse_args()

    views = [v.strip() for v in args.views.split(',') if v.strip()]
    for name in views:
        if name not in ORTHO_VIEWS and name not in ('iso', 'iso2'):
            raise SystemExit('unknown view %r' % name)

    many = len(args.vox) > 1
    out_dir = args.out if (many or (args.out and os.path.isdir(args.out))) else None
    if out_dir and not os.path.isdir(out_dir):
        os.makedirs(out_dir)

    for path in args.vox:
        model = voxlib.read_vox(path)
        sx, sy, sz = model.size
        label = '%s   SIZE %dx%dx%d' % (os.path.basename(path), sx, sy, sz)
        if sx % voxlib.TILE == 0 and sy % voxlib.TILE == 0:
            label += '   footprint %d cols x %d rows' % (sx // voxlib.TILE, sy // voxlib.TILE)
        label += '   %d voxels' % len(model.voxels)
        sheet = render(model, views, args.scale, label=label)
        if out_dir:
            dest = os.path.join(out_dir, os.path.splitext(os.path.basename(path))[0] + '_preview.png')
        else:
            dest = args.out or os.path.splitext(path)[0] + '_preview.png'
        sheet.save(dest)
        print('%s -> %s  (%dx%d)' % (label, dest, sheet.width, sheet.height))


if __name__ == '__main__':
    main()
