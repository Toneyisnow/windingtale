"""Shared helpers for the 2D -> 3D chapter map pipeline.

Everything here is derived from the chapter 01 assets that were built by hand,
so the numbers are not arbitrary -- see ``docs`` notes on each constant.

Coordinate conventions (all verified against Chapter_01.json + Shapes_01):

  ShapeMatrix[x][y]   tile id at map X = x + 1 (column, left -> right)
                                map Y = y + 1 (row,    top  -> bottom)

  tile PNG (px, py)   px = column inside the tile, py = row from the TOP

  shape VOX (x, y, z) x = px                 (map X grows with vox X)
                      y = 23 - py            (map Y grows with vox -Y)
                      z = terrain level, ground is GROUND_Z

  obstacle VOX        size = (cols * 24, rows * 24, height); same X / Y axes as
                      a shape VOX, and Position in the chapter JSON is the
                      top-left tile (smallest map X, smallest map Y) of the
                      footprint, 1-based, and may be <= 0 when the object is
                      only partially on screen.
"""

import os
import struct

TILE = 24              # a map tile is 24 x 24 pixels in the original art
CANVAS = 40            # shape VOX models are authored on a 40^3 grid
GROUND_Z = 23          # z of the ground layer for ordinary land

# Terrain levels, relative to GROUND_Z. Sand sits one voxel lower than land,
# water one lower again, deep water one lower still.
TERRAIN_Z = {
    'land': GROUND_Z,
    'sand': GROUND_Z - 1,
    'water': GROUND_Z - 2,
    'deep_water': GROUND_Z - 3,
}

# The original art is drawn in a 6-level-per-channel palette once quantized;
# dark green is the "grass" colour that gets pulled up into 3D blades.
GRASS_RGB = (51, 102, 0)
GRASS_LIFT = 2         # extra voxels stacked above the ground layer

_RAMP = (238, 221, 187, 170, 136, 119, 85, 68, 34, 17)


def default_palette():
    """The MagicaVoxel default 256-colour palette, as [(r, g, b, a)] * 256.

    Index n (1-based, as stored in XYZI) is ``palette[n - 1]``. Entries 1..215
    are the 6x6x6 colour cube in steps of 51; 216..255 are the red/green/blue/
    grey ramps; 256 is black.
    """
    pal = []
    for i in range(215):
        pal.append((255 - 51 * (i // 36),
                    255 - 51 * ((i // 6) % 6),
                    255 - 51 * (i % 6),
                    255))
    for v in _RAMP:
        pal.append((v, 0, 0, 255))
    for v in _RAMP:
        pal.append((0, v, 0, 255))
    for v in _RAMP:
        pal.append((0, 0, v, 255))
    for v in _RAMP:
        pal.append((v, v, v, 255))
    pal.append((0, 0, 0, 255))
    return pal


PALETTE = default_palette()

# Index 256 is pure black; the original chapter 01 models never use it, so the
# nearest-colour search stops at 255 and ties go to the lowest index. That is
# what reproduces Shapes_01 exactly (pure black in the art lands on 225, the
# darkest red-ramp entry, not on 256).
PALETTE_SEARCH_MAX = 255

_index_cache = {}


def palette_index(rgb):
    """Nearest palette index (1-based) for an RGB colour."""
    key = tuple(rgb[:3])
    hit = _index_cache.get(key)
    if hit is not None:
        return hit
    r, g, b = key
    best, best_d = 1, None
    for n in range(1, PALETTE_SEARCH_MAX + 1):
        c = PALETTE[n - 1]
        d = (c[0] - r) ** 2 + (c[1] - g) ** 2 + (c[2] - b) ** 2
        if best_d is None or d < best_d:
            best, best_d = n, d
    _index_cache[key] = best
    return best


def palette_rgb(rgb):
    """The palette colour an art colour resolves to."""
    return PALETTE[palette_index(rgb) - 1][:3]


# Terrain is decided on the *resolved palette colour*, not the raw art colour,
# so the classes below are exact colour sets read out of Shapes_01. Sand is the
# light-tan family, water the two blues. Note (153,102,51) is dirt, not sand,
# and stays at ground level -- which is why this is a table and not a formula.
SAND_COLOURS = frozenset([
    (204, 153, 102), (204, 153, 153), (204, 153, 0),
    (255, 153, 0), (255, 204, 51), (255, 204, 102),
])
WATER_COLOURS = frozenset([(51, 102, 153)])
DEEP_WATER_COLOURS = frozenset([(51, 51, 153)])


def classify_terrain(rgb, resolve=True):
    """Terrain class of a tile pixel: land / sand / water / deep_water."""
    c = palette_rgb(rgb) if resolve else tuple(rgb[:3])
    if c in DEEP_WATER_COLOURS:
        return 'deep_water'
    if c in WATER_COLOURS:
        return 'water'
    if c in SAND_COLOURS:
        return 'sand'
    return 'land'


# --------------------------------------------------------------------------
# .vox read / write (MagicaVoxel version 150, MAIN > SIZE + XYZI + RGBA)
# --------------------------------------------------------------------------
#
# A .vox stores each voxel coordinate in one byte, so no single model can be
# wider than 256 voxels. Anything bigger -- chapter 08's castle wall is 696 --
# is written the way MagicaVoxel itself handles it: cut into parts that each
# get their own SIZE + XYZI pair, held together by the scene graph (nTRN > nGRP
# > nTRN > nSHP) that carries each part's translation. read_vox() folds the
# parts back into one VoxModel, so every caller keeps seeing one model with
# the full SIZE it was written with. A model that fits in one part is written
# exactly as before, byte for byte.

MODEL_MAX = 256     # MagicaVoxel's per-model edge limit
PART = 240          # part edge used when splitting: 10 tiles, so cuts fall on tile edges


class VoxModel(object):
    def __init__(self, size, voxels, palette=None, parts=1):
        self.size = tuple(size)          # (sx, sy, sz)
        self.voxels = list(voxels)       # [(x, y, z, color_index)]
        self.palette = palette or list(PALETTE)
        self.parts = parts               # how many models the file held

    def by_z(self, z):
        return [(x, y, c) for x, y, zz, c in self.voxels if zz == z]

    def bbox(self):
        if not self.voxels:
            return None
        xs = [v[0] for v in self.voxels]
        ys = [v[1] for v in self.voxels]
        zs = [v[2] for v in self.voxels]
        return (min(xs), max(xs), min(ys), max(ys), min(zs), max(zs))


def _read_string(data, i):
    n = struct.unpack('<i', data[i:i + 4])[0]
    return data[i + 4:i + 4 + n].decode('utf-8', 'replace'), i + 4 + n


def _read_dict(data, i):
    n = struct.unpack('<i', data[i:i + 4])[0]
    i += 4
    d = {}
    for _ in range(n):
        k, i = _read_string(data, i)
        v, i = _read_string(data, i)
        d[k] = v
    return d, i


def read_vox(path):
    with open(path, 'rb') as f:
        data = f.read()
    if data[:4] != b'VOX ':
        raise ValueError('%s is not a .vox file' % path)
    models = []                 # [(size, voxels)] in file order
    palette = None
    trn = {}                    # node id -> (child id, (tx, ty, tz))
    grp = {}                    # node id -> [child ids]
    shp = {}                    # node id -> model index
    size = None
    i = 8
    while i < len(data):
        cid = data[i:i + 4].decode('ascii', 'replace')
        n, _children = struct.unpack('<ii', data[i + 4:i + 12])
        i += 12
        body = data[i:i + n]
        if cid == 'SIZE':
            size = struct.unpack('<iii', body[:12])
        elif cid == 'XYZI':
            count = struct.unpack('<i', body[:4])[0]
            voxels = [tuple(body[4 + k * 4:8 + k * 4]) for k in range(count)]
            models.append((size, voxels))
        elif cid == 'RGBA':
            palette = [tuple(body[k * 4:k * 4 + 4]) for k in range(256)]
        elif cid == 'nTRN':
            node = struct.unpack('<i', body[:4])[0]
            _attrs, j = _read_dict(body, 4)
            child, _res, _layer, frames = struct.unpack('<iiii', body[j:j + 16])
            j += 16
            t = (0, 0, 0)
            for _ in range(frames):
                frame, j = _read_dict(body, j)
                if '_t' in frame:
                    t = tuple(int(v) for v in frame['_t'].split())
            trn[node] = (child, t)
        elif cid == 'nGRP':
            node = struct.unpack('<i', body[:4])[0]
            _attrs, j = _read_dict(body, 4)
            count = struct.unpack('<i', body[j:j + 4])[0]
            grp[node] = list(struct.unpack('<%di' % count, body[j + 4:j + 4 + 4 * count]))
        elif cid == 'nSHP':
            node = struct.unpack('<i', body[:4])[0]
            _attrs, j = _read_dict(body, 4)
            shp[node] = struct.unpack('<i', body[j + 4:j + 8])[0]
        if cid != 'MAIN':
            i += n
    if not models:
        raise ValueError('%s has no SIZE chunk' % path)

    if len(models) == 1 and not shp:
        size, voxels = models[0]
        return VoxModel(size, voxels, palette)

    # Walk the scene graph: a model's world position is the sum of the
    # translations on the nTRN nodes above it, applied to its centre.
    placed = {}             # model index -> world position of its (0, 0, 0)

    def walk(node, t):
        if node in trn:
            child, dt = trn[node]
            walk(child, (t[0] + dt[0], t[1] + dt[1], t[2] + dt[2]))
        elif node in grp:
            for child in grp[node]:
                walk(child, t)
        elif node in shp:
            k = shp[node]
            if k < len(models):
                s = models[k][0]
                placed[k] = (t[0] - s[0] // 2, t[1] - s[1] // 2, t[2] - s[2] // 2)

    if trn or grp or shp:
        walk(0, (0, 0, 0))
    for k in range(len(models)):
        placed.setdefault(k, (0, 0, 0))

    ox = min(p[0] for p in placed.values())
    oy = min(p[1] for p in placed.values())
    oz = min(p[2] for p in placed.values())
    voxels = []
    ex = ey = ez = 0
    for k, (s, vs) in enumerate(models):
        px, py, pz = placed[k]
        px, py, pz = px - ox, py - oy, pz - oz
        ex, ey, ez = max(ex, px + s[0]), max(ey, py + s[1]), max(ez, pz + s[2])
        voxels.extend((x + px, y + py, z + pz, c) for x, y, z, c in vs)
    return VoxModel((ex, ey, ez), voxels, palette, parts=len(models))


def _chunk(cid, content):
    return cid + struct.pack('<ii', len(content), 0) + content


def _string(s):
    b = s.encode('utf-8')
    return struct.pack('<i', len(b)) + b


def _dict(pairs):
    out = struct.pack('<i', len(pairs))
    for k, v in pairs:
        out += _string(k) + _string(v)
    return out


def _xyzi(voxels):
    xyzi = struct.pack('<i', len(voxels))
    xyzi += b''.join(bytes(bytearray((x, y, z, c))) for x, y, z, c in voxels)
    return _chunk(b'XYZI', xyzi)


def write_vox(path, size, voxels, palette=None):
    """Write a .vox. ``voxels`` is an iterable of (x, y, z, color_index).

    ``size`` may exceed 256 on any axis; the model is then written as several
    parts that MagicaVoxel and read_vox() reassemble. Parts are cut every PART
    voxels, which is 10 tiles, so the seams fall on tile boundaries.
    """
    size = tuple(int(v) for v in size)
    voxels = list(voxels)
    if len(voxels) > 0xFFFFFFFF:
        raise ValueError('too many voxels')
    for x, y, z, c in voxels:
        if not (0 <= x < size[0] and 0 <= y < size[1] and 0 <= z < size[2]):
            raise ValueError('voxel %r outside SIZE %r' % ((x, y, z, c), size))
        if not (1 <= c <= 255):
            raise ValueError('colour index %d out of range 1..255' % c)

    pal = palette or PALETTE
    if max(size) <= MODEL_MAX:
        body = _chunk(b'SIZE', struct.pack('<iii', *size))
        body += _xyzi(voxels)
    else:
        cuts = [list(range(0, s, PART)) for s in size]
        parts = []                       # [(origin, part size)]
        for x0 in cuts[0]:
            for y0 in cuts[1]:
                for z0 in cuts[2]:
                    parts.append(((x0, y0, z0),
                                  (min(PART, size[0] - x0),
                                   min(PART, size[1] - y0),
                                   min(PART, size[2] - z0))))
        buckets = [[] for _ in parts]
        index = {origin: k for k, (origin, _s) in enumerate(parts)}
        for x, y, z, c in voxels:
            k = index[(x // PART * PART, y // PART * PART, z // PART * PART)]
            ox, oy, oz = parts[k][0]
            buckets[k].append((x - ox, y - oy, z - oz, c))

        body = b''
        for (origin, s), vs in zip(parts, buckets):
            body += _chunk(b'SIZE', struct.pack('<iii', *s))
            body += _xyzi(vs)

        # scene graph: root transform > group > one (transform > shape) per part
        body += _chunk(b'nTRN', struct.pack('<i', 0) + _dict([]) +
                       struct.pack('<iiii', 1, -1, -1, 1) + _dict([]))
        body += _chunk(b'nGRP', struct.pack('<i', 1) + _dict([]) +
                       struct.pack('<i', len(parts)) +
                       b''.join(struct.pack('<i', 2 + 2 * k) for k in range(len(parts))))
        for k, ((ox, oy, oz), s) in enumerate(parts):
            t = '%d %d %d' % (ox + s[0] // 2, oy + s[1] // 2, oz + s[2] // 2)
            body += _chunk(b'nTRN', struct.pack('<i', 2 + 2 * k) + _dict([]) +
                           struct.pack('<iiii', 3 + 2 * k, -1, 0, 1) + _dict([('_t', t)]))
            body += _chunk(b'nSHP', struct.pack('<i', 3 + 2 * k) + _dict([]) +
                           struct.pack('<ii', 1, k) + _dict([]))
    body += _chunk(b'RGBA', b''.join(bytes(bytearray(c)) for c in pal))

    parent = os.path.dirname(os.path.abspath(path))
    if parent and not os.path.isdir(parent):
        os.makedirs(parent)
    with open(path, 'wb') as f:
        f.write(b'VOX ' + struct.pack('<i', 150))
        f.write(b'MAIN' + struct.pack('<ii', 0, len(body)))
        f.write(body)


# --------------------------------------------------------------------------
# Workspace layout
# --------------------------------------------------------------------------

def workspace_root(start=None):
    """Walk up from this file to the windingtale workspace root."""
    here = os.path.abspath(start or __file__)
    d = os.path.dirname(here)
    while True:
        if os.path.isdir(os.path.join(d, 'Resources')) and os.path.isdir(os.path.join(d, 'Tools')):
            return d
        parent = os.path.dirname(d)
        if parent == d:
            raise RuntimeError('could not locate the windingtale workspace root')
        d = parent


def chapter_json_path(root, nn):
    return os.path.join(root, 'WindingTale2', 'Assets', 'Resources', 'Data',
                        'Chapters', 'Chapter_%s.json' % nn)


def shape_panel_dir(root, nn):
    return os.path.join(root, 'Resources', 'Original', 'Shapes', 'ShapePanel%s' % nn)


def shapes_vox_dir(root, nn):
    return os.path.join(root, 'Resources', 'Remastered', 'Shapes', 'Shapes_%s' % nn, 'vox')


def obstacles_vox_dir(root):
    return os.path.join(root, 'Resources', 'Remastered', 'Obstacles', 'vox')


def tile_png_name(nn, tile_id):
    """Source tile file name. ShapePanel01 holds Shape_0_*, ShapePanel02 holds
    Shape_1_*, ... -- the prefix is the panel's 0-based index."""
    return 'Shape_%d_%d.png' % (int(nn) - 1, tile_id)


def shape_vox_name(nn, tile_id):
    """Remastered tile file name. Shapes_01 holds Shape_1_*, Shapes_02 holds
    Shape_2_*, ... -- the prefix is the chapter's 1-based index, one more than
    the source PNG's."""
    return 'Shape_%d_%d.vox' % (int(nn), tile_id)


def map_png_path(root, nn, grid=False):
    name = 'Chapter-%s-grid.png' % nn if grid else 'Chapter-%s.png' % nn
    return os.path.join(root, 'Resources', 'Original', 'Maps', nn, name)


def nn(chapter):
    """Normalise a chapter argument ('1', 1, '01') to the two-digit form."""
    return '%02d' % int(chapter)
