"""
Edit BG_07.vox:
  1. Make the ground deeper green (palette recolor of the grass + ground tans).
  2. Remove the small water pool in the middle (the largest interior water
     component) and flatten it to ground at the surrounding level (z=11).

Backs up the original to BG_07.vox.bak before overwriting.
"""
import struct, shutil, collections, os

PATH = r'D:\SourceCode\Git\toneyisnow\windingtale\Resources\Remastered\BG\BG_07.vox'
BAK = PATH + '.bak'
WATER = {5, 6}
GROUND_Z = 11          # surrounding flat-ground top level
GREEN_SURFACE = 2      # palette idx used for the filled pool's top surface
DIRT = 7               # palette idx used for the fill below the surface

# palette idx -> new (r,g,b). Deepen greens; nudge the ground tans toward green.
PALETTE_OVERRIDES = {
    9:  (28, 60, 26),    # darkest green
    1:  (40, 82, 36),    # dark green
    2:  (52, 100, 44),   # mid green  (also the filled-pool surface)
    10: (74, 122, 52),   # was yellow-green -> deep green
    4:  (66, 110, 48),   # flat ground tan -> deep green
    3:  (96, 140, 66),   # bright terrain tan -> green
    5:  (51, 51, 153),   # water surface: pale cyan -> map-tile deep sea blue
    6:  (51, 51, 153),   # deep water -> same map-tile deep sea blue
}

def read_main_children(data):
    # data starts at 'VOX ' + version(4) + MAIN header. Return list of
    # (cid, content, children_bytes) for MAIN's children (flat).
    assert data[:4] == b'VOX '
    off = 8
    cid = data[off:off+4]; off += 4
    n, m = struct.unpack('<ii', data[off:off+8]); off += 8
    assert cid == b'MAIN'
    off += n                      # MAIN content (empty)
    end = off + m
    out = []
    while off < end:
        ccid = data[off:off+4]; off += 4
        cn, cm = struct.unpack('<ii', data[off:off+8]); off += 8
        content = data[off:off+cn]; off += cn
        children = data[off:off+cm]; off += cm
        out.append((ccid, content, children))
    return out

def main():
    # Always edit from the pristine original so re-runs (e.g. re-tuning the
    # palette) are idempotent. The first run creates the .bak.
    if not os.path.exists(BAK):
        shutil.copyfile(PATH, BAK)
    data = open(BAK, 'rb').read()
    children = read_main_children(data)

    size = None
    vox = None
    rgba = None
    for cid, content, _ in children:
        if cid == b'SIZE':
            size = struct.unpack('<iii', content[:12])
        elif cid == b'XYZI':
            cnt = struct.unpack('<i', content[:4])[0]
            vox = [[content[4+i*4], content[5+i*4], content[6+i*4], content[7+i*4]]
                   for i in range(cnt)]
        elif cid == b'RGBA':
            rgba = [list(content[i*4:i*4+4]) for i in range(256)]
    print('chunks:', [c[0].decode() for c in children], 'size', size, 'voxels', len(vox))

    # --- locate the small middle pool: largest water component not touching edge
    sx, sy, sz = size
    wset = set((x, y) for x, y, z, c in vox if c in WATER)
    seen = set(); comps = []
    for cell in wset:
        if cell in seen:
            continue
        stack = [cell]; seen.add(cell); comp = []
        while stack:
            cx, cy = stack.pop(); comp.append((cx, cy))
            for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                nb = (cx+dx, cy+dy)
                if nb in wset and nb not in seen:
                    seen.add(nb); stack.append(nb)
        comps.append(comp)
    def touches_edge(comp):
        return any(x == 0 or x == sx-1 or y == 0 or y == sy-1 for x, y in comp)
    interior = [c for c in comps if not touches_edge(c)]
    interior.sort(key=len, reverse=True)
    pool = set(interior[0])
    xs = [p[0] for p in pool]; ys = [p[1] for p in pool]
    print(f'pool: {len(pool)} cells, x[{min(xs)},{max(xs)}] y[{min(ys)},{max(ys)}]')

    # --- palette recolor
    for idx, (r, g, b) in PALETTE_OVERRIDES.items():
        rgba[idx-1] = [r, g, b, 255]

    # --- flatten the pool to ground
    new_vox = []
    removed = recolored = 0
    for x, y, z, c in vox:
        if (x, y) in pool:
            if z > GROUND_Z:
                removed += 1
                continue                      # remove the water bulge above ground
            if z == GROUND_Z:
                c = GREEN_SURFACE; recolored += 1
            elif c in WATER:
                c = DIRT; recolored += 1
        new_vox.append((x, y, z, c))
    print(f'pool fill: removed {removed} voxels above z={GROUND_Z}, recolored {recolored}')

    # --- rebuild file, replacing XYZI and RGBA, keeping all other chunks
    def chunk(cid, content, kids=b''):
        return cid + struct.pack('<ii', len(content), len(kids)) + content + kids

    xyzi_body = bytearray(struct.pack('<i', len(new_vox)))
    for (x, y, z, c) in new_vox:
        xyzi_body += bytes([x, y, z, c])
    rgba_body = bytearray()
    for i in range(256):
        rgba_body += bytes(rgba[i])

    rebuilt = b''
    for cid, content, kids in children:
        if cid == b'XYZI':
            rebuilt += chunk(b'XYZI', bytes(xyzi_body))
        elif cid == b'RGBA':
            rebuilt += chunk(b'RGBA', bytes(rgba_body))
        else:
            rebuilt += chunk(cid, content, kids)

    blob = b'VOX ' + struct.pack('<i', 150) + chunk(b'MAIN', b'', rebuilt)

    with open(PATH, 'wb') as f:
        f.write(blob)
    print('wrote', PATH, '(source: .bak), total voxels now', len(new_vox))

if __name__ == '__main__':
    main()
