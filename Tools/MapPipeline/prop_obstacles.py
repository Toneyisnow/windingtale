"""Read the props painted in a column of tiles -- statues, pillars, columns --
out of a chapter's ShapeMatrix into an obstacle list for map_clean.py.

The art draws a prop in elevation over a stack of tiles: the knight bust over
its pedestal, a column's ball over its shaft over its base. The prop stands on
the *bottom* tile of the stack (the Blocked one) and the tiles above it are
cleaned with it -- the same "painted over N rows, stands on one" rule the
statues of chapter 08, the fire pillars of chapter 10 and the columns of
chapter 21 follow.

    python prop_obstacles.py 23 \\
        --stack stone_column_1=64/66/68 \\
        --stack stone_column_2=64/68,304/308 \\
        --stack stone_pillar_1=126/33,122/32 \\
        --stack stone_statue_1=120/32 \\
        -o obstacles/obstacles_23.json

``--stack KEY=top/../bottom`` lists a stack top to bottom; several alternative
stacks for one key are separated by commas. Longer stacks are matched first,
so a two-tile column is not read off the lower half of a three-tile one.

Every cleared tile gets a ``Fill``: the tile whose art matches the prop tile
pixel for pixel wherever the prop is not -- the ground the artist painted it
over -- chosen among the chapter's tiles that are not prop tiles themselves
(ties go to the tile used most on the map). ``--fill TILE=GROUND`` overrides
that for one prop tile. ``--with FILE`` merges a hand-written list (a prop
that is not a simple stack) in front of the generated one.
"""

import argparse
import json
import os
from collections import Counter, OrderedDict

from PIL import Image

import voxlib


def parse_stacks(values):
    stacks = []
    for v in values or []:
        key, _, spec = v.partition('=')
        if not spec:
            raise SystemExit('--stack expects KEY=top/../bottom: %r' % v)
        for alt in spec.split(','):
            tiles = [int(t) for t in alt.split('/') if t]
            stacks.append((key.strip(), tiles))
    stacks.sort(key=lambda s: -len(s[1]))
    return stacks


def tile_pixels(root, nn, tile_id, cache):
    if tile_id not in cache:
        path = os.path.join(voxlib.shape_panel_dir(root, nn), voxlib.tile_png_name(nn, tile_id))
        cache[tile_id] = list(Image.open(path).convert('RGB').getdata())
    return cache[tile_id]


def ground_fill(root, nn, tile_id, candidates, counts, cache):
    px = tile_pixels(root, nn, tile_id, cache)
    scored = []
    for cand in candidates:
        cp = tile_pixels(root, nn, cand, cache)
        same = sum(1 for a, b in zip(px, cp) if a == b)
        scored.append((same, counts[cand], -cand, cand))
    scored.sort(reverse=True)
    best = scored[0]
    return best[3], best[0] / float(len(px)), [(s[3], s[0] / float(len(px))) for s in scored[1:3]]


def main():
    ap = argparse.ArgumentParser(description=__doc__.split('\n\n')[0],
                                 formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument('chapter')
    ap.add_argument('--root')
    ap.add_argument('--stack', action='append', required=True)
    ap.add_argument('--fill', action='append', help='TILE=GROUND override (repeatable)')
    ap.add_argument('--with', dest='with_file', help='hand-written obstacle list to merge in front')
    ap.add_argument('-o', '--out', required=True)
    a = ap.parse_args()

    root = a.root or voxlib.workspace_root()
    nn = '%02d' % int(a.chapter)
    chapter_path = voxlib.chapter_json_path(root, nn)
    chapter = json.load(open(chapter_path, encoding='utf-8'))
    width, height = chapter['Width'], chapter['Height']
    matrix = chapter['ShapeMatrix']
    shapes = chapter.get('Shapes', {})

    def at(x, y):
        if 1 <= x <= width and 1 <= y <= height:
            return matrix[x - 1][y - 1]
        return None

    stacks = parse_stacks(a.stack)
    prop_tiles = set(t for _, tiles in stacks for t in tiles)
    overrides = {}
    for v in a.fill or []:
        t, _, g = v.partition('=')
        overrides[int(t)] = int(g)

    counts = Counter()
    for col in matrix:
        counts.update(col)
    candidates = sorted(t for t in counts if t not in prop_tiles)

    # fill per prop tile
    cache = {}
    fills = {}
    print('fill per prop tile (pixel match against the chapter\'s other tiles):')
    for t in sorted(prop_tiles):
        if t in overrides:
            fills[t] = overrides[t]
            print('  %4d -> %4d  (forced)' % (t, fills[t]))
            continue
        best, score, runners = ground_fill(root, nn, t, candidates, counts, cache)
        fills[t] = best
        print('  %4d -> %4d  %3.0f%% identical   next: %s' % (
            t, best, score * 100, ', '.join('%d (%.0f%%)' % (c, s * 100) for c, s in runners)))

    obstacles = []
    if a.with_file:
        with_list = json.load(open(a.with_file, encoding='utf-8'))
        obstacles.extend(with_list['Obstacles'] if isinstance(with_list, dict) else with_list)
    occupied = set()
    for o in obstacles:
        for r in o.get('Clear', []):
            for dx in range(int(r['Cols'])):
                for dy in range(int(r['Rows'])):
                    occupied.add((int(r['X']) + dx, int(r['Y']) + dy))

    found = []
    for key, tiles in stacks:
        bottom = tiles[-1]
        n = len(tiles)
        for y in range(1, height + 1):
            for x in range(1, width + 1):
                if at(x, y) != bottom or (x, y) in occupied:
                    continue
                if any(at(x, y - i) != tiles[-1 - i] or (x, y - i) in occupied for i in range(1, n)):
                    continue
                for i in range(n):
                    occupied.add((x, y - i))
                found.append((key, tiles, x, y))

    found.sort(key=lambda f: (f[3], f[2]))
    next_id = max([int(o.get('Id', 0)) for o in obstacles] + [0]) + 1
    per_key = Counter()
    for key, tiles, x, y in found:
        n = len(tiles)
        clear = []
        for i, t in enumerate(tiles):
            clear.append(OrderedDict([('X', x), ('Y', y - (n - 1) + i), ('Cols', 1), ('Rows', 1),
                                      ('Fill', fills[t])]))
        obstacles.append(OrderedDict([('Id', next_id), ('DefinitionKey', key),
                                      ('Position', OrderedDict([('X', x), ('Y', y)])),
                                      ('Clear', clear)]))
        next_id += 1
        per_key[key] += 1
        stand_type = int(shapes.get(str(tiles[-1]), {}).get('Type', 0))
        if stand_type == 0:
            print('  note: %s at (%d, %d) stands on tile %d, which is Type 0 (walkable)' % (key, x, y, tiles[-1]))

    # prop tiles left over that no stack matched
    leftovers = Counter()
    for y in range(1, height + 1):
        for x in range(1, width + 1):
            t = at(x, y)
            if t in prop_tiles and (x, y) not in occupied:
                leftovers[t] += 1
    for t, n in sorted(leftovers.items()):
        print('  WARNING: %d x tile %d is a prop tile but matched no stack' % (n, t))

    out = OrderedDict([
        ('_comment', ['Chapter %s obstacle list -- input to map_clean.py.' % nn,
                      'Generated by prop_obstacles.py from the tile matrix: ' +
                      '; '.join('%s = %s' % (k, '/'.join(map(str, t))) for k, t in stacks) +
                      '. Each prop stands on the bottom tile of its stack and Clear takes the whole stack, ' +
                      'every tile with the ground its art was painted over as Fill.']),
        ('Obstacles', obstacles),
    ])
    with open(a.out, 'w', encoding='utf-8') as f:
        json.dump(out, f, indent=2)
        f.write('\n')
    print('wrote %s: %d obstacles  %s' % (a.out, len(obstacles),
                                         ', '.join('%s x%d' % kv for kv in sorted(per_key.items()))))


if __name__ == '__main__':
    main()
