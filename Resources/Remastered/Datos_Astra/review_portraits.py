from pathlib import Path
from PIL import Image, ImageDraw
import json
import numpy as np

ROOT=Path(__file__).resolve().parent
def sheets():
    for start,end in [(0,6),(6,13),(13,19),(19,25),(25,29),(29,33)]:
        c=Image.new('RGB',(1000,(end-start)*274),'white');d=ImageDraw.Draw(c)
        for n in range(start,end):
            for v in range(4):
                p=ROOT/f'{n:03d}'/f'Data_{n:03d}_{v}.png'
                if p.exists(): c.paste(Image.open(p).resize((250,250)),(v*250,(n-start)*274))
                d.text((v*250+5,(n-start)*274+253),f'{n:03d}_{v}',fill='black')
        c.save(ROOT/f'review_{start:03d}_{end-1:03d}.png')

def verify():
    rows=[]
    for n in range(33):
        ident=f'{n:03d}'; folder=ROOT/ident
        base=np.array(Image.open(folder/f'Data_{ident}_0.png').convert('RGB'))
        bg=np.array(Image.open(folder/'_background_mask.png'))>0
        for v in range(4):
            p=folder/f'Data_{ident}_{v}.png'
            if not p.exists():
                rows.append(dict(id=ident,frame=v,missing=True));continue
            a=np.array(Image.open(p).convert('RGB'))
            diff=np.any(base!=a,axis=2)
            m=folder/f'_edit_mask_{v}.png'
            allowed=np.array(Image.open(m))>0 if v and m.exists() else np.zeros(diff.shape,bool)
            rows.append(dict(id=ident,frame=v,size=list(a.shape[:2]),changed_pixels=int(diff.sum()),outside_mask_changes=int((diff&~allowed).sum()),background_nonflat_pixels=int(np.any(a[bg]!=[23,70,140],axis=1).sum())))
    (ROOT/'validation.json').write_text(json.dumps(rows,indent=2))
    print(json.dumps(dict(frames=len(rows),missing=sum(r.get('missing',False) for r in rows),outside_mask_failures=[r for r in rows if r.get('outside_mask_changes',0)],background_failures=[r for r in rows if r.get('background_nonflat_pixels',0)])))

if __name__=='__main__':
    sheets();verify()
