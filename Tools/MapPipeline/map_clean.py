"""Punch obstacle footprints out of a chapter's ShapeMatrix.

Step 2 of the 2D -> 3D workflow. The obstacles (houses, huts, barrels...) are
painted into the original 2D tile art; once they become 3D models standing on
the board, the tiles underneath them have to go back to plain ground.

Input is an obstacle list -- the judgement call about what is on the map and
where is made by a human/skill looking at the art, not by this tool:

    {
      "Obstacles": [
        { "Id": 1, "DefinitionKey": "blue_house_1",
          "Position": { "X": 3, "Y": 3 },
          "Size": { "Cols": 6, "Rows": 4 } }
      ]
    }

``Position`` is the top-left tile of the footprint, 1-based, and may be <= 0 or
run past the board when the object is only partly on screen. ``Size`` is
optional: when omitted it is read from Resources/Remastered/Obstacles/vox/
<DefinitionKey>.vox, whose SIZE chunk is (Cols*24, Rows*24, height).

Output is Chapter_NN_Cleaned.json: the same chapter with the footprint tiles
replaced by the fill tile and an "Obstacles" block inserted immediately before
"ShapeMatrix". A Chapter_NN_UsedTiles.json listing the tile ids that survive
the clean is written alongside it -- that is the input to step 3.

Examples
--------
    python map_clean.py 02 --obstacles obstacles_02.json
    python map_clean.py 02 --obstacles obstacles_02.json --fill 52 --dry-run
"""

import argparse
import json
import os
from collections import Counter, OrderedDict

import voxlib


def obstacle_tile_size(root, key, size=None):
    """(cols, rows) footprint of an obstacle, from its declared Size or its VOX."""
    if size:
        return int(size['Cols']), int(size['Rows'])
    p = os.path.join(voxlib.obstacles_vox_dir(root), '%s.vox' % key)
    if not os.path.isfile(p):
        raise SystemExit(
            'obstacle "%s" has no Size in the input and no VOX at %s -- either add\n'
            '  "Size": {"Cols": C, "Rows": R}  or build the VOX first.' % (key, p))
    sx, sy, _sz = voxlib.read_vox(p).size
    if sx % voxlib.TILE or sy % voxlib.TILE:
        raise SystemExit('%s: SIZE %dx%d is not a whole number of %d-px tiles'
                         % (p, sx, sy, voxlib.TILE))
    return sx // voxlib.TILE, sy // voxlib.TILE


def covered_tiles(width, height, footprints):
    """The on-board tiles under the footprints, plus a per-obstacle count."""
    cleared = set()
    per = []
    for key, x, y, cols, rows in footprints:
        n = 0
        for tx in range(x, x + cols):
            for ty in range(y, y + rows):
                if 1 <= tx <= width and 1 <= ty <= height:
                    cleared.add((tx, ty))
                    n += 1
        per.append((key, x, y, cols, rows, n))
    return cleared, per


def clean_matrix(matrix, width, height, cleared, fill_tile):
    """Replace every cleared tile with ``fill_tile``."""
    new = [list(col) for col in matrix]
    for tx, ty in cleared:
        new[tx - 1][ty - 1] = fill_tile
    return new


MIN_COMMON = 4      # a tile used fewer times than this is scenery, not ground


def clean_matrix_nearest(matrix, width, height, cleared, common):
    """Replace every cleared tile with its nearest surviving neighbour's tile.

    A single fill tile is wrong whenever the map has more than one kind of
    ordinary ground -- chapter 02's churches stand on cobbled plazas while its
    hut stands on grass, so one global fill leaves either stone patches in the
    lawn or lawn patches in the plaza. This grows the surviving ground inward
    instead (multi-source BFS).

    ``common`` is the whole-map tile histogram, used to keep one-off scenery
    from breeding: a treasure or signpost tile sitting next to a cleared area
    would otherwise get copied across the whole footprint.
    """
    new = [list(col) for col in matrix]
    pending = set(cleared)
    while pending:
        votes = {}
        for x, y in list(pending):
            for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                nx, ny = x + dx, y + dy
                if 1 <= nx <= width and 1 <= ny <= height and (nx, ny) not in pending:
                    votes.setdefault((x, y), Counter())[new[nx - 1][ny - 1]] += 1
        if not votes:
            break

        def resolve(cell, c, pool):
            # most votes wins; ties go to the tile that is commonest map-wide, and
            # then to the lowest id. That last key is what makes the result depend
            # only on the map: without it a tie is broken by whichever neighbour
            # happened to be counted first, i.e. by set iteration order.
            x, y = cell
            new[x - 1][y - 1] = max(pool, key=lambda t: (c[t], common.get(t, 0), -t))
            pending.discard(cell)

        # Cells are resolved from a snapshot of `votes`, so the order within a wave
        # cannot change the outcome -- sorted only to keep runs comparable.
        wave = sorted(votes.items())

        progressed = False
        for cell, c in wave:
            ground = [t for t in c if common.get(t, 0) >= MIN_COMMON]
            if ground:
                resolve(cell, c, ground)
                progressed = True
        if not progressed:
            # every reachable neighbour is one-off scenery; take the best of a
            # bad set rather than spinning
            for cell, c in wave:
                resolve(cell, c, list(c))
    # anything still pending is enclosed by cleared tiles only; leave it to the
    # caller's fallback rather than guessing
    return new, pending


# --------------------------------------------------------------------------
# serialisation: keep the file readable, one map column per line
# --------------------------------------------------------------------------

# The chapter's two maps: ShapeMatrix is the painted one the battle runs on,
# RenderMatrix the cleaned one ShapesLayer draws. Both print a column per line
# so they can be diffed against each other by eye.
MATRIX_KEYS = ('ShapeMatrix', 'RenderMatrix')


def dump_chapter(chapter, path):
    parts = []
    for key, value in chapter.items():
        if key in MATRIX_KEYS:
            rows = ',\n'.join('    [%s]' % ', '.join(str(v) for v in col) for col in value)
            parts.append('  "%s": [\n%s\n  ]' % (key, rows))
        else:
            body = json.dumps(value, indent=2, ensure_ascii=False)
            body = '\n'.join(('  ' + line) if i else line
                             for i, line in enumerate(body.split('\n')))
            parts.append('  "%s": %s' % (key, body))
    with open(path, 'w', encoding='utf-8') as f:
        f.write('{\n' + ',\n'.join(parts) + '\n}\n')


def insert_obstacles(chapter, obstacles):
    """Rebuild the chapter dict with "Obstacles" sitting before "ShapeMatrix"."""
    out = OrderedDict()
    for key, value in chapter.items():
        if key == 'Obstacles':
            continue
        if key == 'ShapeMatrix':
            out['Obstacles'] = obstacles
        out[key] = value
    if 'Obstacles' not in out:
        out['Obstacles'] = obstacles
    return out


def main():
    p = argparse.ArgumentParser(description=__doc__,
                                formatter_class=argparse.RawDescriptionHelpFormatter)
    p.add_argument('chapter')
    p.add_argument('--obstacles', required=True, help='obstacle list JSON (see module docstring)')
    p.add_argument('--root')
    p.add_argument('--chapter-json', help='override the source Chapter_NN.json')
    p.add_argument('--fill', type=int,
                   help='tile id to paint under the obstacles. Default: grow the '
                        'surrounding ground inward (see --fill-mode nearest)')
    p.add_argument('--fill-mode', choices=('nearest', 'tile'), default='nearest',
                   help='"nearest" (default) gives each cleared tile its nearest '
                        'surviving neighbour, so plazas stay paved and lawns stay '
                        'grass; "tile" paints one id everywhere')
    p.add_argument('-o', '--out', help='output path (default Chapter_NN_Cleaned.json '
                                       'next to the source)')
    p.add_argument('--dry-run', action='store_true', help='report only, write nothing')
    args = p.parse_args()

    root = args.root or voxlib.workspace_root()
    nn = voxlib.nn(args.chapter)
    src = args.chapter_json or voxlib.chapter_json_path(root, nn)
    with open(src, 'r', encoding='utf-8-sig') as f:
        chapter = json.load(f, object_pairs_hook=OrderedDict)

    # Cleaning always starts from the painted ShapeMatrix. If the source is an
    # already-installed chapter its RenderMatrix is the previous run's output --
    # drop it rather than carry a stale copy into the new one.
    chapter.pop('RenderMatrix', None)

    with open(args.obstacles, 'r', encoding='utf-8-sig') as f:
        obstacles = json.load(f)
    if isinstance(obstacles, dict):
        obstacles = obstacles['Obstacles']

    width, height = chapter['Width'], chapter['Height']
    matrix = chapter['ShapeMatrix']
    before = Counter()
    for col in matrix:
        before.update(col)

    footprints = []
    clean_list = []
    for i, o in enumerate(obstacles, start=1):
        key = o['DefinitionKey']
        cols, rows = obstacle_tile_size(root, key, o.get('Size'))
        x, y = int(o['Position']['X']), int(o['Position']['Y'])
        footprints.append((key, x, y, cols, rows))
        clean_list.append(OrderedDict([('Id', int(o.get('Id', i))),
                                       ('DefinitionKey', key),
                                       ('Position', OrderedDict([('X', x), ('Y', y)]))]))

    cleared, per = covered_tiles(width, height, footprints)
    total = len(cleared)

    # The default fill tile is the commonest tile that is NOT under an obstacle:
    # on a map that is mostly buildings, the overall histogram is dominated by
    # roof and wall tiles, which would be a nonsense choice of "ground".
    outside = Counter(matrix[x - 1][y - 1]
                      for x in range(1, width + 1) for y in range(1, height + 1)
                      if (x, y) not in cleared)
    fill = args.fill if args.fill is not None else outside.most_common(1)[0][0]

    if args.fill_mode == 'nearest' and args.fill is None:
        new_matrix, stranded = clean_matrix_nearest(matrix, width, height, cleared, outside)
        for x, y in stranded:
            new_matrix[x - 1][y - 1] = fill
        fill_desc = 'nearest surviving ground (%d tiles fell back to %d)' % (len(stranded), fill)
    else:
        new_matrix = clean_matrix(matrix, width, height, cleared, fill)
        fill_desc = 'tile %d everywhere' % fill

    after = Counter()
    for col in new_matrix:
        after.update(col)

    print('chapter %s  %dx%d tiles   source %s' % (nn, width, height, src))
    print('fill: %s   obstacles: %d   tiles cleared: %d' % (fill_desc, len(per), total))
    for key, x, y, cols, rows, n in per:
        off = '' if n == cols * rows else '   (%d of %d on board)' % (n, cols * rows)
        print('  %-22s X=%-4d Y=%-4d  %d cols x %d rows%s' % (key, x, y, cols, rows, off))

    dropped = sorted(set(before) - set(after))
    added = sorted(set(after) - set(before))
    print('distinct tiles: %d -> %d%s%s'
          % (len(before), len(after),
             '   no longer used: %s' % dropped if dropped else '',
             '   newly used: %s' % added if added else ''))

    # Footprint overlap is almost always a mistake in the obstacle list.
    seen = {}
    for key, x, y, cols, rows in footprints:
        for tx in range(x, x + cols):
            for ty in range(y, y + rows):
                if (tx, ty) in seen:
                    print('  !! %s overlaps %s at tile (%d, %d)' % (key, seen[(tx, ty)], tx, ty))
                seen[(tx, ty)] = key

    if args.dry_run:
        print('(dry run, nothing written)')
        return

    chapter['ShapeMatrix'] = new_matrix
    out = args.out or os.path.join(os.path.dirname(src), 'Chapter_%s_Cleaned.json' % nn)
    dump_chapter(insert_obstacles(chapter, clean_list), out)
    print('wrote %s' % out)

    used = os.path.join(os.path.dirname(out), 'Chapter_%s_UsedTiles.json' % nn)
    with open(used, 'w', encoding='utf-8') as f:
        json.dump({'chapter': nn,
                   'tile_ids': sorted(after),
                   'tile_counts': {str(k): v for k, v in sorted(after.items())}},
                  f, indent=2)
    print('wrote %s  (%d tile ids)' % (used, len(after)))


if __name__ == '__main__':
    main()
