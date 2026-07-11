"""Find connected water components (by x,y footprint) in BG_07.vox."""
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

data=open(PATH,'rb').read()
size=vox=None
for cid,c in read_chunks(data,20,len(data),[]):
    if cid==b'SIZE': size=struct.unpack('<iii',c[:12])
    elif cid==b'XYZI':
        n=struct.unpack('<i',c[:4])[0]
        vox=[(c[4+i*4],c[5+i*4],c[6+i*4],c[7+i*4]) for i in range(n)]
sx,sy,sz=size
wset=set((x,y) for x,y,z,col in vox if col in WATER)

seen=set(); comps=[]
for cell in wset:
    if cell in seen: continue
    stack=[cell]; seen.add(cell); comp=[]
    while stack:
        cx,cy=stack.pop(); comp.append((cx,cy))
        for dx,dy in ((1,0),(-1,0),(0,1),(0,-1)):
            nb=(cx+dx,cy+dy)
            if nb in wset and nb not in seen:
                seen.add(nb); stack.append(nb)
    comps.append(comp)

comps.sort(key=len, reverse=True)
print(f'{len(comps)} water components')
for i,comp in enumerate(comps):
    xs=[p[0] for p in comp]; ys=[p[1] for p in comp]
    touches_edge = min(xs)==0 or max(xs)==sx-1 or min(ys)==0 or max(ys)==sy-1
    print(f'  comp {i}: cells={len(comp):5d}  x[{min(xs)},{max(xs)}] y[{min(ys)},{max(ys)}]'
          f'  centroid=({sum(xs)//len(xs)},{sum(ys)//len(ys)})  touchesEdge={touches_edge}')
