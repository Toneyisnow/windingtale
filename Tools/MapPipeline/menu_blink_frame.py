"""Make a menu item's second animation frame by moving its floating sparks.

A selected menu item blinks between Menu_NNN_1 and Menu_NNN_2 (MapObjects/Menus/Menu.cs,
MenuItemBlink). The menu meshes in Assets/Resources/Menus are one-voxel-per-column OBJs
(24 x 20 columns, each holding a single voxel at z = 0 for the flat plate or z = 1 for the
raised picture, exported as separate quads). Menu_210 (行军) had a second frame that was
practically the first one, so nothing seemed to move.

This writes Menu_210_2 as Menu_210_1 with the three isolated raised voxels -- the sparks
floating in the air above the picture -- put on other, random plate cells nearby. Nothing
else changes. The result is deterministic for a seed, and the two frames never share a spark
position, so every one of the three visibly jumps.

    python menu_blink_frame.py                       # Menu_210_1 -> Menu_210_2
    python menu_blink_frame.py --seed 7 --item 210
"""

import argparse
import os
import random
from collections import Counter

NORMALS = {
    (-1, 0, 0): (-1, 0, 0), (1, 0, 0): (1, 0, 0), (0, 0, 1): (0, 0, 1),
    (0, 0, -1): (0, 0, -1), (0, -1, 0): (0, -1, 0), (0, 1, 0): (0, 1, 0),
}

MENUS_DIR = os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', '..',
                         'WindingTale2', 'Assets', 'Resources', 'Menus')


def read_obj(path):
    text = open(path, encoding='utf-8', newline='').read().replace('\r\n', '\n')
    head = text[:text.index('# verts')]
    verts, vts, vns, faces = [], [], [], []
    for line in text.split('\n'):
        p = line.split()
        if not p:
            continue
        if p[0] == 'v':
            verts.append(tuple(int(float(t)) for t in p[1:4]))
        elif p[0] == 'vn':
            vns.append(tuple(int(float(t)) for t in p[1:4]))
        elif p[0] == 'f':
            faces.append([tuple(int(x) for x in t.split('/')) for t in p[1:]])
    return head, verts, vns, faces


def voxels_and_templates(verts, vns, faces):
    """The voxel grid (x, y, z) -> colour index, and per normal the corner order of a face
    as offsets from its voxel's minimum corner (the winding to copy for new faces)."""
    vox = {}
    templates = {}
    for f in faces:
        corners = [verts[i[0] - 1] for i in f]
        n = vns[f[0][2] - 1]
        mins = [min(c[i] for c in corners) for i in range(3)]
        maxs = [max(c[i] for c in corners) for i in range(3)]
        v = [mins[i] if n[i] <= 0 else maxs[i] - 1 for i in range(3)]
        vox[tuple(v)] = f[0][1]
        offsets = tuple(tuple(c[i] - v[i] for i in range(3)) for c in corners)
        assert templates.setdefault(n, offsets) == offsets, 'faces of one normal wind differently'
    return vox, templates


def sparks(vox):
    """The raised voxels with no raised neighbour (8-connected): the ones floating alone."""
    raised = set((x, y) for (x, y, z) in vox if z == 1)
    out = []
    for (x, y) in sorted(raised):
        if not any((x + dx, y + dy) in raised for dx in (-1, 0, 1) for dy in (-1, 0, 1) if dx or dy):
            out.append((x, y))
    return out


def move_sparks(vox, seed):
    old = sparks(vox)
    assert len(old) == 3, 'expected three floating sparks, found %r' % (old,)
    spark_colour = vox[(old[0][0], old[0][1], 1)]
    plate = Counter(c for (x, y, z), c in vox.items() if z == 0).most_common(1)[0][0]

    xs = [x for x, y in old]
    ys = [y for x, y in old]
    lo_x, hi_x = min(xs) - 4, max(xs) + 1
    lo_y, hi_y = min(ys) - 4, max(ys) + 4

    raised = set((x, y) for (x, y, z) in vox if z == 1)
    rng = random.Random(seed)
    for _ in range(1000):
        picks = []
        pool = [(x, y) for x in range(lo_x, hi_x + 1) for y in range(lo_y, hi_y + 1)
                if vox.get((x, y, 0)) == plate
                and all(abs(x - ox) > 1 or abs(y - oy) > 1 for ox, oy in old)]
        rng.shuffle(pool)
        for cell in pool:
            near_picture = any((cell[0] + dx, cell[1] + dy) in raised and (cell[0] + dx, cell[1] + dy) not in old
                               for dx in (-1, 0, 1) for dy in (-1, 0, 1))
            near_pick = any(abs(cell[0] - p[0]) <= 2 and abs(cell[1] - p[1]) <= 2 for p in picks)
            if near_picture or near_pick:
                continue
            picks.append(cell)
            if len(picks) == 3:
                break
        if len(picks) == 3:
            break
    else:
        raise SystemExit('no room for the sparks')

    new = dict(vox)
    for (x, y) in old:                      # the old sparks fall back to plate
        del new[(x, y, 1)]
        new[(x, y, 0)] = plate
    for (x, y) in picks:                    # and three new ones rise from the plate
        del new[(x, y, 0)]
        new[(x, y, 1)] = spark_colour
    return new, old, picks


def write_obj(path, head, vns, vox, templates):
    verts_out, faces_out = [], []
    for v in sorted(vox):
        for n, step in NORMALS.items():
            if (v[0] + step[0], v[1] + step[1], v[2] + step[2]) in vox:
                continue
            base = len(verts_out)
            for off in templates[n]:
                verts_out.append((v[0] + off[0], v[1] + off[1], v[2] + off[2]))
            ni = vns.index(n) + 1
            faces_out.append('f ' + ' '.join('%d/%d/%d' % (base + k + 1, vox[v], ni) for k in range(4)))
    lines = [head.rstrip('\n'), '# verts']
    lines += ['v %d %d %d' % v for v in verts_out]
    lines += ['', '# faces']
    lines += faces_out
    with open(path, 'w', encoding='utf-8', newline='') as f:
        f.write('\n'.join(lines) + '\n')
    return len(verts_out), len(faces_out)


def main():
    ap = argparse.ArgumentParser(description=__doc__,
                                 formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument('--item', default='210')
    ap.add_argument('--seed', type=int, default=2)
    ap.add_argument('--dir', default=MENUS_DIR)
    a = ap.parse_args()

    src = os.path.join(a.dir, 'Menu_%s_1.obj' % a.item)
    dst = os.path.join(a.dir, 'Menu_%s_2.obj' % a.item)
    head, verts, vns, faces = read_obj(src)
    vox, templates = voxels_and_templates(verts, vns, faces)
    new, old, picks = move_sparks(vox, a.seed)
    nv, nf = write_obj(dst, head, vns, new, templates)
    print('sparks %r -> %r' % (old, picks))
    print('%s: %d verts, %d faces' % (dst, nv, nf))


if __name__ == '__main__':
    main()
