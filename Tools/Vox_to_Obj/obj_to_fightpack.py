"""
Pack one creature's fight OBJs (Resources/Fights3D/NNN/*.obj) into a single
Fight_NNN_mesh.bytes that FightModel3D loads instead of the OBJs.

Why: the frames of one animation are mostly the same drawing, so most triangles
appear in many OBJs (only ~1/3 of a creature's triangles are unique), and OBJ text
is ~5x bigger than the same mesh in binary.

How:
  * every corner (position, uv, normal) of every model goes into one shared vertex
    pool; every distinct triangle is stored once.
  * each triangle is tagged with the set of models that use it; triangles with the
    same set form a "group", stored contiguously. A model is the list of its groups.
  * positions are quantised to uint16 over the creature's bounding box (error well
    under 1/100 voxel), normals to int8 x3, uvs are an index into the few distinct
    texture coordinates (the palette columns).
  * columns are delta coded and the whole payload is raw-deflated (DeflateStream).

Layout (little endian, after inflating):
  char[4] "WTFM", u16 version=1
  u16 uvCount, uvCount * (f32 u, f32 v)
  f32[3] boundsMin, f32[3] step
  u32 vertexCount
    3 * vertexCount u16 : x column, y column, z column (each delta coded, wrapping)
    3 * vertexCount i8  : nx, ny, nz columns
    vertexCount u8|u16  : uv index (u8 when uvCount <= 256)
  u32 triangleCount
    3 * triangleCount varint : zigzag(index - previous index), corner by corner
  u32 groupCount, groupCount * u32 triangles in group (groups are consecutive)
  u16 modelCount, per model: u8 nameLength, name (utf8, no extension),
                             u32 groupRefCount, groupRefCount * varint group id

Usage:
  python obj_to_fightpack.py <Fights3D dir> <id> [<id> ...] | all
                             [--verify] [--delete-obj] [--jobs N]
  --verify      decode the pack and check every model against its OBJ
  --delete-obj  then delete the OBJs (+ .meta) -- only for creatures that verified
                (implies --verify); FightModel3D loads the pack instead
  Creatures without OBJs (already packed) are skipped.
"""

import glob
import os
import struct
import sys
import zlib

import numpy as np

MAGIC = b"WTFM"
VERSION = 1


def read_obj(path):
    vs, vts, vns, tris = [], [], [], []
    with open(path) as f:
        for line in f:
            if line.startswith("v "):
                vs.append(tuple(float(x) for x in line.split()[1:4]))
            elif line.startswith("vt "):
                vts.append(tuple(float(x) for x in line.split()[1:3]))
            elif line.startswith("vn "):
                vns.append(tuple(float(x) for x in line.split()[1:4]))
            elif line.startswith("f "):
                corners = []
                for part in line.split()[1:]:
                    p = part.split("/")
                    v = int(p[0]) - 1
                    t = int(p[1]) - 1 if len(p) > 1 and p[1] else -1
                    n = int(p[2]) - 1 if len(p) > 2 and p[2] else -1
                    corners.append((v, t, n))
                for i in range(1, len(corners) - 1):   # fan, in case of quads
                    tris.append((corners[0], corners[i], corners[i + 1]))
    return vs, vts, vns, tris


def varint(n, out):
    while True:
        b = n & 0x7F
        n >>= 7
        if n:
            out.append(b | 0x80)
        else:
            out.append(b)
            return


def zigzag(n):
    return (n << 1) ^ (n >> 63)


def quant_normal(n):
    l = (n[0] * n[0] + n[1] * n[1] + n[2] * n[2]) ** 0.5 or 1.0
    return tuple(max(-127, min(127, int(round(c / l * 127)))) for c in n)


def pack(folder, creature):
    objs = sorted(glob.glob(os.path.join(folder, "*.obj")))
    names = [os.path.splitext(os.path.basename(p))[0] for p in objs]

    uv_ids = {}          # (u, v) -> id
    corner_ids = {}      # (pos, uvId, quantised normal) -> pool vertex id
    pool = []            # [(pos, uvId, nq)]
    tri_ids = {}         # (a, b, c) -> triangle id
    tri_users = []       # triangle id -> bitmask of models
    model_tris = []      # per model, its triangle ids in order (for verification)

    for m, path in enumerate(objs):
        vs, vts, vns, tris = read_obj(path)
        mine = []
        for tri in tris:
            key = []
            for v, t, n in tri:
                uv = vts[t] if t >= 0 else (0.0, 0.0)
                u = uv_ids.setdefault(uv, len(uv_ids))
                nq = quant_normal(vns[n]) if n >= 0 else (0, 127, 0)
                ck = (vs[v], u, nq)
                cid = corner_ids.get(ck)
                if cid is None:
                    cid = corner_ids[ck] = len(pool)
                    pool.append(ck)
                key.append(cid)
            key = tuple(key)
            tid = tri_ids.get(key)
            if tid is None:
                tid = tri_ids[key] = len(tri_users)
                tri_users.append(0)
            tri_users[tid] |= 1 << m
            mine.append(tid)
        model_tris.append(mine)

    # group triangles by the set of models using them
    tri_list = list(tri_ids.keys())
    order = sorted(range(len(tri_list)), key=lambda t: (tri_users[t], t))
    groups = []          # [(mask, count)]
    for t in order:
        if groups and groups[-1][0] == tri_users[t]:
            groups[-1][1] += 1
        else:
            groups.append([tri_users[t], 1])

    # renumber vertices in first-use order of the grouped triangle stream
    remap = {}
    stream = []
    for t in order:
        for c in tri_list[t]:
            if c not in remap:
                remap[c] = len(remap)
            stream.append(remap[c])
    verts = [None] * len(remap)
    for old, new in remap.items():
        verts[new] = pool[old]

    P = np.array([v[0] for v in verts], dtype=np.float64)
    lo = P.min(axis=0)
    hi = P.max(axis=0)
    step = np.where(hi > lo, (hi - lo) / 65535.0, 1.0)
    # quantise with exactly the float32 values the file stores, so decode == verify
    lo = lo.astype(np.float32).astype(np.float64)
    step = step.astype(np.float32).astype(np.float64)
    Q = np.round((P - lo) / step).astype(np.int64).clip(0, 65535).astype(np.uint16)
    N = np.array([v[2] for v in verts], dtype=np.int8)
    U = np.array([v[1] for v in verts])

    out = bytearray()
    out += MAGIC + struct.pack("<H", VERSION)
    uvs = sorted(uv_ids.items(), key=lambda kv: kv[1])
    out += struct.pack("<H", len(uvs))
    for (u, v), _ in uvs:
        out += struct.pack("<ff", u, v)
    out += struct.pack("<3f3f", *lo.astype(np.float32), *step.astype(np.float32))
    out += struct.pack("<I", len(verts))
    for axis in range(3):
        col = Q[:, axis].astype(np.int64)
        d = np.diff(col, prepend=0) & 0xFFFF
        out += d.astype("<u2").tobytes()
    for axis in range(3):
        out += N[:, axis].tobytes()
    out += U.astype(np.uint8 if len(uvs) <= 256 else "<u2").tobytes()

    out += struct.pack("<I", len(order))
    prev = 0
    idx = bytearray()
    for i in stream:
        varint(zigzag(i - prev), idx)
        prev = i
    out += idx

    out += struct.pack("<I", len(groups))
    for _, count in groups:
        out += struct.pack("<I", count)
    out += struct.pack("<H", len(names))
    for m, name in enumerate(names):
        nb = name.encode("utf8")
        out += struct.pack("<B", len(nb)) + nb
        refs = [g for g, (mask, _) in enumerate(groups) if mask >> m & 1]
        out += struct.pack("<I", len(refs))
        rb = bytearray()
        prev = 0
        for g in refs:
            varint(g - prev, rb)
            prev = g
        out += rb

    comp = zlib.compressobj(9, zlib.DEFLATED, -15)
    data = comp.compress(bytes(out)) + comp.flush()
    dest = os.path.join(folder, "Fight_%s_mesh.bytes" % creature)
    with open(dest, "wb") as f:
        f.write(data)

    stats = dict(models=len(names), tris_total=sum(len(t) for t in model_tris),
                 tris_unique=len(tri_list), verts=len(verts), groups=len(groups),
                 raw=len(out), packed=len(data),
                 obj_bytes=sum(os.path.getsize(p) for p in objs))
    return dest, stats


# ---------------------------------------------------------------- decoding / check

def read_varint(buf, pos):
    n = shift = 0
    while True:
        b = buf[pos]
        pos += 1
        n |= (b & 0x7F) << shift
        if not b & 0x80:
            return n, pos
        shift += 7


def unpack(path):
    """Mirror of FightMeshPack.cs. Returns (lo, step, {name: set of triangles}), a
    triangle being three corners (qx, qy, qz, u, v, nx, ny, nz) on the quantised grid."""
    buf = zlib.decompress(open(path, "rb").read(), -15)
    assert buf[:4] == MAGIC
    pos = 6
    (uvc,) = struct.unpack_from("<H", buf, pos); pos += 2
    uvs = [struct.unpack_from("<ff", buf, pos + 8 * i) for i in range(uvc)]; pos += 8 * uvc
    lo = np.array(struct.unpack_from("<3f", buf, pos)); pos += 12
    step = np.array(struct.unpack_from("<3f", buf, pos)); pos += 12
    (vc,) = struct.unpack_from("<I", buf, pos); pos += 4
    cols = []
    for _ in range(3):
        d = np.frombuffer(buf, "<u2", vc, pos).astype(np.int64); pos += 2 * vc
        cols.append(np.cumsum(d) & 0xFFFF)
    Q = np.stack(cols, 1)
    N = np.stack([np.frombuffer(buf, np.int8, vc, pos + vc * a) for a in range(3)], 1)
    pos += 3 * vc
    if uvc <= 256:
        U = np.frombuffer(buf, np.uint8, vc, pos); pos += vc
    else:
        U = np.frombuffer(buf, "<u2", vc, pos); pos += 2 * vc
    corners = [tuple(int(c) for c in Q[i]) + uvs[U[i]] + tuple(int(c) for c in N[i]) for i in range(vc)]
    (tc,) = struct.unpack_from("<I", buf, pos); pos += 4
    idx = []
    prev = 0
    for _ in range(3 * tc):
        z, pos = read_varint(buf, pos)
        prev += (z >> 1) ^ -(z & 1)
        idx.append(prev)
    (gc,) = struct.unpack_from("<I", buf, pos); pos += 4
    counts = struct.unpack_from("<%dI" % gc, buf, pos); pos += 4 * gc
    starts = np.concatenate([[0], np.cumsum(counts)]).astype(int)
    (mc,) = struct.unpack_from("<H", buf, pos); pos += 2
    models = {}
    for _ in range(mc):
        ln = buf[pos]; pos += 1
        name = buf[pos:pos + ln].decode("utf8"); pos += ln
        (rc,) = struct.unpack_from("<I", buf, pos); pos += 4
        g = 0
        tris = set()
        for _ in range(rc):
            d, pos = read_varint(buf, pos)
            g += d
            for t in range(starts[g], starts[g + 1]):
                tris.add(tuple(corners[i] for i in idx[3 * t:3 * t + 3]))
        models[name] = tris
    assert pos == len(buf), "trailing bytes"
    return lo, step, models


def verify(folder, path):
    """Every model must decode to exactly its OBJ's triangles (same winding, uv and
    quantised normal), with positions on the pack's uint16 grid."""
    lo, step, models = unpack(path)
    worst = 0.0
    for p in sorted(glob.glob(os.path.join(folder, "*.obj"))):
        name = os.path.splitext(os.path.basename(p))[0]
        vs, vts, vns, tris = read_obj(p)
        V = np.array(vs)
        Qf = (V - lo) / step
        Q = np.round(Qf).astype(np.int64)
        worst = max(worst, float((np.abs(Qf - Q) * step).max()))
        want = set()
        for tri in tris:
            want.add(tuple(tuple(int(c) for c in Q[v]) + (vts[t] if t >= 0 else (0.0, 0.0))
                           + (quant_normal(vns[n]) if n >= 0 else (0, 127, 0)) for v, t, n in tri))
        # uvs went through float32
        want = {tuple(c[:3] + tuple(float(np.float32(x)) for x in c[3:5]) + c[5:] for c in t) for t in want}
        if want != models[name]:
            raise ValueError("%s: %d missing, %d extra triangles"
                             % (name, len(want - models[name]), len(models[name] - want)))
    return "verify ok: %d models identical, max position error %.5f voxel" % (len(models), worst / 0.1)


def run(job):
    folder, cid, check, delete = job
    try:
        dest, s = pack(folder, cid)
        line = ("%s: %d models, triangles %d -> %d unique (%.0f%%) | OBJ %.1f MB -> %.2f MB (x%.1f)"
                % (cid, s["models"], s["tris_total"], s["tris_unique"],
                   100.0 * s["tris_unique"] / s["tris_total"],
                   s["obj_bytes"] / 1e6, s["packed"] / 1e6, s["obj_bytes"] / s["packed"]))
        if check:
            line += " | " + verify(folder, dest)
        if delete:
            for obj in glob.glob(os.path.join(folder, "*.obj")):
                os.remove(obj)
                if os.path.exists(obj + ".meta"):
                    os.remove(obj + ".meta")
            line += " | OBJs deleted"
        return cid, True, line, s["obj_bytes"], s["packed"]
    except Exception as e:
        return cid, False, "%s: FAILED %s: %s (OBJs kept)" % (cid, type(e).__name__, e), 0, 0


def main():
    from concurrent.futures import ProcessPoolExecutor
    argv = sys.argv[1:]
    jobs = 1
    if "--jobs" in argv:
        jobs = int(argv[argv.index("--jobs") + 1])
        del argv[argv.index("--jobs"):argv.index("--jobs") + 2]
    delete = "--delete-obj" in argv
    check = delete or "--verify" in argv
    args = [a for a in argv if not a.startswith("--")]
    root, ids = args[0], args[1:]
    if ids == ["all"]:
        ids = sorted(d for d in os.listdir(root) if os.path.isdir(os.path.join(root, d)))
    work = [(os.path.join(root, c), c, check, delete) for c in ids
            if glob.glob(os.path.join(root, c, "*.obj"))]
    skipped = len(ids) - len(work)
    tot_obj = tot_pack = 0
    failed = []
    with ProcessPoolExecutor(max_workers=jobs) as pool:
        for cid, ok, line, o, p in pool.map(run, work):
            print(line, flush=True)
            tot_obj += o
            tot_pack += p
            if not ok:
                failed.append(cid)
    if tot_pack:
        print("TOTAL %d packed, %d skipped (no OBJs), %d failed: OBJ %.1f MB -> %.1f MB (x%.1f)"
              % (len(work) - len(failed), skipped, len(failed), tot_obj / 1e6, tot_pack / 1e6, tot_obj / tot_pack))
    if failed:
        print("FAILED: " + " ".join(failed))
        sys.exit(1)


if __name__ == "__main__":
    main()
