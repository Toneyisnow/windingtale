"""Surface + water analysis for BG_07.vox."""
import struct, collections

PATH = r'D:\SourceCode\Git\toneyisnow\windingtale\Resources\Remastered\BG\BG_07.vox'
WATER = {5, 6}

def read_chunks(data, off, end, out):
    while off < end:
        cid = data[off:off+4]; off += 4
        n, m = struct.unpack('<ii', data[off:off+8]); off += 8
        out.append((cid, data[off:off+n])); off += n
        if m: read_chunks(data, off, off+m, out); off += m
    return out

data = open(PATH,'rb').read()
chunks = read_chunks(data, 20, len(data), [])
size=None; vox=None; rgba=None
for cid,c in chunks:
    if cid==b'SIZE': size=struct.unpack('<iii',c[:12])
    elif cid==b'XYZI':
        n=struct.unpack('<i',c[:4])[0]
        vox=[(c[4+i*4],c[5+i*4],c[6+i*4],c[7+i*4]) for i in range(n)]
    elif cid==b'RGBA': rgba=[tuple(c[i*4:i*4+4]) for i in range(256)]
sx,sy,sz=size

# top surface color per (x,y)
topz={}; topc={}
for x,y,z,col in vox:
    if z>topz.get((x,y),-1):
        topz[(x,y)]=z; topc[(x,y)]=col
surf=collections.Counter(topc.values())
print('=== top-surface color counts (idx rgba count) ===')
for idx,n in surf.most_common():
    print(f'  idx {idx:3d} {rgba[idx-1]} {n}')

# water footprint
wx=[x for x,y,z,col in vox if col in WATER]
wy=[y for x,y,z,col in vox if col in WATER]
wz=[z for x,y,z,col in vox if col in WATER]
print('\n=== water bbox ===')
print(' x', min(wx), max(wx), ' y', min(wy), max(wy), ' z', min(wz), max(wz))
print(' water voxels', len(wx))

# height map stats around water region (top z), and what's under water
wset=set((x,y) for x,y,z,col in vox if col in WATER)
print('\n=== water footprint (x,y) count', len(wset))
# surrounding ring surface height: sample border cells just outside water footprint
import statistics
border_heights=[]
for (x,y) in wset:
    for dx,dy in ((1,0),(-1,0),(0,1),(0,-1)):
        nb=(x+dx,y+dy)
        if nb not in wset and nb in topz:
            border_heights.append(topz[nb])
if border_heights:
    print(' surrounding surface top z: min',min(border_heights),'max',max(border_heights),
          'median',int(statistics.median(border_heights)),'mean',round(statistics.mean(border_heights),1))

# water-surface (top z) height per cell distribution
water_top=collections.Counter()
wtopz={}
for x,y,z,col in vox:
    if col in WATER and z>wtopz.get((x,y),-1):
        wtopz[(x,y)]=z
print(' water top-z distribution:', dict(collections.Counter(wtopz.values())))

# ASCII map of where water is (downsampled), over the full XY
print('\n=== top-down water map (X horiz, Y vert, downsample 4) ===')
step=4
for y in range(0,sy,step):
    row=[]
    for x in range(0,sx,step):
        cell=any((x+i,y+j) in wset for i in range(step) for j in range(step))
        row.append('#' if cell else '.')
    print(''.join(row))
