"""Which tiles a chapter can build a backdrop from, and where they sit on its map.

    python tile_ids.py NN               # usable tile ids, commonest first, with mean colour
    python tile_ids.py NN X0 Y0 X1 Y1   # the tile-id grid over that window of the map

A backdrop can only lay tiles that have a ``Shapes_NN`` VOX. The tiles that
were under trees and buildings were never generated, so a tile id read off the
map is not necessarily one you can use -- the first form lists only those you
can. ``sd`` is how busy the tile is: low is a flat colour, high is a pattern
(edges, cliffs, stairs, planks).
"""

import os
import re
import sys

_HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(_HERE, '..', '..', '..', '..'))
sys.path.insert(0, os.path.join(ROOT, 'Tools', 'MapPipeline'))

import chapter_map  # noqa: E402
import voxlib       # noqa: E402


def usable(nn):
    d = voxlib.shapes_vox_dir(ROOT, nn)
    return set(int(re.sub(r'\D', '', f.split('_')[-1])) for f in os.listdir(d))


def info(nn):
    ch, _ = chapter_map.load_chapter(ROOT, nn)
    have = usable(nn)
    cache, out = {}, []
    for tid, n in chapter_map.used_tile_ids(ch).most_common():
        if tid not in have:
            continue
        im = chapter_map.tile_image(ROOT, nn, tid, cache)
        if im is None:
            continue
        px = list(im.getdata())
        rgb = tuple(sum(p[k] for p in px) // len(px) for k in range(3))
        sd = int((sum((sum(p[:3]) - sum(rgb)) ** 2 for p in px) / len(px)) ** 0.5)
        out.append('%4d x%-4d rgb%s sd%d' % (tid, n, rgb, sd))
    print('== %s  %dx%d tiles' % (nn, ch['Width'], ch['Height']))
    for i in range(0, len(out), 3):
        print('  |  '.join(out[i:i + 3]))


def window(nn, x0, y0, x1, y1):
    ch, _ = chapter_map.load_chapter(ROOT, nn)
    m = ch['ShapeMatrix']                      # column-major: m[x][y]
    print('   ' + ''.join('%4d' % x for x in range(x0, x1)))
    for y in range(y0, y1):
        print('%3d' % y + ''.join('%4d' % m[x][y] for x in range(x0, x1)))


if __name__ == '__main__':
    nn = sys.argv[1].zfill(2)
    if len(sys.argv) >= 6:
        window(nn, *map(int, sys.argv[2:6]))
    else:
        info(nn)
