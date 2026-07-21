"""
build_treasures.py
------------------

Build every treasure-chest variant end to end:

    treasure_to_vox.py  ->  .vox
    vox_to_obj_exporter ->  .obj / .mtl / .png
    align + copy        ->  WindingTale2/Assets/Resources/Shapes/Treasures/

    python build_treasures.py

Why this exists rather than running the two tools by hand: the *opened* models
need a different export invocation from the closed ones. The exporter's
``center=true`` centres on the model's bounding box, but an opened chest's bbox
includes the swung-back lid -- so centring it would shift the *base* off the
tile centre, and the open/closed pair would jump when swapped in game.

So opened models are exported with ``--no-center --no-ground`` and then
translated by hand onto the same origin the closed models get:

    world = voxel * SCALE - HALF_FOOTPRINT

which keeps every variant, open or closed, sharing one origin.
"""

import os
import shutil
import subprocess
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(HERE, '..', '..'))

VOX_DIR = os.path.join(ROOT, 'Resources', 'Remastered', 'Others')
EXPORTER = os.path.join(ROOT, 'Tools', 'Vox_to_Obj', 'vox_to_obj_exporter.py')
UNITY_DIR = os.path.join(ROOT, 'WindingTale2', 'Assets', 'Resources',
                         'Shapes', 'Treasures')

SCALE = 0.1         # must match the exporter default
FOOTPRINT = 24      # voxels; the closed models centre on this
ORIGIN = FOOTPRINT * SCALE / 2.0    # 1.2

THEMES = ['01', '02']
SIDECARS = ('.obj', '.mtl', '.png')


def run(*args):
    subprocess.run([sys.executable, *args], check=True)


def align_obj(path):
    """Translate an OBJ exported with --no-center onto the shared origin."""
    lines = []
    for line in open(path):
        if line.startswith('v '):
            _, x, y, z = line.split()
            lines.append('v {:.6f} {:.6f} {:.6f}\n'.format(
                float(x) - ORIGIN, float(y) - ORIGIN, float(z)))
        else:
            lines.append(line)
    open(path, 'w').writelines(lines)


def bounds(path):
    vs = [l.split()[1:4] for l in open(path) if l.startswith('v ')]
    return tuple((round(min(float(v[i]) for v in vs), 3),
                  round(max(float(v[i]) for v in vs), 3)) for i in range(3))


def main():
    os.makedirs(UNITY_DIR, exist_ok=True)
    gen = os.path.join(HERE, 'treasure_to_vox.py')

    for theme in THEMES:
        for opened in (False, True):
            name = 'TreasureBox_{}{}'.format(theme, '_Opened' if opened else '')
            vox = os.path.join(VOX_DIR, name + '.vox')

            gen_args = [gen, '-t', theme]
            if opened:
                gen_args.append('--open')
            run(*gen_args)

            obj = os.path.join(VOX_DIR, name + '.obj')
            if opened:
                # bbox includes the swung lid -> centre it ourselves
                run(EXPORTER, vox, '--no-center', '--no-ground')
                align_obj(obj)
            else:
                run(EXPORTER, vox)

            for ext in SIDECARS:
                shutil.copy2(os.path.join(VOX_DIR, name + ext), UNITY_DIR)

            bx, by, bz = bounds(obj)
            print('  {:24} x {}  y {}  z {}'.format(name, bx, by, bz))

    print('\ncopied to', UNITY_DIR)


if __name__ == '__main__':
    main()
