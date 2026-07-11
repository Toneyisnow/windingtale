"""Quick analyzer for a MagicaVoxel .vox: dims, palette usage, color stats,
and per-Z occupancy. Helps locate ground vs water voxels."""
import struct, sys, collections

def read_chunks(data, off, end, out):
    while off < end:
        cid = data[off:off+4]; off += 4
        n, m = struct.unpack('<ii', data[off:off+8]); off += 8
        content = data[off:off+n]; off += n
        children = data[off:off+m]; off += m
        out.append((cid, content))
        if m:
            read_chunks(data, off-m, off, out)
    return out

def main(path):
    data = open(path, 'rb').read()
    assert data[:4] == b'VOX ', 'not a vox'
    ver = struct.unpack('<i', data[4:8])[0]
    chunks = read_chunks(data, 20, len(data), [])  # skip VOX+ver+MAIN header
    sizes, xyzis, rgba = [], [], None
    for cid, content in chunks:
        if cid == b'SIZE':
            sizes.append(struct.unpack('<iii', content[:12]))
        elif cid == b'XYZI':
            cnt = struct.unpack('<i', content[:4])[0]
            vox = []
            for i in range(cnt):
                x, y, z, c = content[4+i*4:8+i*4]
                vox.append((x, y, z, c))
            xyzis.append(vox)
        elif cid == b'RGBA':
            rgba = [tuple(content[i*4:i*4+4]) for i in range(256)]
    print('version', ver)
    print('models', len(xyzis), 'sizes', sizes)
    vox = xyzis[0]
    sx, sy, sz = sizes[0]
    print('total voxels', len(vox))
    # color usage
    cc = collections.Counter(c for _,_,_,c in vox)
    print('distinct color indices used:', len(cc))
    print('top colors (idx -> rgba, count):')
    for idx, n in cc.most_common(25):
        col = rgba[idx-1] if 1 <= idx <= 256 else None
        print(f'  idx {idx:3d}  rgba {col}  count {n}')
    # per-z occupancy
    zc = collections.Counter(z for _,_,z,_ in vox)
    print('per-Z voxel counts:')
    for z in range(sz):
        print(f'  z={z:3d}  {zc.get(z,0)}')

if __name__ == '__main__':
    main(sys.argv[1] if len(sys.argv) > 1 else
         r'D:\SourceCode\Git\toneyisnow\windingtale\Resources\Remastered\BG\BG_07.vox')
