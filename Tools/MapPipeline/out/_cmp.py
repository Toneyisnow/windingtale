"""Find where a tile-rect crop from one chapter's map also appears in another's."""
import sys
from PIL import Image
import numpy as np

ROOT = r'D:\SourceCode\Git\toneyisnow\windingtale'
def mapimg(nn):
    return np.asarray(Image.open(rf'{ROOT}\Resources\Original\Maps\{nn}\Chapter-{nn}.png').convert('RGB'))

def crop_tiles(nn, x1, y1, x2, y2):
    a = mapimg(nn)
    return a[(y1-1)*24:y2*24, (x1-1)*24:x2*24]

def best_match(pat, target, topn=5):
    ph, pw = pat.shape[:2]
    th, tw = target.shape[:2]
    res = []
    for oy in range(0, th-ph+1):
        for ox in range(0, tw-pw+1):
            sub = target[oy:oy+ph, ox:ox+pw]
            diff = np.count_nonzero(np.any(sub != pat, axis=2))
            res.append((diff, ox, oy))
    res.sort()
    return res[:topn]
