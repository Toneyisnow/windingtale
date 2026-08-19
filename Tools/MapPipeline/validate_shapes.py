"""Regression-check shapes_to_vox.py against the hand-built chapter 01 models.

Shapes_01/vox was produced before this pipeline existed, so it is the ground
truth for every rule in ``voxlib`` and ``shapes_to_vox``. This regenerates each
tile from ShapePanel01 and diffs it voxel-for-voxel against the committed VOX.

    python validate_shapes.py               # all tiles that exist on both sides
    python validate_shapes.py --tiles 43,64 # just these, with a per-z breakdown
    python validate_shapes.py -v            # list every differing tile

The six tree tiles (46, 40, 43, 44, 47, 42) only match when their own crown is
stamped back on; the script does that for you.
"""

import argparse
import os
import re
from collections import Counter

import voxlib
import shapes_to_vox


def compare(generated, reference):
    a = Counter(generated)
    b = Counter(reference.voxels)
    only_a = sum((a - b).values())
    only_b = sum((b - a).values())
    return only_a, only_b


def main():
    p = argparse.ArgumentParser(description=__doc__,
                                formatter_class=argparse.RawDescriptionHelpFormatter)
    p.add_argument('--root')
    p.add_argument('--chapter', default='01')
    p.add_argument('--tiles', help='comma-separated subset')
    p.add_argument('-v', '--verbose', action='store_true')
    args = p.parse_args()

    root = args.root or voxlib.workspace_root()
    nn = voxlib.nn(args.chapter)
    ref_dir = voxlib.shapes_vox_dir(root, nn)
    panel = voxlib.shape_panel_dir(root, nn)

    if args.tiles:
        tiles = [int(t) for t in args.tiles.split(',')]
    else:
        pattern = re.compile(r'Shape_%d_(\d+)\.vox$' % int(nn))
        tiles = sorted(int(m.group(1)) for m in
                       (pattern.match(f) for f in os.listdir(ref_dir)) if m)

    templates = {tid: shapes_to_vox.load_tree_template(root, tid)
                 for tid in shapes_to_vox.REFERENCE_TREE_TILES}
    tree_by_tile = {tid: tid for tid in shapes_to_vox.REFERENCE_TREE_TILES}

    ok = bad = missing = 0
    failures = []
    for tid in tiles:
        ref_path = os.path.join(ref_dir, voxlib.shape_vox_name(nn, tid))
        png_path = os.path.join(panel, voxlib.tile_png_name(nn, tid))
        if not os.path.isfile(ref_path) or not os.path.isfile(png_path):
            missing += 1
            continue
        tree = tree_by_tile.get(tid)
        voxels, _src, _n = shapes_to_vox.build_tile(root, nn, tid, tree=tree,
                                                    templates=templates)
        reference = voxlib.read_vox(ref_path)
        extra, absent = compare(voxels, reference)
        if extra == 0 and absent == 0:
            ok += 1
            if args.tiles:
                print('  tile %-4d OK  (%d voxels)' % (tid, len(voxels)))
        else:
            bad += 1
            failures.append((tid, extra, absent, len(voxels), len(reference.voxels)))
            if args.verbose or args.tiles:
                print('  tile %-4d DIFF  generated-only %d, reference-only %d '
                      '(gen %d vs ref %d voxels)'
                      % (tid, extra, absent, len(voxels), len(reference.voxels)))

    print()
    print('chapter %s: %d tiles identical, %d differ, %d skipped (no PNG or no reference)'
          % (nn, ok, bad, missing))
    if failures and not (args.verbose or args.tiles):
        print('differing tiles: %s' % ', '.join(str(f[0]) for f in failures))
        print('re-run with -v for counts, or --tiles <id> for one tile')
    return 1 if bad else 0


if __name__ == '__main__':
    raise SystemExit(main())
