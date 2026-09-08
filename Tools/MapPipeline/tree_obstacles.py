"""Derive a chapter's tree obstacles from its tile matrix.

The 2D art paints every tree over two tiles: a *top* tile holding the cone of
the crown, and directly under it a *base* tile holding the foliage band, the
trunk and the roots. A forest is a column of tops stacked one on the other with
a single base at the bottom: each top is one tree, and the tree in front hides
everything below the crown of the one behind it. So

  * every top tile at (X, Y) is one tree, standing on the tile below, (X, Y+1)
    -- the row its trunk is painted on, the same convention as the statues and
    fire pillars that are painted over two rows and stand on one;
  * a base tile is where the tree whose top is above it stands, and is cleaned
    with that tree. A base with nothing above it (its top is off the map's
    upper edge) stands its own tree;
  * a top on the last row, or one whose trunk row is not a tree tile at all
    (a crown peeking out from behind a house), stands on its own tile.

Which tile ids are tops and bases, and the colour of each, is read off the
art -- tree_obstacles.py is told, it does not detect:

    python tree_obstacles.py 01 \
        --top 40,43=tree_light_green --top 44,47=tree_dark_green \
        --base 42=tree_light_green   --base 46=tree_dark_green \
        --with obstacles/obstacles_01.json \
        -o obstacles/obstacles_01_with_trees.json

``--with`` prepends the chapter's hand-authored obstacle list (the buildings),
so the output is the complete list map_clean.py takes. A base's colour only
matters when it has no top above it: the tree is the colour of its crown.

Every cleared tree tile carries a ``Fill``: the ordinary ground tile whose art
is closest in colour to the ground the tree was painted on, found by comparing
colour histograms of the tile PNGs (the crown's shades are not in any ground
tile, so the match is driven by the grass around it). map_clean.py paints that
tile instead of growing the neighbours in, so a tree on the edge of a road
comes back as grass, not road.

The output is deterministic -- the same chapter and the same arguments give
the same list, ids running row by row after the ``--with`` ones.
"""

import argparse
import json
import os
from collections import Counter, OrderedDict

from PIL import Image

import map_clean
import voxlib

MIN_COMMON = 4          # a ground tile used fewer times than this is scenery
MIN_OVERLAP = 0.15      # below this histogram overlap, leave the fill to the neighbours
TIE_BAND = 0.03         # candidates scoring within this much of each other tie


def parse_spec(values, what):
    """['40,43=tree_light_green', ...] -> {40: 'tree_light_green', 43: ...}."""
    out = {}
    for value in values:
        if '=' not in value:
            raise SystemExit('--%s must be IDS=KEY, got %r' % (what, value))
        ids, key = value.split('=', 1)
        key = key.strip()
        for t in ids.replace(' ', '').split(','):
            if t:
                out[int(t)] = key
    return out


def tile_histogram(root, nn, tile_id, cache):
    if tile_id not in cache:
        path = os.path.join(voxlib.shape_panel_dir(root, nn), voxlib.tile_png_name(nn, tile_id))
        if not os.path.isfile(path):
            raise SystemExit('tile PNG not found: %s' % path)
        cache[tile_id] = Counter(Image.open(path).convert('RGB').getdata())
    return cache[tile_id]


def tree_colours():
    """Every colour the tree models are painted in: the crown ramps, bark and
    roots of build_trees.py. Pixels of these colours in a tree tile are the
    tree, everything else is the ground it stands on."""
    import build_trees
    colours = set()
    for ramp in build_trees.COLOURS.values():
        colours.update(ramp)
    colours.update(build_trees.BARK)
    colours.update(build_trees.ROOT)
    colours.update(build_trees.SHADOW)
    return colours


def tile_pixels(root, nn, tile_id, cache):
    key = ('px', tile_id)
    if key not in cache:
        path = os.path.join(voxlib.shape_panel_dir(root, nn), voxlib.tile_png_name(nn, tile_id))
        if not os.path.isfile(path):
            raise SystemExit('tile PNG not found: %s' % path)
        cache[key] = list(Image.open(path).convert('RGB').getdata())
    return cache[key]


def ground_fill(root, nn, tile_id, candidates, counts, cache, ignore):
    """(best ground tile, score 0..1, runners-up) for a tree tile.

    The art paints a tree over the chapter's ground texture, so the plain
    ground tile matches the tree tile pixel for pixel everywhere the tree is
    not: the candidate with the most identical pixels wins. A tile that is
    grass plus the edge of a crown matches just as well, so ties go to the
    candidate used most on the map -- the plain ground -- and then to the
    lowest id.

    When no candidate matches even a quarter of the tile pixel for pixel (a
    tree painted on a texture the map does not use plain), fall back to colour
    histograms: the tree tile's pixels that are not tree colours are its
    ground, and the candidate whose colours cover the most of them wins.
    """
    # Scores within a few percent of each other are a tie -- a stray pixel of a
    # root clump landing under the cone must not outvote the plain ground.
    def rank(score, cand):
        return (int(score / TIE_BAND), counts[cand], -cand)

    px = tile_pixels(root, nn, tile_id, cache)
    scored = []
    for cand in candidates:
        cp = tile_pixels(root, nn, cand, cache)
        same = sum(1 for a, b in zip(px, cp) if a == b) / float(len(px))
        scored.append((rank(same, cand), same, cand))
    scored.sort(reverse=True)
    if scored[0][1] >= 0.25:
        best = scored[0]
        return best[2], best[1], [(s[2], s[1]) for s in scored[1:4]]

    h = Counter({colour: n for colour, n in tile_histogram(root, nn, tile_id, cache).items()
                 if colour not in ignore})
    total = float(sum(h.values()))
    if total == 0:
        return None, 0.0, []
    scored = []
    for cand in candidates:
        c = tile_histogram(root, nn, cand, cache)
        score = sum(min(n, c.get(colour, 0)) for colour, n in h.items()) / total
        scored.append((rank(score, cand), score, cand))
    scored.sort(reverse=True)
    best = scored[0]
    return best[2], best[1], [(s[2], s[1]) for s in scored[1:4]]


def derive(matrix, width, height, tops, bases, occupied=frozenset()):
    """The trees: [(key, stand_x, stand_y, [clear tiles])], row by row.

    ``occupied`` is the set of tiles the chapter's other obstacles cover. A
    tree whose trunk row is one of those is painted in front of a building
    (chapter 01 has one over a hut's corner); it stands on its crown tile
    instead, and the building keeps the base tile.
    """
    def at(x, y):
        if 1 <= x <= width and 1 <= y <= height:
            return matrix[x - 1][y - 1]
        return None

    def is_tree(x, y):
        t = at(x, y)
        return t is not None and (t in tops or t in bases)

    trees = []
    taken = set()
    for y in range(1, height + 1):
        for x in range(1, width + 1):
            t = at(x, y)
            if t in tops:
                key = tops[t]
                clear = [(x, y)]
                below = (x, y + 1)
                if (x, y) in occupied:
                    # A crown painted over a building's back rows: the tree is
                    # behind the building. Stand it on the free ground one row
                    # further back, with nothing to clean there (the building
                    # cleans the crown tile); drop it when that row is off the
                    # map, another obstacle, or a tree tile of its own.
                    behind = (x, y - 1)
                    if y - 1 < 1 or behind in occupied or is_tree(x, y - 1):
                        continue
                    stand = behind
                    clear = []
                elif y + 1 <= height and is_tree(x, y + 1) and below not in occupied:
                    stand = below
                    if at(x, y + 1) in bases:
                        clear.append(stand)
                else:
                    stand = (x, y)
            elif t in bases:
                if at(x, y - 1) in tops:
                    continue            # cleaned with the tree standing here
                key = bases[t]
                clear = [(x, y)]
                stand = (x, y)
            else:
                continue
            if stand in taken:
                # two crowns claim one tile: only at the bottom edge, where the
                # last row's top falls back onto its own tile. Its tiles are
                # still cleaned, with the tree that took the spot.
                for tree in trees:
                    if (tree[1], tree[2]) == stand:
                        tree[3].extend(clear)
                        break
                continue
            taken.add(stand)
            trees.append([key, stand[0], stand[1], clear])
    return trees


def main():
    p = argparse.ArgumentParser(description=__doc__,
                                formatter_class=argparse.RawDescriptionHelpFormatter)
    p.add_argument('chapter')
    p.add_argument('--root')
    p.add_argument('--chapter-json', help='override the source Chapter_NN.json (the painted map)')
    p.add_argument('--top', action='append', default=[], metavar='IDS=KEY',
                   help='crown-top tile ids and the tree they belong to; repeatable')
    p.add_argument('--base', action='append', default=[], metavar='IDS=KEY',
                   help='trunk/base tile ids and the tree they belong to; repeatable')
    p.add_argument('--with', dest='with_list', metavar='JSON',
                   help='obstacle list to put in front of the trees (the buildings)')
    p.add_argument('--colour-from-stand', action='store_true',
                   help='a tree takes the key of the tile it stands on rather than of '
                        'its crown tile. For chapter 16, where every crown is the same '
                        'snow-white cone and the colour only shows on the tile below: '
                        'give --top the key of the tree showing BEHIND that tile')
    p.add_argument('--fill-exclude', default='',
                   help='comma-separated tile ids never used as a tree tile\'s Fill')
    p.add_argument('--fill', action='append', default=[], metavar='IDS=TILE',
                   help='force the Fill of these tree tiles to TILE instead of the '
                        'colour match; repeatable. For a chapter whose forests '
                        'alternate two tree colours cell by cell over one lawn '
                        '(chapter 14): matched per tile id, the two would fill with '
                        'two different grass tiles and the cleaned map would show a '
                        'checkerboard where the forest stood')
    p.add_argument('-o', '--out', required=True, help='obstacle list to write')
    args = p.parse_args()

    root = args.root or voxlib.workspace_root()
    nn = voxlib.nn(args.chapter)
    src = args.chapter_json or voxlib.chapter_json_path(root, nn)
    with open(src, 'r', encoding='utf-8-sig') as f:
        chapter = json.load(f, object_pairs_hook=OrderedDict)
    width, height = chapter['Width'], chapter['Height']
    matrix = chapter['ShapeMatrix']

    tops = parse_spec(args.top, 'top')
    bases = parse_spec(args.base, 'base')
    if not tops and not bases:
        raise SystemExit('no --top / --base tiles given')
    tree_tiles = set(tops) | set(bases)

    before = []
    if args.with_list:
        with open(args.with_list, 'r', encoding='utf-8-sig') as f:
            before = json.load(f)
        if isinstance(before, dict):
            before = before['Obstacles']

    # the tiles the buildings cover: a tree's trunk row cannot be one of them
    occupied = set()
    for o in before:
        if o.get('Clear'):
            rects = [(int(r['X']), int(r['Y']), int(r['Cols']), int(r['Rows'])) for r in o['Clear']]
        else:
            cols, rows = map_clean.obstacle_tile_size(root, o['DefinitionKey'], o.get('Size'))
            rects = [(int(o['Position']['X']), int(o['Position']['Y']), cols, rows)]
        for x, y, cols, rows in rects:
            occupied.update((tx, ty) for tx in range(x, x + cols) for ty in range(y, y + rows))

    # ground candidates: Plain, common, not a tree
    counts = Counter()
    for col in matrix:
        counts.update(col)
    shapes = chapter.get('Shapes', {})
    excluded = set(int(t) for t in args.fill_exclude.replace(' ', '').split(',') if t)
    candidates = [t for t, n in counts.items()
                  if n >= MIN_COMMON and t not in tree_tiles and t not in excluded
                  and int(shapes.get(str(t), {}).get('Type', 0)) == 0]
    if not candidates:
        raise SystemExit('no ground candidates on this map')

    cache = {}
    fills = {}
    ignore = tree_colours()
    print('chapter %s  %dx%d tiles   source %s' % (nn, width, height, src))
    print('fill per tree tile (closest ground by colour of the non-tree pixels):')
    for t in sorted(tree_tiles):
        if t not in counts:
            print('  tile %-4d not on this map' % t)
            continue
        best, score, others = ground_fill(root, nn, t, candidates, counts, cache, ignore)
        also = '   then %s' % ', '.join('%d %.0f%%' % (c, s * 100) for c, s in others) if others else ''
        if best is None or score < MIN_OVERLAP:
            print('  tile %-4d -> (neighbours)   best %s covers only %.0f%%' % (t, best, score * 100))
            continue
        fills[t] = best
        print('  tile %-4d -> %-4d  %.0f%% covered  x%d%s' % (t, best, score * 100, counts[t], also))
    for spec in args.fill:
        ids, _, tile = spec.replace(' ', '').partition('=')
        if not tile:
            raise SystemExit('--fill expects IDS=TILE, got %r' % spec)
        for t in ids.split(','):
            fills[int(t)] = int(tile)
            print('  tile %-4d -> %-4d  forced by --fill' % (int(t), int(tile)))

    trees = derive(matrix, width, height, tops, bases, occupied)
    if args.colour_from_stand:
        for tree in trees:
            t = matrix[tree[1] - 1][tree[2] - 1]
            if t in tops:
                tree[0] = tops[t]
            elif t in bases:
                tree[0] = bases[t]

    # A tile deep inside a forest shows no ground at all -- crowns behind it on
    # every side -- so it has no fill of its own. It takes the fill the rest of
    # its forest agreed on: the commonest fill among the tree tiles connected
    # to it (4-neighbours), which is where the visible ground around the
    # forest's edge and under its trunks ends up.
    def tile_fill(x, y):
        painted = matrix[x - 1][y - 1]
        if painted in fills:
            return fills[painted]
        seen = set([(x, y)])
        queue = [(x, y)]
        votes = Counter()
        while queue:
            cx, cy = queue.pop(0)
            for nx, ny in ((cx + 1, cy), (cx - 1, cy), (cx, cy + 1), (cx, cy - 1)):
                if (nx, ny) in seen or not (1 <= nx <= width and 1 <= ny <= height):
                    continue
                t = matrix[nx - 1][ny - 1]
                if t not in tree_tiles:
                    continue
                seen.add((nx, ny))
                queue.append((nx, ny))
                if t in fills:
                    votes[fills[t]] += 1
        if not votes:
            return None
        return max(votes, key=lambda f: (votes[f], counts[f], -f))

    obstacles = []
    for i, o in enumerate(before, start=1):
        o = OrderedDict(o)
        o['Id'] = int(o.get('Id', i))
        obstacles.append(o)
    next_id = max([o['Id'] for o in obstacles] + [0]) + 1
    per_key = Counter()
    cleared = set()
    unfilled = []
    for key, x, y, clear in trees:
        rects = []
        for cx, cy in clear:
            rect = OrderedDict([('X', cx), ('Y', cy), ('Cols', 1), ('Rows', 1)])
            fill = tile_fill(cx, cy)
            if fill is not None:
                rect['Fill'] = fill
            else:
                unfilled.append((cx, cy))
            rects.append(rect)
            cleared.add((cx, cy))
        if not rects:
            # nothing to clean (a tree stood behind a building): a rectangle
            # that paints the stand tile with its own id, so map_clean.py does
            # not fall back to clearing Position + Size
            rects.append(OrderedDict([('X', x), ('Y', y), ('Cols', 1), ('Rows', 1),
                                      ('Fill', matrix[x - 1][y - 1])]))
        obstacles.append(OrderedDict([
            ('Id', next_id), ('DefinitionKey', key),
            ('Position', OrderedDict([('X', x), ('Y', y)])),
            ('Clear', rects)]))
        next_id += 1
        per_key[key] += 1

    if unfilled:
        print('tiles with no fill of their own or their forest\'s, left to the neighbours: %s' % unfilled)

    # every tree tile must be cleaned by exactly one tree
    all_tree_tiles = set((x + 1, y + 1) for x in range(width) for y in range(height)
                         if matrix[x][y] in tree_tiles)
    missed = sorted(all_tree_tiles - cleared - occupied)
    if missed:
        raise SystemExit('tree tiles not cleaned by any tree: %s' % missed)
    under = sorted(all_tree_tiles & occupied - cleared)

    print('trees: %d   (%s)' % (len(trees), ', '.join('%s x%d' % kv for kv in sorted(per_key.items()))))
    print('tiles cleared: %d   obstacles carried over: %d%s'
          % (len(cleared), len(before),
             '   tree tiles left to the buildings covering them: %s' % under if under else ''))

    # where the trees stand, one character per tile
    stand = {(x, y): key for key, x, y, _c in trees}
    symbols = {}
    for key in sorted(per_key):
        symbols[key] = chr(ord('a') + len(symbols))
    print('legend: %s' % '  '.join('%s=%s' % (s, k) for k, s in symbols.items()))
    for y in range(1, height + 1):
        row = ''
        for x in range(1, width + 1):
            if (x, y) in stand:
                row += symbols[stand[(x, y)]]
            elif (x, y) in cleared:
                row += '.'
            else:
                row += ' '
        print('  %2d |%s|' % (y, row))

    with open(args.out, 'w', encoding='utf-8') as f:
        json.dump({'Obstacles': obstacles}, f, indent=2)
        f.write('\n')
    print('wrote %s  (%d obstacles)' % (args.out, len(obstacles)))


if __name__ == '__main__':
    main()
