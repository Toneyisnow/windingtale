import json, sys
from PIL import Image, ImageDraw
ROOT = r'D:\SourceCode\Git\toneyisnow\windingtale'
def lcrop(nn, x1, y1, x2, y2, scale, out, src=None):
    src = src or rf'{ROOT}\Resources\Original\Maps\{nn}\Chapter-{nn}.png'
    im = Image.open(src).convert('RGB')
    im = im.crop(((x1-1)*24, (y1-1)*24, x2*24, y2*24))
    im = im.resize((im.width*scale, im.height*scale), Image.NEAREST)
    d = ImageDraw.Draw(im)
    s = 24*scale
    for i in range(x2-x1+2):
        d.line([(i*s,0),(i*s,im.height)], fill=(255,0,0), width=1)
    for j in range(y2-y1+2):
        d.line([(0,j*s),(im.width,j*s)], fill=(255,0,0), width=1)
    for i in range(x2-x1+1):
        d.text((i*s+2, 1), str(x1+i), fill=(255,255,0))
    for j in range(y2-y1+1):
        d.text((2, j*s+10), str(y1+j), fill=(0,255,255))
    im.save(out)
    print('wrote', out, im.size)
