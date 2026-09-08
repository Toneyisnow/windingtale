"""Greedy-meshed OBJ export for big voxel models.

``Tools/Vox_to_Obj/vox_to_obj_exporter.py`` writes one quad per exposed voxel
face, which is fine for a 40^3 tile and tolerable for a house (red_mansion_1 is
22 MB) but not for chapter 08's castle wall: 29 tiles of flat stone would come
out at 70 MB and nearly two million vertices. This module builds the same three
files -- ``.obj`` + ``.mtl`` + palette ``.png`` -- from a dense numpy grid, but
merges every run of same-coloured, coplanar faces into one rectangle first.

Everything the game relies on is kept identical to the exporter's output:

- the six normals in the same order, one ``vt`` per palette index at
  ``((c - 1) / 256 + 1 / 512, 0.5)``, quads written as ``f v/vt/vn``
- the vertex order of each face, so the winding (and so the lit side) matches
- ``scale 0.1``, centred on the model's bounding box in X and Y, grounded at the
  lowest voxel, Z-up -- the layers stand the model up themselves
- the ``.mtl`` and ``.png`` written by the exporter's own functions

The only visible difference is that a wall face is a handful of rectangles
instead of thousands of unit squares. Merged rectangles of different sizes meet
at T-junctions, which is normal for greedy voxel meshes and does not show at
the sizes the board is viewed at.

    python voxmesh.py <model.vox> [--out <dir>]

``vox_batch_to_obj.py`` routes every multi-part model (a SIZE over 256 on any
axis) through here automatically, and ``--greedy`` sends the rest.
"""

import argparse
import os
import sys

import numpy as np

import voxlib

EXPORTER_DIR = os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', 'Vox_to_Obj')

# AAQuad.normals in vox_to_obj_exporter.py, same order: left, right, top,
# bottom, front, back. "front" is -y, which is the map's south face.
NORMALS = ((-1, 0, 0), (1, 0, 0), (0, 0, 1), (0, 0, -1), (0, -1, 0), (0, 1, 0))


def _exporter():
    sys.path.insert(0, os.path.abspath(EXPORTER_DIR))
    import vox_to_obj_exporter
    return vox_to_obj_exporter


def grid_from_model(model):
    """Dense (sx, sy, sz) uint8 array of palette indices, 0 for empty."""
    g = np.zeros(model.size, dtype=np.uint8)
    if model.voxels:
        v = np.asarray(model.voxels, dtype=np.int64)
        g[v[:, 0], v[:, 1], v[:, 2]] = v[:, 3]
    return g


def greedy_rects(mask):
    """Cover the non-zero cells of a 2-D colour mask with maximal rectangles.

    Rows are taken top to bottom; each run of one colour along a row is grown
    downward while the rows below repeat it exactly. Returns
    ``[(row, col, height, width, colour)]``.
    """
    m = np.array(mask, copy=True)
    height, width = m.shape
    out = []
    for i in range(height):
        row = m[i]
        j = 0
        while True:
            nz = np.flatnonzero(row[j:])
            if nz.size == 0:
                break
            j += int(nz[0])
            c = row[j]
            diff = np.flatnonzero(row[j:] != c)
            k = j + int(diff[0]) if diff.size else width
            h = 1
            while i + h < height and np.all(m[i + h, j:k] == c):
                h += 1
            out.append((i, j, h, k - j, int(c)))
            m[i:i + h, j:k] = 0
            j = k
    return out


def _exposed(grid, axis, positive):
    """Colour of every voxel whose face on that side touches empty space."""
    solid = grid != 0
    shifted = np.zeros_like(solid)
    idx_src = [slice(None)] * 3
    idx_dst = [slice(None)] * 3
    if positive:
        idx_src[axis] = slice(1, None)
        idx_dst[axis] = slice(0, -1)
    else:
        idx_src[axis] = slice(0, -1)
        idx_dst[axis] = slice(1, None)
    shifted[tuple(idx_dst)] = solid[tuple(idx_src)]
    return np.where(solid & ~shifted, grid, 0)


def fill_interior(grid):
    """Seal every cavity that has no path to the outside of the grid.

    A model may be stored as a shell -- build_obstacles_08 writes the castle
    that way to keep the .vox small -- and a shell meshed as-is would get an
    inside surface. Cavities are filled with the colour of the voxel above
    them, which is never seen. Passages open to the outside (the gate) stay
    open. Uses scipy when it is installed and falls back to filling each
    column between its lowest and highest voxel, which is what
    housekit.Model.solidify does and is right for every building so far.
    """
    solid = grid != 0
    try:
        from scipy import ndimage
        inside = ndimage.binary_fill_holes(solid) & ~solid
    except ImportError:
        print('  (scipy not installed: sealing columns instead of true cavities)')
        has = solid.any(axis=2)
        top = np.where(has, grid.shape[2] - 1 - np.argmax(solid[:, :, ::-1], axis=2), -1)
        bottom = np.where(has, np.argmax(solid, axis=2), grid.shape[2])
        z = np.arange(grid.shape[2])[None, None, :]
        inside = has[:, :, None] & (z > bottom[:, :, None]) & (z < top[:, :, None]) & ~solid
    if not inside.any():
        return grid
    filled = grid.copy()
    # take the colour from the nearest solid voxel straight up the column
    for z in range(grid.shape[2] - 2, -1, -1):
        layer = inside[:, :, z]
        filled[:, :, z][layer] = filled[:, :, z + 1][layer]
    filled[inside & (filled == 0)] = 1
    return filled


def mesh(grid):
    """Greedy quads for the whole grid: ``[(side, colour, (v0, v1, v2, v3))]``.

    ``side`` indexes NORMALS. Vertex order per side is the exporter's, extended
    from a unit voxel to a rectangle. Enclosed cavities are sealed first.
    """
    grid = fill_interior(grid)
    quads = []

    # left / right: slices along x, mask over (y, z)
    for side, positive in ((0, False), (1, True)):
        exp = _exposed(grid, 0, positive)
        for x in np.flatnonzero(exp.any(axis=(1, 2))):
            X = int(x) + (1 if positive else 0)
            for y0, z0, dy, dz, c in greedy_rects(exp[x]):
                y1, z1 = y0 + dy, z0 + dz
                if positive:
                    v = ((X, y0, z1), (X, y0, z0), (X, y1, z0), (X, y1, z1))
                else:
                    v = ((X, y1, z1), (X, y1, z0), (X, y0, z0), (X, y0, z1))
                quads.append((side, c, v))

    # top / bottom: slices along z, mask over (x, y)
    for side, positive in ((2, True), (3, False)):
        exp = _exposed(grid, 2, positive)
        for z in np.flatnonzero(exp.any(axis=(0, 1))):
            Z = int(z) + (1 if positive else 0)
            for x0, y0, dx, dy, c in greedy_rects(exp[:, :, z]):
                x1, y1 = x0 + dx, y0 + dy
                if positive:
                    v = ((x0, y1, Z), (x0, y0, Z), (x1, y0, Z), (x1, y1, Z))
                else:
                    v = ((x0, y0, Z), (x0, y1, Z), (x1, y1, Z), (x1, y0, Z))
                quads.append((side, c, v))

    # front / back: slices along y, mask over (x, z)
    for side, positive in ((4, False), (5, True)):
        exp = _exposed(grid, 1, positive)
        for y in np.flatnonzero(exp.any(axis=(0, 2))):
            Y = int(y) + (1 if positive else 0)
            for x0, z0, dx, dz, c in greedy_rects(exp[:, y, :]):
                x1, z1 = x0 + dx, z0 + dz
                if positive:
                    v = ((x1, Y, z1), (x1, Y, z0), (x0, Y, z0), (x0, Y, z1))
                else:
                    v = ((x0, Y, z1), (x0, Y, z0), (x1, Y, z0), (x1, Y, z1))
                quads.append((side, c, v))

    return quads


def _fmt(x):
    s = ('%.6f' % x).rstrip('0').rstrip('.')
    return s if s else '0'


def write_obj(stream, quads, mtl_lib, material_name='palette',
              scale=0.1, center=True, ground=True):
    """The exporter's exportObjToStream, for merged quads."""
    colours = sorted(set(c for _s, c, _v in quads))
    uv_index = {c: i + 1 for i, c in enumerate(colours)}

    off_x = off_y = off_z = 0.0
    if quads and (center or ground):
        pts = np.array([p for _s, _c, v in quads for p in v], dtype=np.float64)
        if center:
            off_x = (pts[:, 0].min() + pts[:, 0].max()) / 2.0
            off_y = (pts[:, 1].min() + pts[:, 1].max()) / 2.0
        if ground:
            off_z = float(pts[:, 2].min())

    stream.write('# generated by voxmesh (greedy)\n')
    stream.write('# scale=%g center=%s ground=%s y_up=false\n'
                 % (scale, str(bool(center)).lower(), str(bool(ground)).lower()))
    stream.write('\n')
    stream.write('mtllib %s\n' % mtl_lib)
    stream.write('usemtl %s\n' % material_name)
    stream.write('\n')
    stream.write('# normals\n')
    for n in NORMALS:
        stream.write('vn %s %s %s\n' % (_fmt(n[0]), _fmt(n[1]), _fmt(n[2])))
    stream.write('\n')
    stream.write('# texcoords\n')
    for c in colours:
        stream.write('vt %s %s\n' % (str((c - 1) / 256 + 1 / 512), str(0.5)))
    stream.write('\n')
    stream.write('# verts\n')
    lines = []
    n_v = 0
    for side, c, v in quads:
        for p in v:
            stream.write('v %s %s %s\n' % (_fmt((p[0] - off_x) * scale),
                                           _fmt((p[1] - off_y) * scale),
                                           _fmt((p[2] - off_z) * scale)))
        uv, n = uv_index[c], side + 1
        lines.append('f %d/%d/%d %d/%d/%d %d/%d/%d %d/%d/%d\n'
                     % (n_v + 1, uv, n, n_v + 2, uv, n, n_v + 3, uv, n, n_v + 4, uv, n))
        n_v += 4
    stream.write('\n')
    stream.write('# faces\n')
    for line in lines:
        stream.write(line)
    stream.write('\n')
    return n_v, len(lines)


def export(vox_path, out_dir=None, material_name='palette', scale=0.1,
           center=True, ground=True):
    """Write <stem>.obj / .mtl / .png for one .vox. Returns the exporter's tuple."""
    exporter = _exporter()
    model = voxlib.read_vox(vox_path)
    stem = os.path.splitext(os.path.basename(vox_path))[0]
    out_dir = out_dir or os.path.dirname(os.path.abspath(vox_path))
    if not os.path.isdir(out_dir):
        os.makedirs(out_dir)
    obj_path = os.path.join(out_dir, stem + '.obj')
    mtl_path = os.path.join(out_dir, stem + '.mtl')
    png_path = os.path.join(out_dir, stem + '.png')

    quads = mesh(grid_from_model(model))
    with open(obj_path, 'w') as f:
        n_v, n_q = write_obj(f, quads, os.path.basename(mtl_path), material_name,
                             scale=scale, center=center, ground=ground)
    exporter.writeMtl(mtl_path, os.path.basename(png_path), material_name)
    exporter.writePalettePng(png_path, model.palette)
    return obj_path, mtl_path, png_path, n_v, n_q


def main():
    p = argparse.ArgumentParser(description=__doc__,
                                formatter_class=argparse.RawDescriptionHelpFormatter)
    p.add_argument('vox', nargs='+')
    p.add_argument('--out', help='destination folder (default: next to the .vox)')
    args = p.parse_args()
    for path in args.vox:
        obj, _mtl, _png, n_v, n_q = export(path, args.out)
        print('%s  %d verts  %d quads' % (obj, n_v, n_q))


if __name__ == '__main__':
    main()
