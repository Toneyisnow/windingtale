import sys,json,shutil
from PIL import Image,ImageDraw
sys.path.insert(0,'Resources/Remastered/Datos_Astra')
import process_portraits as p
r=json.loads((p.ROOT/'regions.json').read_text())
ids=sorted(d.name for d in p.ROOT.iterdir() if d.is_dir() and (d/'_neutral_closed_3.png').exists())
for i in ids:
 d=p.ROOT/i;f=d/f'Data_{i}_3.png'
 if not (d/'_before_neutral_closed_3.png').exists():shutil.copy2(f,d/'_before_neutral_closed_3.png')
 mask=p.composite(d/f'Data_{i}_0.png',d/'_neutral_closed_3.png',f,r[i]['eyes']);mask.save(d/'_edit_mask_3.png')
for start in range(0,len(ids),8):
 group=ids[start:start+8];out=Image.new('RGB',(1000,((len(group)+3)//4)*270),'white');dr=ImageDraw.Draw(out)
 for n,i in enumerate(group):
  out.paste(Image.open(p.ROOT/i/f'Data_{i}_3.png').resize((250,250)),((n%4)*250,(n//4)*270));dr.text(((n%4)*250,(n//4)*270+250),i,fill='black')
 out.save(p.ROOT/f'neutral_review_{start}.png')
print(ids)
