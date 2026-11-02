import sys,json,shutil,uuid,re,hashlib
from pathlib import Path
from PIL import Image,ImageDraw
import numpy as np
sys.path.insert(0,str(Path(__file__).resolve().parent));import process_portraits as p
ids=['758','759','760'];game=Path('WindingTale2/Assets/Resources/Datos');out=Image.new('RGB',(1000,810),'white');draw=ImageDraw.Draw(out)
for row,i in enumerate(ids):
 d=p.ROOT/i
 for v in range(1,4):p.frame(i,v)
 base=np.array(Image.open(d/f'Data_{i}_0.png'));bg=np.array(Image.open(d/'_background_mask.png'))>0
 for v in range(4):
  f=d/f'Data_{i}_{v}.png';a=np.array(Image.open(f));assert np.all(a[bg]==p.BLUE)
  if v:
   m=np.array(Image.open(d/f'_edit_mask_{v}.png'))>0;assert np.array_equal(a[~m],base[~m])
  out.paste(Image.open(f).resize((250,250)),(v*250,row*270));draw.text((v*250,row*270+250),f'{i}_{v}',fill='black')
out.save(p.ROOT/'review_758_760.png');print('Composite and pixel validation complete')
if '--sync' in sys.argv:
 for i in ids:
  dest=game/i;dest.mkdir(exist_ok=True)
  for v in range(4):
   f=p.ROOT/i/f'Data_{i}_{v}.png';target=dest/f'Dato_{i}_{v}.png';shutil.copy2(f,target);assert hashlib.sha256(f.read_bytes()).digest()==hashlib.sha256(target.read_bytes()).digest()
   meta=Path(str(target)+'.meta')
   if not meta.exists():
    text=(game/'000'/'Dato_000_0.png.meta').read_text();text=re.sub(r'(?m)^guid: .+$','guid: '+uuid.uuid4().hex,text);text=re.sub(r'(?m)^(    spriteID: ).+$',lambda m:m.group(1)+uuid.uuid4().hex,text);meta.write_text(text)
   text=meta.read_text()
   for k,val in [('textureType','8'),('spriteMode','1')]:text=re.sub(r'(?m)^(  '+k+r': )\d+',lambda m:m.group(1)+val,text)
   if text!=meta.read_text():meta.write_text(text)
  fm=Path(str(dest)+'.meta')
  if not fm.exists():fm.write_text('fileFormatVersion: 2\nguid: '+uuid.uuid4().hex+'\nfolderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n')
 report={'ids':ids,'frames':12,'size':[1254,1254],'outside_mask_unchanged':True,'background':'#17468C','unity_hashes_verified':True,'mode':'built-in image_gen and authorized local compositing','prompts':{'base':'Faithfully remaster supplied red-brown, cobalt blue and charcoal teal dragons. Preserve original long snout, swept horns, scales, pose and silhouette. Use existing dragon 733 only for crisp anime linework and cel shading style. Remove scenery; solid blue background.','1':'Slightly open long reptilian mouth along existing seam, quiet speaking not roaring.','2':'Naturally close mouth, preserve external fangs.','3':'Close visible eye with scaled eyelid.','invariants':'All non-mouth/non-eye regions unchanged between frames.'}}
 (p.ROOT/'validation_758_760.json').write_text(json.dumps(report,indent=2));print('12 frames synchronized to Unity')
