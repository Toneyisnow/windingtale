"""Contact sheet + core colour of a chapter's candidate tree tiles (Type 2, plus any ids given)."""
import sys, json, os
from PIL import Image, ImageDraw
from collections import Counter
import voxlib
root = voxlib.workspace_root()
nn = voxlib.nn(sys.argv[1]); extra = [int(t) for t in sys.argv[2:]]
d = json.load(open(voxlib.chapter_json_path(root, nn), encoding='utf-8-sig'))
counts = Counter(); [counts.update(c) for c in d['ShapeMatrix']]
ids = sorted(set(int(k) for k, v in d['Shapes'].items() if int(v.get('Type', 0)) == 2 and int(k) in counts) | set(extra))
S = 5; w = 24 * S + 6; per = 16
rows = (len(ids) + per - 1) // per
sheet = Image.new('RGB', (w * min(per, len(ids)), rows * (24 * S + 18)), (255, 255, 255)); dr = ImageDraw.Draw(sheet)
for i, t in enumerate(ids):
    p = os.path.join(voxlib.shape_panel_dir(root, nn), voxlib.tile_png_name(nn, t))
    im = Image.open(p).convert('RGB'); px = im.load()
    x, y = (i % per) * w, (i // per) * (24 * S + 18)
    sheet.paste(im.resize((24 * S, 24 * S), Image.NEAREST), (x, y + 16)); dr.text((x + 2, y + 2), '%d x%d' % (t, counts[t]), fill=(0, 0, 0))
    col = Counter(px[11, yy] for yy in range(8, 22))
    print('%s %4d x%-3d core %-16s centre-col %s  corner %s' % (nn, t, counts[t], px[11, 16], [c for c, _ in col.most_common(3)], px[1, 1]))
sheet.save('out/trees/sheet_%s.png' % nn); print('sheet', sheet.size)
