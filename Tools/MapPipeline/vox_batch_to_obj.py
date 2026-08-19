"""Convert every .vox in a folder to .obj/.mtl/.png in the sibling "obj" folder.

Thin wrapper around Tools/Vox_to_Obj/vox_to_obj_exporter.py. That script writes
its three output files next to the source .vox; the project keeps them apart:

    <something>/vox/foo.vox   ->   <something>/obj/foo.obj + .mtl + .png

so this runs the exporter and moves the results. Export options match what the
committed chapter 01 models were built with -- scale 0.1, centred, grounded and
**Z-up** (no --y-up): ShapesLayer/ObstaclesLayer stand the model up themselves
with a parent Euler(90) plus an inner Euler(180).

    python vox_batch_to_obj.py --obstacles
    python vox_batch_to_obj.py --shapes 02
    python vox_batch_to_obj.py --in <dir>/vox --out <dir>/obj
"""

import argparse
import os
import shutil
import sys

import voxlib

EXPORTER_DIR = os.path.join(os.path.dirname(os.path.abspath(__file__)),
                            '..', 'Vox_to_Obj')


def load_exporter():
    sys.path.insert(0, os.path.abspath(EXPORTER_DIR))
    try:
        import vox_to_obj_exporter
    except ImportError as exc:
        raise SystemExit('could not import the exporter from %s: %s'
                         % (os.path.abspath(EXPORTER_DIR), exc))
    return vox_to_obj_exporter


def convert(in_dir, out_dir, scale, center, ground, y_up, force, dry_run):
    exporter = load_exporter()
    if not os.path.isdir(in_dir):
        raise SystemExit('no such folder: %s' % in_dir)
    vox_files = sorted(f for f in os.listdir(in_dir) if f.lower().endswith('.vox'))
    if not vox_files:
        raise SystemExit('no .vox files in %s' % in_dir)

    print('%s\n  -> %s\n  %d models   scale=%g center=%s ground=%s y_up=%s'
          % (in_dir, out_dir, len(vox_files), scale, center, ground, y_up))
    if not dry_run and not os.path.isdir(out_dir):
        os.makedirs(out_dir)

    done = skipped = 0
    for name in vox_files:
        stem = os.path.splitext(name)[0]
        dest_obj = os.path.join(out_dir, stem + '.obj')
        if os.path.isfile(dest_obj) and not force:
            print('  skip  %-28s obj exists (use --force)' % stem)
            skipped += 1
            continue
        if dry_run:
            print('  would %s' % stem)
            done += 1
            continue
        obj, mtl, png, nv, nq = exporter.exportVoxFile(
            os.path.join(in_dir, name), scale=scale, center=center,
            ground=ground, y_up=y_up)
        for produced in (obj, mtl, png):
            shutil.move(produced, os.path.join(out_dir, os.path.basename(produced)))
        print('  ok    %-28s %7d verts %6d quads' % (stem, nv, nq))
        done += 1

    print('%d converted, %d skipped' % (done, skipped))


def main():
    p = argparse.ArgumentParser(description=__doc__,
                                formatter_class=argparse.RawDescriptionHelpFormatter)
    p.add_argument('--root')
    g = p.add_mutually_exclusive_group(required=True)
    g.add_argument('--obstacles', action='store_true',
                   help='Resources/Remastered/Obstacles/vox -> ../obj')
    g.add_argument('--shapes', metavar='NN',
                   help='Resources/Remastered/Shapes/Shapes_NN/vox -> ../obj')
    g.add_argument('--in', dest='in_dir', help='explicit source folder of .vox files')
    p.add_argument('--out', help='explicit destination (default: sibling "obj" folder)')
    p.add_argument('--scale', type=float, default=0.1)
    p.add_argument('--no-center', action='store_true')
    p.add_argument('--no-ground', action='store_true')
    p.add_argument('--y-up', action='store_true', dest='y_up',
                   help='NOT used by this project -- the layers rotate models themselves')
    p.add_argument('--force', action='store_true', help='re-export models that already have an .obj')
    p.add_argument('--dry-run', action='store_true')
    args = p.parse_args()

    root = args.root or voxlib.workspace_root()
    if args.obstacles:
        in_dir = voxlib.obstacles_vox_dir(root)
    elif args.shapes:
        in_dir = voxlib.shapes_vox_dir(root, voxlib.nn(args.shapes))
    else:
        in_dir = os.path.abspath(args.in_dir)
    out_dir = args.out or os.path.join(os.path.dirname(os.path.normpath(in_dir)), 'obj')

    convert(in_dir, out_dir, args.scale, not args.no_center, not args.no_ground,
            args.y_up, args.force, args.dry_run)


if __name__ == '__main__':
    main()
