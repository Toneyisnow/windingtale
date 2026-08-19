"""Inspect a chapter's map data and rebuild the 2D map image from its tiles.

Step 1 of the 2D -> 3D workflow ("understand chapter schema") is a reading
task; this tool supplies the facts that reading alone is bad at: how big the
map is, which tile ids it actually uses, which tile is the ordinary background
one, and what the ShapeMatrix looks like when you paint it back out of the
ShapePanel PNGs.

Subcommands
-----------
info      dimensions, tile-id histogram, obstacles, Shapes property summary
render    rebuild <Width*24> x <Height*24> PNG from ShapeMatrix + tile PNGs
crop      cut a tile rectangle out of a map PNG and upscale it, so a specific
          candidate obstacle can be looked at closely
verify    diff a rebuilt map against the original Chapter-NN.png and report
          which tiles differ (this is how you find what was painted on top of
          the tile grid, i.e. the obstacles)

Examples
--------
    python chapter_map.py info 02
    python chapter_map.py render 02 --grid --labels -o out/chapter02_rebuilt.png
    python chapter_map.py crop 02 --tiles 10,9,15,12 --scale 8 -o out/house.png
    python chapter_map.py verify 02 -o out/chapter02_diff.png
"""

import argparse
import json
import os
from collections import Counter

from PIL import Image, ImageDraw

import voxlib


# --------------------------------------------------------------------------
# loading
# --------------------------------------------------------------------------

def load_chapter(root, nn, path=None):
    path = path or voxlib.chapter_json_path(root, nn)
    with open(path, 'r', encoding='utf-8-sig') as f:
        return json.load(f), path


def tile_image(root, nn, tile_id, cache):
    if tile_id not in cache:
        p = os.path.join(voxlib.shape_panel_dir(root, nn), voxlib.tile_png_name(nn, tile_id))
        cache[tile_id] = Image.open(p).convert('RGB') if os.path.isfile(p) else None
    return cache[tile_id]


def used_tile_ids(chapter):
    c = Counter()
    for col in chapter['ShapeMatrix']:
        c.update(col)
    return c


# --------------------------------------------------------------------------
# render
# --------------------------------------------------------------------------

def render_map(root, nn, chapter, grid=False, labels=False, missing_colour=(255, 0, 255)):
    w, h = chapter['Width'], chapter['Height']
    matrix = chapter['ShapeMatrix']
    img = Image.new('RGB', (w * voxlib.TILE, h * voxlib.TILE), missing_colour)
    cache = {}
    missing = set()
    for x in range(w):
        for y in range(h):
            tid = matrix[x][y]
            t = tile_image(root, nn, tid, cache)
            if t is None:
                missing.add(tid)
                continue
            img.paste(t, (x * voxlib.TILE, y * voxlib.TILE))

    if grid or labels:
        draw = ImageDraw.Draw(img)
        if grid:
            for x in range(w + 1):
                draw.line([(x * voxlib.TILE, 0), (x * voxlib.TILE, h * voxlib.TILE)], fill=(255, 0, 0))
            for y in range(h + 1):
                draw.line([(0, y * voxlib.TILE), (w * voxlib.TILE, y * voxlib.TILE)], fill=(255, 0, 0))
        if labels:
            for x in range(w):
                draw.text((x * voxlib.TILE + 2, 1), str(x + 1), fill=(255, 255, 0))
            for y in range(h):
                draw.text((2, y * voxlib.TILE + 2), str(y + 1), fill=(0, 255, 255))
    return img, missing


# --------------------------------------------------------------------------
# subcommands
# --------------------------------------------------------------------------

def cmd_info(args, root):
    nn = voxlib.nn(args.chapter)
    chapter, path = load_chapter(root, nn, args.chapter_json)
    counts = used_tile_ids(chapter)
    print('chapter %s  (%s)' % (nn, path))
    print('  Width x Height : %d x %d tiles' % (chapter['Width'], chapter['Height']))
    print('  map PNG size   : %d x %d px' % (chapter['Width'] * voxlib.TILE,
                                             chapter['Height'] * voxlib.TILE))
    print('  distinct tiles : %d' % len(counts))
    print('  background tile: %d  (%d of %d cells)'
          % (counts.most_common(1)[0][0], counts.most_common(1)[0][1], sum(counts.values())))
    print('  tile ids       : %s' % ','.join(str(t) for t in sorted(counts)))
    print('  top 12 by use  : %s' % ', '.join('%d x%d' % (t, n) for t, n in counts.most_common(12)))

    panel = voxlib.shape_panel_dir(root, nn)
    prefix = 'Shape_%d_' % (int(nn) - 1)
    have = {int(f[len(prefix):-len('.png')])
            for f in os.listdir(panel) if f.startswith(prefix) and f.endswith('.png')} \
        if os.path.isdir(panel) else set()
    absent = sorted(set(counts) - have)
    print('  ShapePanel%s   : %s (%d %s*.png)%s'
          % (nn, panel, len(have), prefix, '' if not absent else '  MISSING %s' % absent))

    shapes = chapter.get('Shapes') or {}
    if shapes:
        types = Counter(v.get('Type') for v in shapes.values())
        bgs = Counter(v.get('bg') for v in shapes.values())
        print('  Shapes entries : %d   Type histogram %s   bg histogram %s'
              % (len(shapes), dict(types), dict(bgs)))
        undeclared = sorted(t for t in counts if str(t) not in shapes)
        if undeclared:
            print('  !! tiles used by ShapeMatrix but absent from Shapes: %s' % undeclared)

    obstacles = chapter.get('Obstacles') or []
    print('  Obstacles      : %d' % len(obstacles))
    for o in obstacles:
        print('     #%-3s %-22s at X=%s Y=%s' % (o.get('Id'), o.get('DefinitionKey'),
                                                 o['Position']['X'], o['Position']['Y']))

    if args.out:
        with open(args.out, 'w', encoding='utf-8') as f:
            json.dump({'chapter': nn,
                       'width': chapter['Width'], 'height': chapter['Height'],
                       'tile_ids': sorted(counts),
                       'tile_counts': {str(k): v for k, v in counts.items()},
                       'background_tile': counts.most_common(1)[0][0]}, f, indent=2)
        print('  wrote %s' % args.out)


def cmd_render(args, root):
    nn = voxlib.nn(args.chapter)
    chapter, _ = load_chapter(root, nn, args.chapter_json)
    img, missing = render_map(root, nn, chapter, grid=args.grid, labels=args.labels)
    if args.scale != 1:
        img = img.resize((img.width * args.scale, img.height * args.scale), Image.NEAREST)
    out = args.out or os.path.join(os.getcwd(), 'chapter%s_rebuilt.png' % nn)
    img.save(out)
    print('wrote %s (%dx%d)' % (out, img.width, img.height))
    if missing:
        print('MISSING tile PNGs (drawn magenta): %s' % sorted(missing))


def cmd_crop(args, root):
    nn = voxlib.nn(args.chapter)
    src = args.source or voxlib.map_png_path(root, nn)
    img = Image.open(src).convert('RGB')
    x1, y1, x2, y2 = [int(v) for v in args.tiles.split(',')]
    box = ((x1 - 1) * voxlib.TILE, (y1 - 1) * voxlib.TILE, x2 * voxlib.TILE, y2 * voxlib.TILE)
    crop = img.crop(box)
    if args.scale != 1:
        crop = crop.resize((crop.width * args.scale, crop.height * args.scale), Image.NEAREST)
    if args.grid:
        draw = ImageDraw.Draw(crop)
        step = voxlib.TILE * args.scale
        for i in range(0, crop.width + 1, step):
            draw.line([(i, 0), (i, crop.height)], fill=(255, 0, 0))
        for j in range(0, crop.height + 1, step):
            draw.line([(0, j), (crop.width, j)], fill=(255, 0, 0))
    out = args.out or os.path.join(os.getcwd(), 'chapter%s_crop.png' % nn)
    crop.save(out)
    print('wrote %s  tiles X %d..%d  Y %d..%d  (%dx%d px)'
          % (out, x1, x2, y1, y2, crop.width, crop.height))


def cmd_verify(args, root):
    nn = voxlib.nn(args.chapter)
    chapter, _ = load_chapter(root, nn, args.chapter_json)
    rebuilt, missing = render_map(root, nn, chapter)
    original = Image.open(args.source or voxlib.map_png_path(root, nn)).convert('RGB')
    if rebuilt.size != original.size:
        print('SIZE MISMATCH rebuilt %r vs original %r' % (rebuilt.size, original.size))
        return 1

    a, b = rebuilt.load(), original.load()
    w, h = chapter['Width'], chapter['Height']
    per_tile = [[0] * w for _ in range(h)]
    for ty in range(h):
        for tx in range(w):
            n = 0
            for py in range(ty * voxlib.TILE, ty * voxlib.TILE + voxlib.TILE):
                for px in range(tx * voxlib.TILE, tx * voxlib.TILE + voxlib.TILE):
                    if a[px, py] != b[px, py]:
                        n += 1
            per_tile[ty][tx] = n

    total = sum(sum(r) for r in per_tile)
    print('mismatched pixels: %d / %d (%.1f%%)'
          % (total, w * h * voxlib.TILE ** 2, 100.0 * total / (w * h * voxlib.TILE ** 2)))
    if missing:
        print('MISSING tile PNGs: %s' % sorted(missing))
    print()
    print('     ' + ''.join(str((i + 1) % 10) for i in range(w)))
    for ty in range(h):
        row = ''.join('#' if n > args.solid else ('+' if n > args.partial else '.')
                      for n in per_tile[ty])
        print('%4d %s' % (ty + 1, row))
    print()
    print("legend: '#' tile differs heavily, '+' tile differs a little, '.' identical.")
    print('A clean chapter shows all "."; the "#" clusters are what the original')
    print('art painted on top of the tile grid -- i.e. the obstacle footprints.')

    if args.out:
        from PIL import ImageChops
        ImageChops.difference(rebuilt, original).save(args.out)
        print('wrote diff image %s' % args.out)
    return 0


def main():
    p = argparse.ArgumentParser(description=__doc__,
                                formatter_class=argparse.RawDescriptionHelpFormatter)
    p.add_argument('--root', help='windingtale workspace root (auto-detected)')
    p.add_argument('--chapter-json', help='override the Chapter_NN.json path')
    sub = p.add_subparsers(dest='cmd', required=True)

    q = sub.add_parser('info', help='print dimensions / tile ids / obstacles')
    q.add_argument('chapter')
    q.add_argument('-o', '--out', help='also write the summary as JSON')
    q.set_defaults(func=cmd_info)

    q = sub.add_parser('render', help='rebuild the map PNG from ShapeMatrix')
    q.add_argument('chapter')
    q.add_argument('-o', '--out')
    q.add_argument('--grid', action='store_true', help='overlay the tile grid')
    q.add_argument('--labels', action='store_true', help='overlay tile X/Y numbers')
    q.add_argument('--scale', type=int, default=1)
    q.set_defaults(func=cmd_render)

    q = sub.add_parser('crop', help='cut and upscale a tile rectangle')
    q.add_argument('chapter')
    q.add_argument('--tiles', required=True, metavar='X1,Y1,X2,Y2',
                   help='inclusive 1-based tile rectangle')
    q.add_argument('--source', help='PNG to cut from (default the original map)')
    q.add_argument('--scale', type=int, default=4)
    q.add_argument('--grid', action='store_true')
    q.add_argument('-o', '--out')
    q.set_defaults(func=cmd_crop)

    q = sub.add_parser('verify', help='diff rebuilt vs original map, per tile')
    q.add_argument('chapter')
    q.add_argument('--source', help='original map PNG (default Chapter-NN.png)')
    q.add_argument('--solid', type=int, default=200, help="pixels over which a tile prints '#'")
    q.add_argument('--partial', type=int, default=20, help="pixels over which a tile prints '+'")
    q.add_argument('-o', '--out', help='write the raw diff image here')
    q.set_defaults(func=cmd_verify)

    args = p.parse_args()
    root = args.root or voxlib.workspace_root()
    raise SystemExit(args.func(args, root) or 0)


if __name__ == '__main__':
    main()
