from pathlib import Path
import argparse
import json
import numpy as np
from PIL import Image, ImageDraw, ImageFilter
from scipy import ndimage

ROOT = Path(__file__).resolve().parent
BLUE = (23, 70, 140)
SEEDS = {
0:[(.02,.02),(.04,.5),(.40,.82)],1:[(.04,.7)],2:[(.02,.5),(.98,.5)],
3:[(.03,.4),(.05,.99)],4:[(.05,.5),(.99,.05)],5:[(.05,.6),(.98,.03)],
6:[(.02,.03),(.98,.03)],7:[(.03,.5),(.98,.03)],8:[(.04,.5),(.98,.73)],
9:[(.02,.4),(.98,.4)],10:[(.02,.04),(.98,.5)],11:[(.02,.5),(.98,.5)],
12:[(.02,.4),(.98,.65)],13:[(.05,.55),(.02,.85)],14:[(.02,.3)],
15:[(.02,.03),(.45,.02),(.98,.4),(.06,.6),(.87,.66)],16:[(.04,.4),(.1,.01)],
17:[(.03,.5),(.98,.55)],18:[(.02,.1),(.01,.5)],19:[(.05,.1),(.04,.9),(.54,.02),(.74,.03)],
20:[(.02,.35),(.98,.4)],21:[(.04,.5),(.03,.03)],22:[(.05,.4),(.02,.02),(.48,.98),(.14,.02),(.31,.05)],
23:[(.04,.4)],24:[(.02,.55),(.96,.1)],25:[(.02,.2),(.97,.08),(.99,.4)],
26:[(.02,.4),(.98,.04)],27:[(.03,.5),(.97,.5)],28:[(.02,.2),(.98,.2)],
29:[(.03,.35),(.01,.01)],30:[(.05,.3),(.04,.83),(.34,.06),(.59,.05),(.95,.05)],
31:[(.03,.5)],32:[(.04,.7),(.97,.08)]}

def background(number):
    ident=f'{number:03d}'
    d=ROOT/ident
    raw=d/'_base_before_background.png'
    final=d/f'Data_{ident}_0.png'
    if not raw.exists():
        Image.open(final).save(raw)
    im=Image.open(raw).convert('RGB')
    a=np.array(im)
    lo=ndimage.minimum_filter(a,size=(3,3,1)).astype(int)
    hi=ndimage.maximum_filter(a,size=(3,3,1)).astype(int)
    smooth=np.max(hi-lo,axis=2)<22
    blue=(a[:,:,2]>70)&(a[:,:,0]<95)&(a[:,:,1]<175)&(a[:,:,2].astype(int)>a[:,:,0].astype(int)+45)
    labels,_=ndimage.label(smooth&blue)
    mask=np.zeros(a.shape[:2],dtype=bool)
    h,w=mask.shape
    for x,y in SEEDS[number]:
        xx,yy=round(x*(w-1)),round(y*(h-1))
        label=labels[yy,xx]
        if label:
            mask |= labels==label
    mask=ndimage.binary_fill_holes(mask)
    # Extend only a few pixels to cover the smooth side of anti-aliased boundaries.
    mask=ndimage.binary_dilation(mask,iterations=1)&blue
    if number==12:
        c=np.array(Image.open(d/'_background_clean_candidate.png').convert('RGB').resize(im.size))
        cb=(c[:,:,2]>85)&(c[:,:,0]<60)&(c[:,:,1]<110)&(c[:,:,2].astype(int)>c[:,:,0].astype(int)+60)
        yy,xx=np.indices(mask.shape)
        mask |= cb & (yy>h*.77) & ((xx<w*.27)|(xx>w*.9))
    a[mask]=BLUE
    Image.fromarray(a).save(final)
    Image.fromarray((mask*255).astype('uint8')).save(d/'_background_mask.png')
    return float(mask.mean())

def contact():
    canvas=Image.new('RGB',(7*200,5*224),'#eeeeee')
    draw=ImageDraw.Draw(canvas)
    for n in range(33):
        ident=f'{n:03d}'
        im=Image.open(ROOT/ident/f'Data_{ident}_0.png').convert('RGB').resize((200,200))
        x,y=(n%7)*200,(n//7)*224
        canvas.paste(im,(x,y));draw.text((x+8,y+203),ident,fill='black')
    canvas.save(ROOT/'base_contact_sheet.png')

def frame(ident, variant):
    d=ROOT/ident
    regions=json.loads((ROOT/'regions.json').read_text())[ident]
    base=d/f'Data_{ident}_0.png'
    output=d/f'Data_{ident}_{variant}.png'
    if (variant in (1,2) and regions.get('hidden')) or (variant==3 and regions.get('eyes_hidden')):
        Image.open(base).save(output)
        return
    candidate=d/f'_generated_{variant}.png'
    boxes=regions['eyes' if variant==3 else 'mouth']
    mask=composite(base,candidate,output,boxes)
    mask.save(d/f'_edit_mask_{variant}.png')

def composite(base_path, edit_path, output_path, boxes):
    base = Image.open(base_path).convert('RGB')
    edit = Image.open(edit_path).convert('RGB').resize(base.size, Image.Resampling.LANCZOS)
    mask = Image.new('L', base.size)
    draw = ImageDraw.Draw(mask)
    w, h = base.size
    for box in boxes:
        if isinstance(box[0], (list, tuple)):
            draw.polygon([(round(x*w),round(y*h)) for x,y in box], fill=255)
        else:
            draw.ellipse(tuple(round(v * (w if i % 2 == 0 else h)) for i, v in enumerate(box)), fill=255)
    mask = mask.filter(ImageFilter.GaussianBlur(max(1, w / 300)))
    background_mask = Path(base_path).parent / '_background_mask.png'
    if background_mask.exists():
        m = np.array(mask)
        m[np.array(Image.open(background_mask).convert('L')) > 0] = 0
        mask = Image.fromarray(m)
    result = Image.composite(edit, base, mask)
    result.save(output_path)
    a, b, m = np.array(base), np.array(result), np.array(mask)
    assert np.array_equal(a[m == 0], b[m == 0]), 'Pixels outside mask changed'
    return mask

if __name__ == '__main__':
    parser = argparse.ArgumentParser()
    parser.add_argument('action', choices=['fix013','background','contact','frame'])
    parser.add_argument('--id')
    parser.add_argument('--variant',type=int)
    args = parser.parse_args()
    if args.action == 'fix013':
        d = ROOT / '013'
        composite(d/'Data_013_0.png', d/'Data_013_mouth_correction.png', d/'Data_013_0.png', [(0.285,0.515,0.515,0.665)])
    elif args.action == 'background':
        for n in range(33):
            print(f'{n:03d}: {background(n):.3%}')
        contact()
    elif args.action == 'contact':
        contact()
    elif args.action == 'frame':
        frame(args.id,args.variant)
