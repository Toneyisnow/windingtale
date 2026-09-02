import json, sys, os
from PIL import Image, ImageDraw
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import voxlib

ch = int(sys.argv[1]); scale = int(sys.argv[2]) if len(sys.argv)>2 else 5
cols = int(sys.argv[3]) if len(sys.argv)>3 else 10
out = sys.argv[4]
root = voxlib.workspace_root()
used = json.load(open(rf"{root}\WindingTale2\Assets\Resources\Data\Chapters\Chapter_{ch:02d}_UsedTiles.json"))
ids = used["tile_ids"] if isinstance(used, dict) else used
ids = sorted(int(i) for i in ids)
panel = rf"{root}\Resources\Original\Shapes\ShapePanel{ch:02d}"
tw = 24*scale; lab = 14; pad = 4
rows = (len(ids)+cols-1)//cols
img = Image.new("RGB", (cols*(tw+pad)+pad, rows*(tw+lab+pad)+pad), (30,30,30))
d = ImageDraw.Draw(img)
for i, tid in enumerate(ids):
    r, c = divmod(i, cols)
    x = pad + c*(tw+pad); y = pad + r*(tw+lab+pad)
    d.text((x+2, y+1), str(tid), fill=(255,255,0))
    p = os.path.join(panel, f"Shape_{ch-1}_{tid}.png")
    if os.path.exists(p):
        img.paste(Image.open(p).convert("RGB").resize((tw,tw), Image.NEAREST), (x, y+lab))
img.save(out); print("wrote", out, img.size, len(ids), "tiles")
