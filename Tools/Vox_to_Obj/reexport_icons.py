"""Re-export every character icon's OBJ from the .vox files already on disk.

``generate_icon.bat all`` rebuilds the .vox from the source PNGs first, which
is not what you want when only the *mesh* settings changed -- a rounding knob,
a normal threshold -- and the voxels themselves are fine. This does steps 2
and 3 of that script and nothing else:

    Resources/Remastered/Icons/NNN/Icon_NNN_FF.vox
      -> Resources/Remastered/Icons/NNN/smoothed/Icon_NNN_FF.{obj,mtl,png}
      -> WindingTale2/Assets/Resources/Icons/NNN/Icon_NNN_FF.{obj,mtl,png}

The hard-edged original next to the .vox is never touched: it stays as the
reference, the smoothed copy is what ships.

    python reexport_icons.py                 # every 3-digit icon folder
    python reexport_icons.py 001 002 007     # just these
    python reexport_icons.py --round-lambda 0.8 --no-deploy

Only folders named exactly three digits count -- 001_old and the loose sprite
sheets that sit alongside them are skipped.
"""

import argparse
import os
import re
import shutil
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import vox_to_obj_exporter as exporter  # noqa: E402

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(HERE, '..', '..'))
REMASTERED = os.path.join(ROOT, 'Resources', 'Remastered', 'Icons')
UNITY = os.path.join(ROOT, 'WindingTale2', 'Assets', 'Resources', 'Icons')

ICON_DIR = re.compile(r'^\d{3}$')
FRAMES = ('01', '02', '03')


def icon_ids(selected):
    if selected:
        return list(selected)
    return sorted(d for d in os.listdir(REMASTERED)
                  if ICON_DIR.match(d)
                  and os.path.isdir(os.path.join(REMASTERED, d)))


def main():
    parser = argparse.ArgumentParser(description=__doc__.split('\n')[0])
    parser.add_argument('icons', nargs='*', metavar='NNN',
                        help='icon ids to rebuild (default: all of them)')
    parser.add_argument('--round-lambda', type=float, default=0.65,
                        help='how far to round, 0..1 (default 0.65)')
    parser.add_argument('--round-iterations', type=int, default=1)
    parser.add_argument('--round-sharp', type=float, default=60.0)
    parser.add_argument('--no-deploy', action='store_true',
                        help='write smoothed/ but do not copy into the Unity project')
    args = parser.parse_args()

    built = skipped = 0
    for icon in icon_ids(args.icons):
        src = os.path.join(REMASTERED, icon)
        out = os.path.join(src, 'smoothed')
        unity = os.path.join(UNITY, icon)
        for frame in FRAMES:
            vox = os.path.join(src, 'Icon_%s_%s.vox' % (icon, frame))
            if not os.path.isfile(vox):
                print('  ! no vox:', os.path.basename(vox))
                skipped += 1
                continue
            obj, mtl, png, verts, faces = exporter.exportVoxFile(
                vox, scale=0.1, center=True, ground=True, y_up=True,
                round_edges=True, out_dir=out,
                round_lambda=args.round_lambda,
                round_iterations=args.round_iterations,
                round_sharp=args.round_sharp)
            built += 1
            if not args.no_deploy:
                if not os.path.isdir(unity):
                    print('  ! no Unity folder for icon', icon)
                    continue
                for path in (obj, mtl, png):
                    shutil.copy2(path, os.path.join(unity, os.path.basename(path)))
        print('%s  %d frames' % (icon, len(FRAMES)))

    print('\n%d models rebuilt, %d skipped, lambda=%g' %
          (built, skipped, args.round_lambda))


if __name__ == '__main__':
    main()
