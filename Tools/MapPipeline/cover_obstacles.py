"""Lay a ground cover -- chapter 25's lava sheets -- over every tile of the
given ids, as an obstacle list for map_clean.py.

A cover is an obstacle that lies ON a tile instead of standing on it: one flat
model per tile *shape*, ``<prefix>_<tile id>`` (build_obstacles_25.py makes
``lava_25_4`` .. ``lava_25_50`` from the tile art), placed on every cell that
carries that tile. The tile itself stays painted -- the cover only adds the
raised, glowing, flickering sheet on top -- so each obstacle clears its cell to
its own tile id (``Clear`` with ``Fill`` = the tile), which leaves the
RenderMatrix exactly as it was.

    python cover_obstacles.py 25 --cover lava_25=4,5,6,7,8,9,10,11,12,13,14,15,50 \\
        --with obstacles/obstacles_25.json -o obstacles/obstacles_25_with_lava.json

``--with FILE`` merges an existing list in front; the covers take the ids
after it. Several ``--cover`` may be given.
"""

import argparse
import json
from collections import Counter, OrderedDict

import voxlib


def parse_covers(values):
    covers = []
    for v in values or []:
        prefix, _, ids = v.partition('=')
        if not ids:
            raise SystemExit('--cover expects PREFIX=id,id,...: %r' % v)
        covers.append((prefix.strip(), [int(t) for t in ids.split(',') if t]))
    return covers


def main():
    ap = argparse.ArgumentParser(description=__doc__.split('\n\n')[0],
                                 formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument('chapter')
    ap.add_argument('--root')
    ap.add_argument('--cover', action='append', required=True, help='PREFIX=id,id,... (repeatable)')
    ap.add_argument('--with', dest='with_file', help='obstacle list to merge in front')
    ap.add_argument('-o', '--out', required=True)
    a = ap.parse_args()

    root = a.root or voxlib.workspace_root()
    nn = '%02d' % int(a.chapter)
    chapter = json.load(open(voxlib.chapter_json_path(root, nn), encoding='utf-8'))
    width, height = chapter['Width'], chapter['Height']
    matrix = chapter['ShapeMatrix']

    obstacles = []
    if a.with_file:
        with_list = json.load(open(a.with_file, encoding='utf-8'))
        obstacles.extend(with_list['Obstacles'] if isinstance(with_list, dict) else with_list)
    next_id = max([int(o.get('Id', 0)) for o in obstacles] + [0]) + 1

    covers = parse_covers(a.cover)
    key_of = {}
    for prefix, ids in covers:
        for t in ids:
            key_of[t] = '%s_%d' % (prefix, t)

    per_key = Counter()
    for y in range(1, height + 1):
        for x in range(1, width + 1):
            t = matrix[x - 1][y - 1]
            if t not in key_of:
                continue
            obstacles.append(OrderedDict([
                ('Id', next_id), ('DefinitionKey', key_of[t]),
                ('Position', OrderedDict([('X', x), ('Y', y)])),
                ('Clear', [OrderedDict([('X', x), ('Y', y), ('Cols', 1), ('Rows', 1), ('Fill', t)])]),
            ]))
            next_id += 1
            per_key[key_of[t]] += 1

    out = OrderedDict([
        ('_comment', ['Chapter %s obstacle list -- input to map_clean.py.' % nn,
                      'Ground covers appended by cover_obstacles.py: ' +
                      '; '.join('%s_<id> over tiles %s' % (p, ','.join(map(str, ids))) for p, ids in covers) +
                      '. A cover lies on its tile and clears the cell to its own tile id, so the RenderMatrix keeps the painted tile under it.']),
        ('Obstacles', obstacles),
    ])
    with open(a.out, 'w', encoding='utf-8') as f:
        json.dump(out, f, indent=2)
        f.write('\n')
    print('wrote %s: %d obstacles  covers: %s' % (a.out, len(obstacles),
                                                 ', '.join('%s x%d' % kv for kv in sorted(per_key.items()))))


if __name__ == '__main__':
    main()
