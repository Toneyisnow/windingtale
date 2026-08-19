"""Install a cleaned chapter into the chapter the game actually loads.

Step 5 of the 2D -> 3D workflow, and the step that is easy to forget. Steps 2-4
only ever write ``Chapter_NN_Cleaned.json``; the game reads ``Chapter_NN.json``.

The two maps are not interchangeable, and the finished chapter needs both:

  * ``ShapeMatrix``  -- the **painted** map. Its tile ids carry the terrain the
    battle runs on (ShapeType -> move cost, AP/DP bonuses), so a tile a house
    was painted onto must stay Blocked/Forest even after the house has been
    lifted out into an obstacle model.
  * ``RenderMatrix`` -- the **cleaned** map. Only ``ShapesLayer`` reads it, to
    pick which tile model to draw, so the ground under a building goes back to
    plain grass and the building is drawn once, as its obstacle.

So this merges them: the cleaned file supplies the whole document (its
``Obstacles``, treasures, music) and becomes ``RenderMatrix``, while
``ShapeMatrix`` is taken from the original, pre-cleaning map.

Examples
--------
    # the normal case, straight after step 4
    python install_chapter.py 02

    # chapter 01 predates the pipeline: its Chapter_01.json is already the
    # cleaned map, and the original survives as the legacy dump
    python install_chapter.py 01 --original <chapters>/ChapterLegacy_01.txt \
        --cleaned <chapters>/Chapter_01.json

Re-running is safe: once ``Chapter_NN.json`` has a ``RenderMatrix`` its
``ShapeMatrix`` is already the original one, and that is what gets kept.
"""

import argparse
import json
import os
from collections import OrderedDict

import map_clean
import voxlib


def load(path):
    with open(path, 'r', encoding='utf-8-sig') as f:
        return json.load(f, object_pairs_hook=OrderedDict)


def matrix_ids(matrix):
    ids = set()
    for col in matrix:
        ids.update(col)
    return ids


def merge(cleaned, original_matrix):
    """Cleaned document + the painted ShapeMatrix, with RenderMatrix after it."""
    out = OrderedDict()
    for key, value in cleaned.items():
        if key == 'RenderMatrix':
            continue
        if key == 'ShapeMatrix':
            out['ShapeMatrix'] = original_matrix
            out['RenderMatrix'] = value
            continue
        out[key] = value
    return out


def check(chapter, nn, root):
    """Everything that would otherwise only show up as holes on screen."""
    problems = []

    shape_ids = set(int(k) for k in chapter['Shapes'])
    missing = sorted(matrix_ids(chapter['ShapeMatrix']) - shape_ids)
    if missing:
        problems.append('ShapeMatrix uses tile ids with no "Shapes" entry: %s' % missing)

    obj_dir = os.path.join(root, 'WindingTale2', 'Assets', 'Resources', 'Shapes',
                           'Shapes_%s' % nn)
    if os.path.isdir(obj_dir):
        have = set(int(f.split('_')[2].split('.')[0])
                   for f in os.listdir(obj_dir) if f.endswith('.obj'))
        missing = sorted(matrix_ids(chapter['RenderMatrix']) - have)
        if missing:
            problems.append('RenderMatrix uses tile ids with no OBJ in %s: %s'
                            % (obj_dir, missing))
    else:
        problems.append('no tile models at %s -- run step 4 first' % obj_dir)

    obstacle_dir = os.path.join(root, 'WindingTale2', 'Assets', 'Resources', 'Obstacles')
    if os.path.isdir(obstacle_dir):
        have = set(f[:-4] for f in os.listdir(obstacle_dir) if f.endswith('.obj'))
        missing = sorted(set(o['DefinitionKey'] for o in chapter.get('Obstacles', [])) - have)
        if missing:
            problems.append('obstacles with no OBJ in %s: %s' % (obstacle_dir, missing))

    return problems


def main():
    p = argparse.ArgumentParser(description=__doc__,
                                formatter_class=argparse.RawDescriptionHelpFormatter)
    p.add_argument('chapter')
    p.add_argument('--root')
    p.add_argument('--original', help='map to take ShapeMatrix from '
                                      '(default: the current Chapter_NN.json)')
    p.add_argument('--cleaned', help='map to take everything else from '
                                     '(default: Chapter_NN_Cleaned.json)')
    p.add_argument('-o', '--out', help='output path (default: Chapter_NN.json)')
    p.add_argument('--dry-run', action='store_true')
    args = p.parse_args()

    root = args.root or voxlib.workspace_root()
    nn = voxlib.nn(args.chapter)
    dest = voxlib.chapter_json_path(root, nn)
    chapters = os.path.dirname(dest)

    original_path = args.original or dest
    cleaned_path = args.cleaned or os.path.join(chapters, 'Chapter_%s_Cleaned.json' % nn)
    for path in (original_path, cleaned_path):
        if not os.path.isfile(path):
            raise SystemExit('not found: %s' % path)

    original = load(original_path)
    cleaned = load(cleaned_path)

    # Re-run: Chapter_NN.json's ShapeMatrix is already the painted map.
    painted = original['ShapeMatrix']
    render = cleaned['ShapeMatrix']
    if len(painted) != len(render) or len(painted[0]) != len(render[0]):
        raise SystemExit('map sizes differ: %dx%d vs %dx%d'
                         % (len(painted), len(painted[0]), len(render), len(render[0])))

    merged = merge(cleaned, painted)
    cleared = sum(1 for x in range(len(painted)) for y in range(len(painted[0]))
                  if painted[x][y] != render[x][y])

    print('chapter %s' % nn)
    print('  ShapeMatrix  <- %s   %d tile ids' % (original_path, len(matrix_ids(painted))))
    print('  RenderMatrix <- %s   %d tile ids' % (cleaned_path, len(matrix_ids(render))))
    print('  %d tiles cleared by %d obstacles' % (cleared, len(merged.get('Obstacles', []))))

    problems = check(merged, nn, root)
    for problem in problems:
        print('  !! %s' % problem)

    out = args.out or dest
    if args.dry_run:
        print('  would write %s' % out)
        return
    if problems:
        raise SystemExit('  refusing to install -- fix the above first')

    map_clean.dump_chapter(merged, out)
    print('  wrote %s' % out)


if __name__ == '__main__':
    main()
