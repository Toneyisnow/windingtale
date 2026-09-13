"""Turn a voxel model into a *rounded, smooth-shaded* OBJ mesh.

The plain export (``vox_to_obj_exporter``) emits one flat axis-aligned quad
per exposed voxel face with one of six flat normals. On a 24-voxel character
that reads as a pile of hard cubes: every edge is a perfect 90 degrees and
every face is one single shade, so a face is either lit or not and nothing
in between.

This module fixes both, without adding a single triangle:

  1. **Weld.**  The blocky surface is rebuilt with shared vertices, so it is
     one connected mesh rather than 2500 loose quads.

  2. **Relax.**  One (or a few) passes of uniform Laplacian smoothing move
     every vertex a fraction of the way toward the average of its neighbours.
     The arithmetic does exactly what we want and nothing else:

       - a vertex in the middle of a flat wall has 4 symmetric neighbours,
         its average IS itself, and it does not move -- flat stays flat;
       - a vertex on a convex 90-degree edge is pulled diagonally inward,
         which cuts that edge into a small chamfer;
       - a vertex in a concave crease is pushed outward, filling it slightly.

     So only the *outer edges* round off, which is the whole request. Because
     the mesh is welded and only vertex positions change, the result cannot
     crack open the way an explicit per-voxel bevel does when two voxels meet
     only along an edge.

     ``lam`` is how far to move (1.0 = all the way to the neighbour average)
     and is the knob for "how round". A 1-voxel-thick limb loses ``lam/2`` of
     its thickness, which is what puts a ceiling on it: at 0.8 a helmet starts
     turning into a dome and small plates wash out, so the default is 0.65.

  3. **Smooth normals.**  Each face corner gets the average of the normals of
     the faces meeting at that vertex, but only those within ``sharp``
     degrees of its own -- so the new chamfer facets blend into their
     neighbours (the light gradient runs across the face instead of the face
     being one flat shade) while a genuine 90-degree edge that survived the
     relax still reads as an edge.

Used by ``vox_to_obj_exporter.py --round``; see that script for the CLI.
"""

import math
from collections import defaultdict

# Outward face directions, and the 4 corners of that face of the unit cube at
# the origin, wound counter-clockwise seen from outside. Same convention (and
# same order) as AAQuad.normals in vox_to_obj_exporter.
FACES = [
    ((-1, 0, 0), ((0, 1, 1), (0, 1, 0), (0, 0, 0), (0, 0, 1))),   # left
    ((1, 0, 0), ((1, 0, 1), (1, 0, 0), (1, 1, 0), (1, 1, 1))),    # right
    ((0, 0, 1), ((0, 1, 1), (0, 0, 1), (1, 0, 1), (1, 1, 1))),    # top
    ((0, 0, -1), ((0, 0, 0), (0, 1, 0), (1, 1, 0), (1, 0, 0))),   # bottom
    ((0, -1, 0), ((0, 0, 1), (0, 0, 0), (1, 0, 0), (1, 0, 1))),   # front
    ((0, 1, 0), ((1, 1, 1), (1, 1, 0), (0, 1, 0), (0, 1, 1))),    # back
]


class RoundedMesh:
    """A welded, relaxed, smooth-normal surface built from a VoxelStruct."""

    def __init__(self, vox, lam=0.65, iterations=1, sharp_degrees=60.0,
                 max_shift=0.5):
        self.positions = []       # welded vertex positions, floats
        self.quads = []           # (i0, i1, i2, i3, colorIndex)
        self._build(vox)
        self.relax(lam, iterations, max_shift)
        self.corner_normals = self._corner_normals(sharp_degrees)

    # ---------------------------------------------------------------- build

    def _build(self, vox):
        """Collect every exposed voxel face, welding shared lattice corners."""
        solid = {}
        for voxel in vox.voxels.values():
            if voxel.colorIndex != 0:
                solid[(voxel.x, voxel.y, voxel.z)] = voxel.colorIndex

        index_of = {}

        def weld(p):
            i = index_of.get(p)
            if i is None:
                i = len(self.positions)
                index_of[p] = i
                self.positions.append([float(p[0]), float(p[1]), float(p[2])])
            return i

        for (x, y, z), color in sorted(solid.items()):
            for (dx, dy, dz), corners in FACES:
                if (x + dx, y + dy, z + dz) in solid:
                    continue
                ring = tuple(weld((x + cx, y + cy, z + cz))
                             for cx, cy, cz in corners)
                self.quads.append(ring + (color,))

    def _vertex_neighbours(self):
        adjacency = defaultdict(set)
        for quad in self.quads:
            ring = quad[:4]
            for k in range(4):
                a, b = ring[k], ring[(k + 1) % 4]
                adjacency[a].add(b)
                adjacency[b].add(a)
        return adjacency

    # ---------------------------------------------------------------- relax

    def relax(self, lam, iterations, max_shift):
        """Uniform Laplacian smoothing, capped so nothing collapses."""
        if lam <= 0 or iterations <= 0:
            return
        adjacency = self._vertex_neighbours()
        origin = [tuple(p) for p in self.positions]
        for _ in range(int(iterations)):
            moved = []
            for i, p in enumerate(self.positions):
                nbrs = adjacency.get(i)
                if not nbrs:
                    moved.append(list(p))
                    continue
                inv = 1.0 / len(nbrs)
                target = [sum(self.positions[n][axis] for n in nbrs) * inv
                          for axis in range(3)]
                moved.append([p[axis] + lam * (target[axis] - p[axis])
                              for axis in range(3)])
            self.positions = moved
        # never let a vertex wander further than max_shift from where the
        # voxel grid put it: keeps thin limbs and faces from melting away.
        if max_shift is not None:
            for i, p in enumerate(self.positions):
                o = origin[i]
                d = [p[axis] - o[axis] for axis in range(3)]
                length = math.sqrt(d[0] * d[0] + d[1] * d[1] + d[2] * d[2])
                if length > max_shift:
                    k = max_shift / length
                    self.positions[i] = [o[axis] + d[axis] * k
                                         for axis in range(3)]

    # -------------------------------------------------------------- normals

    def face_normals(self):
        """Newell normal per quad, from the relaxed positions."""
        normals = []
        for quad in self.quads:
            ring = quad[:4]
            nx = ny = nz = 0.0
            for k in range(4):
                a = self.positions[ring[k]]
                b = self.positions[ring[(k + 1) % 4]]
                nx += (a[1] - b[1]) * (a[2] + b[2])
                ny += (a[2] - b[2]) * (a[0] + b[0])
                nz += (a[0] - b[0]) * (a[1] + b[1])
            length = math.sqrt(nx * nx + ny * ny + nz * nz) or 1.0
            normals.append((nx / length, ny / length, nz / length))
        return normals

    def _corner_normals(self, sharp_degrees):
        """--> [(n0, n1, n2, n3), ...], one normal per quad corner.

        A corner averages the faces around its vertex, but only the ones
        within `sharp_degrees` of the corner's own face -- an edge sharper
        than that stays an edge instead of being smeared over.
        """
        fnormals = self.face_normals()
        # unnormalised area weight: the Newell vector's length is ~2x the area
        around = defaultdict(list)
        for f, quad in enumerate(self.quads):
            for v in quad[:4]:
                around[v].append(f)

        limit = math.cos(math.radians(sharp_degrees))
        out = []
        for f, quad in enumerate(self.quads):
            own = fnormals[f]
            ring = []
            for v in quad[:4]:
                sx = sy = sz = 0.0
                for g in around[v]:
                    n = fnormals[g]
                    if (n[0] * own[0] + n[1] * own[1] + n[2] * own[2]) < limit:
                        continue
                    sx += n[0]
                    sy += n[1]
                    sz += n[2]
                length = math.sqrt(sx * sx + sy * sy + sz * sz)
                ring.append(own if length < 1e-9
                            else (sx / length, sy / length, sz / length))
            out.append(tuple(ring))
        return out

    # --------------------------------------------------------------- output

    def bounds(self):
        if not self.positions:
            # a few icons (758-761) have an empty .vox; the flat exporter
            # writes an empty .obj for those, so match it instead of dividing
            # by a bounding box that does not exist.
            return [0.0, 0.0, 0.0], [0.0, 0.0, 0.0]
        lo = [min(p[axis] for p in self.positions) for axis in range(3)]
        hi = [max(p[axis] for p in self.positions) for axis in range(3)]
        return lo, hi


def _fmt(x):
    s = ('%.5f' % x).rstrip('0').rstrip('.')
    return s if s else '0'


def write_obj(stream, mesh, mtl_lib=None, material_name=None,
              scale=1.0, center=False, ground=False, y_up=False):
    """Write `mesh` as an OBJ, with the same world transform as the flat
    exporter's exportObjToStream() so a rounded model drops straight into
    the place the blocky one occupied.

    Returns (n_vertices, n_triangles).
    """
    lo, hi = mesh.bounds()
    off = [0.0, 0.0, 0.0]
    if center:
        off[0] = (lo[0] + hi[0]) / 2.0
        off[1] = (lo[1] + hi[1]) / 2.0
    if ground:
        off[2] = lo[2]

    def xform_vert(p):
        v = ((p[0] - off[0]) * scale,
             (p[1] - off[1]) * scale,
             (p[2] - off[2]) * scale)
        return (v[0], v[2], -v[1]) if y_up else v

    def xform_normal(n):
        return (n[0], n[2], -n[1]) if y_up else n

    # one vt per palette colour actually used
    uv_index = {}
    uvs = []
    for quad in mesh.quads:
        color = quad[4]
        if color not in uv_index:
            uv_index[color] = len(uvs)
            uvs.append(((color - 1) / 256.0 + 1 / 512.0, 0.5))

    # dedupe normals: a mesh this size has far fewer distinct ones than corners
    normal_index = {}
    normals = []

    def normal_id(n):
        key = (round(n[0], 4), round(n[1], 4), round(n[2], 4))
        i = normal_index.get(key)
        if i is None:
            i = len(normals)
            normal_index[key] = i
            normals.append(key)
        return i

    face_lines = []
    for f, quad in enumerate(mesh.quads):
        ring = quad[:4]
        uv = uv_index[quad[4]] + 1
        ns = [normal_id(xform_normal(n)) + 1 for n in mesh.corner_normals[f]]
        # split the quad along its shorter diagonal: after the relax a quad
        # near an edge is no longer planar, and the short diagonal is the
        # split that keeps the surface from creasing.
        p = [mesh.positions[i] for i in ring]

        def d2(a, b):
            return sum((p[a][k] - p[b][k]) ** 2 for k in range(3))

        if d2(0, 2) <= d2(1, 3):
            tris = ((0, 1, 2), (0, 2, 3))
        else:
            tris = ((1, 2, 3), (1, 3, 0))
        for tri in tris:
            face_lines.append('f ' + ' '.join(
                '%d/%d/%d' % (ring[k] + 1, uv, ns[k]) for k in tri) + '\n')

    stream.write('# generated by vox_to_obj_exporter --round\n')
    stream.write('# scale=%g center=%s ground=%s y_up=%s\n' %
                 (scale, str(bool(center)).lower(), str(bool(ground)).lower(),
                  str(bool(y_up)).lower()))
    stream.write('# welded, edge-relaxed, smooth vertex normals\n')
    stream.write('\n')
    if mtl_lib:
        stream.write('mtllib %s\n' % mtl_lib)
    if material_name:
        stream.write('usemtl %s\n' % material_name)
    if mtl_lib or material_name:
        stream.write('\n')

    stream.write('# verts\n')
    for p in mesh.positions:
        v = xform_vert(p)
        stream.write('v %s %s %s\n' % (_fmt(v[0]), _fmt(v[1]), _fmt(v[2])))
    stream.write('\n# texcoords\n')
    for uv in uvs:
        stream.write('vt %s %s\n' % (_fmt(uv[0]), _fmt(uv[1])))
    stream.write('\n# normals\n')
    for n in normals:
        stream.write('vn %s %s %s\n' % (_fmt(n[0]), _fmt(n[1]), _fmt(n[2])))
    stream.write('\n# faces\n')
    for line in face_lines:
        stream.write(line)
    stream.write('\n')
    return len(mesh.positions), len(face_lines)
