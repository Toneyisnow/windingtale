"""Cut a chapter's black void out of its render map, so the sky shows through.

Chapters 23, 24 and 28 float in a black void (chapter 23's top corners, all of
chapter 24 around its island, chapter 28's top-right corner). In the tile PNGs
that void is not opaque black -- it is alpha 0 -- and ``shapes_to_vox.py``
used to read it as black voxels, laying a black slab over the sky. This step
runs before the shape VOXs are generated and turns the void into nothing:

  1. paint the chapter's cleaned map (``Chapter_NN_Cleaned.json``'s
     ShapeMatrix, the map ``RenderMatrix`` is made from) into one RGBA image
  2. flood-fill the alpha-0 pixels 4-connected to the chapter's seed corners
     (``VOID_SEEDS``). Only the void connected to those corners goes: chapter
     28's wire-mesh holes are alpha 0 too, but they are not connected to the
     top-right corner and stay black pits
  2b. chapter 24 only (``SHAFT_CUT``): its three light shafts are cut away
     too, and written into the chapter as "LightShafts" -- the game stands
     them up as 3D columns hanging below the board
  3. chapter 23 only (``ROCK_TRIM``): the cliff rock in the bottom corners
     keeps the one layer of tiles 8-touching the flat ground, and every rock
     tile behind that layer goes as well
  4. each map cell becomes its tile with the void pixels cut out. A cell with
     nothing left is ``-1`` in ``RenderMatrix`` -- ShapesLayer draws no model
     there, only the click target -- and cells whose cut tile is the same as
     (or close to, ``SIMILAR_*``) another's share one model
  5. write the cut tiles, as PNGs with the void at alpha 0 and every other
     pixel opaque (an alpha-0 pixel that is not void -- a mesh hole -- turns
     opaque black, which is what it always rendered as), into
     ``out/void_NN/panel``, and the ids still drawn into
     ``out/void_NN/UsedTiles.json``

Then ``shapes_to_vox.py NN --panel-dir out/void_NN/panel --used-tiles
out/void_NN/UsedTiles.json --skip-transparent ...`` builds the models with
the void left empty.

A tile id whose cut differs between cells (a void edge tile used on both sides
of the island) keeps its own id for its most common cut; each other cut gets a
new id from ``NEW_ID_BASE`` up. ShapeMatrix is never touched: these ids only
pick models.

The step is deterministic, so ``install_chapter.py`` re-applies it whenever it
installs one of these chapters (it would otherwise reset RenderMatrix to the
cleaned map). Only the chapters below are handled; every other chapter keeps
its black.

    python void_tiles.py 23 --dry-run
    python void_tiles.py 23
"""

import argparse
import json
import os
from collections import Counter, OrderedDict, deque

from PIL import Image

import map_clean
import voxlib

# Where the void is flooded from: 1-based tile cells, the flood starting at that
# cell's corner pixel on the map's edge -- or 'border', every pixel along the
# map's edge (chapter 24: the black between its light shafts is closed off from
# the corners by the shafts and the island). Hard-coded on purpose -- these are
# the only chapters whose black is void rather than painted ground; chapter 28
# must not use 'border', its mesh holes reach the bottom edge.
VOID_SEEDS = {
    '23': [(1, 1), (41, 1)],    # top-left and top-right
    '24': 'border',             # everything round the island
    '28': [(48, 1)],            # top-right
}

# Chapter 23's bottom corners are rows of cliff rock: 34, and the edge tiles
# 192..197 where it meets the dirt. Only rock cells that 8-touch a cell that is
# neither rock nor void stay; the rest are cut away whole.
ROCK_TRIM = {
    '23': {'tiles': frozenset([34, 192, 193, 194, 195, 196, 197]),
           'rows': (26, 40)},
}

# Chapter 24's three light shafts run from under the island off the bottom of the
# map, painted in these seven exact colours (a 6-pixel orange ramp each side of a
# pale core) and nothing else on the map uses them. They are cut away with the
# void -- flooded across those colours from the map's bottom edge -- and stood up
# again as real 3D columns hanging below the board (ShapesLayer / LightShaft.cs),
# from the "LightShafts" this writes into Chapter_24.json: one per shaft, its
# centre X and width in tiles, the tile Y of its front face (TOP_MARGIN pixels
# north of where the art first shows it, so the top tucks under the cliff), and
# the ramp's width in pixels.
SHAFT_CUT = {
    '24': {'colours': frozenset([(168, 84, 0), (192, 108, 0), (216, 132, 0), (240, 156, 12),
                                 (252, 188, 48), (252, 220, 104), (252, 252, 212)]),
           'edge_pixels': 6},
}
TOP_MARGIN = 4

# Ids for a tile's second, third... distinct cut. Above every panel id.
NEW_ID_BASE = 1000

# Two cuts share a model when their void masks are identical and at most
# SIMILAR_PIXELS of their opaque pixels differ by more than SIMILAR_DISTANCE
# (summed RGB difference). Catches the void-edge tiles the 2D art draws twice.
SIMILAR_PIXELS = 0.05
SIMILAR_DISTANCE = 60

T = voxlib.TILE


def out_dir(nn):
    return os.path.join(os.path.dirname(os.path.abspath(__file__)), 'out', 'void_%s' % nn)


def chapters_dir(root):
    return os.path.dirname(voxlib.chapter_json_path(root, '01'))


def load_json(path):
    with open(path, 'r', encoding='utf-8-sig') as f:
        return json.load(f, object_pairs_hook=OrderedDict)


def tile_rgba(root, nn, tile_id, cache):
    if tile_id not in cache:
        p = os.path.join(voxlib.shape_panel_dir(root, nn), voxlib.tile_png_name(nn, tile_id))
        if not os.path.isfile(p):
            raise SystemExit('tile PNG not found: %s' % p)
        cache[tile_id] = Image.open(p).convert('RGBA')
    return cache[tile_id]


def flood_void(image, seeds):
    """Pixels of alpha 0 4-connected to any seed pixel."""
    w, h = image.size
    alpha = image.getchannel('A').load()
    void = bytearray(w * h)
    queue = deque()
    for sx, sy in seeds:
        if alpha[sx, sy] == 0 and not void[sy * w + sx]:
            void[sy * w + sx] = 1
            queue.append((sx, sy))
    while queue:
        x, y = queue.popleft()
        for nx, ny in ((x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1)):
            if 0 <= nx < w and 0 <= ny < h and not void[ny * w + nx] and alpha[nx, ny] == 0:
                void[ny * w + nx] = 1
                queue.append((nx, ny))
    return void


def seed_pixel(cell, width, height):
    """The pixel of a 1-based cell that sits on the map's outer corner."""
    cx, cy = cell
    px = 0 if cx == 1 else (width * T - 1 if cx == width else (cx - 1) * T)
    py = 0 if cy == 1 else (height * T - 1 if cy == height else (cy - 1) * T)
    return px, py


def flood_shafts(image, colours, void):
    """Opaque pixels of the shaft colours 4-connected to the map's bottom edge
    are added to ``void``; returns the shafts as (x0, x1, top) pixel spans, one
    per run of them along the bottom row."""
    w, h = image.size
    px = image.load()

    def shaft(x, y):
        p = px[x, y]
        return p[3] != 0 and p[:3] in colours

    seen = bytearray(w * h)
    queue = deque()
    for x in range(w):
        if shaft(x, h - 1):
            seen[(h - 1) * w + x] = 1
            queue.append((x, h - 1))
    while queue:
        x, y = queue.popleft()
        for nx, ny in ((x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1)):
            if 0 <= nx < w and 0 <= ny < h and not seen[ny * w + nx] and shaft(nx, ny):
                seen[ny * w + nx] = 1
                queue.append((nx, ny))

    runs = []
    start = None
    for x in range(w + 1):
        on = x < w and seen[(h - 1) * w + x]
        if on and start is None:
            start = x
        elif not on and start is not None:
            runs.append([start, x - 1])
            start = None
    spans = []
    for x0, x1 in runs:
        top = min(y for y in range(h) for x in range(x0, x1 + 1) if seen[y * w + x])
        spans.append((x0, x1, top))
    for i in range(w * h):
        if seen[i]:
            void[i] = 1
    return spans


def shaft_definitions(spans, edge_pixels):
    """The chapter JSON's "LightShafts", in tile units (tile X spans pixels
    (X-1)*24 .. X*24, so pixel p is tile p/24 + 0.5)."""
    out = []
    for x0, x1, top in spans:
        out.append(OrderedDict([
            ('X', round((x0 + x1 + 1) / 2.0 / T + 0.5, 4)),
            ('Y', round((top - TOP_MARGIN) / float(T) + 0.5, 4)),
            ('Width', round((x1 - x0 + 1) / float(T), 4)),
            ('EdgePixels', edge_pixels),
        ]))
    return out


def seed_pixels(seeds, width, height):
    if seeds == 'border':
        w, h = width * T, height * T
        return ([(x, 0) for x in range(w)] + [(x, h - 1) for x in range(w)]
                + [(0, y) for y in range(h)] + [(w - 1, y) for y in range(h)])
    return [seed_pixel(c, width, height) for c in seeds]


def cut_cell(tile, void, map_w, cx, cy):
    """(key, image, cut) for one map cell: the tile with the void pixels at
    alpha 0 and everything else opaque. key is None when nothing is left; cut
    says whether any pixel was void."""
    src = tile.load()
    out = Image.new('RGBA', (T, T), (0, 0, 0, 0))
    dst = out.load()
    any_left = cut = False
    for py in range(T):
        row = (cy * T + py) * map_w
        for px in range(T):
            if void[row + cx * T + px]:
                cut = True
                continue
            r, g, b, a = src[px, py]
            dst[px, py] = (r, g, b, 255) if a else (0, 0, 0, 255)
            any_left = True
    if not any_left:
        return None, None, cut
    return out.tobytes(), out, cut


def similar(a, b):
    """True when two cut tiles (RGBA bytes) share a void mask and are close in colour."""
    differ = opaque = 0
    for i in range(0, len(a), 4):
        if (a[i + 3] == 0) != (b[i + 3] == 0):
            return False
        if a[i + 3] == 0:
            continue
        opaque += 1
        if abs(a[i] - b[i]) + abs(a[i + 1] - b[i + 1]) + abs(a[i + 2] - b[i + 2]) > SIMILAR_DISTANCE:
            differ += 1
    return opaque > 0 and differ <= opaque * SIMILAR_PIXELS


def trim_rock(matrix, width, height, void_cells, spec):
    """Rock cells that touch no flat ground -- they are cut away whole."""
    rock = spec['tiles']
    y0, y1 = spec['rows']
    gone = set()
    for x in range(width):
        for y in range(y0 - 1, y1):
            if matrix[x][y] not in rock:
                continue
            keep = False
            for dx in (-1, 0, 1):
                for dy in (-1, 0, 1):
                    nx, ny = x + dx, y + dy
                    if (dx or dy) and 0 <= nx < width and 0 <= ny < height:
                        if matrix[nx][ny] not in rock and (nx, ny) not in void_cells:
                            keep = True
            if not keep:
                gone.add((x, y))
    return gone


def build(root, nn):
    """-> (render matrix, {id: PIL image}, report lines) for one chapter."""
    if nn not in VOID_SEEDS:
        raise SystemExit('chapter %s has no void -- only %s are handled'
                         % (nn, ', '.join(sorted(VOID_SEEDS))))
    cleaned = load_json(os.path.join(chapters_dir(root), 'Chapter_%s_Cleaned.json' % nn))
    matrix = cleaned['ShapeMatrix']
    width, height = cleaned['Width'], cleaned['Height']

    cache = {}
    image = Image.new('RGBA', (width * T, height * T), (0, 0, 0, 0))
    for x in range(width):
        for y in range(height):
            image.paste(tile_rgba(root, nn, matrix[x][y], cache), (x * T, y * T))
    void = flood_void(image, seed_pixels(VOID_SEEDS[nn], width, height))
    shafts = []
    if nn in SHAFT_CUT:
        spec = SHAFT_CUT[nn]
        shafts = shaft_definitions(flood_shafts(image, spec['colours'], void), spec['edge_pixels'])
    map_w = width * T

    # Cut every cell; a cell that is all void is -1.
    cuts = {}          # (x, y) -> key
    images = {}        # key -> image
    void_cells = set()
    partial = 0
    for x in range(width):
        for y in range(height):
            key, img, cut = cut_cell(cache[matrix[x][y]], void, map_w, x, y)
            partial += 1 if key is not None and cut else 0
            if key is None:
                void_cells.add((x, y))
            else:
                cuts[(x, y)] = key
                images.setdefault(key, img)
    trimmed = set()
    if nn in ROCK_TRIM:
        trimmed = trim_rock(matrix, width, height, void_cells, ROCK_TRIM[nn])
        for cell in trimmed:
            cuts.pop(cell, None)

    # Fold near-duplicate cuts onto the most used one of their group.
    use = Counter(cuts.values())
    canon = {}
    reps = []
    for key, _n in sorted(use.items(), key=lambda kv: -kv[1]):
        for rep in reps:
            if key == rep or similar(key, rep):
                canon[key] = rep
                break
        else:
            reps.append(key)
            canon[key] = key

    # Name each representative cut: a tile id keeps its own id for its most
    # used cut, other cuts of it get fresh ids.
    rep_use = Counter()
    rep_source = {}
    for cell, key in cuts.items():
        rep = canon[key]
        rep_use[rep] += 1
        rep_source.setdefault(rep, Counter())[matrix[cell[0]][cell[1]]] += 1
    names = {}
    taken = set()
    next_id = NEW_ID_BASE
    for rep in sorted(rep_use, key=lambda r: -rep_use[r]):
        source = rep_source[rep].most_common(1)[0][0]
        if source not in taken:
            names[rep] = source
            taken.add(source)
        else:
            names[rep] = next_id
            next_id += 1

    render = [[-1] * height for _ in range(width)]
    for (x, y), key in cuts.items():
        render[x][y] = names[canon[key]]

    tiles = {names[rep]: images[rep] for rep in rep_use}
    before = set(i for col in matrix for i in col)
    report = [
        'chapter %s   %d x %d   void flooded from %s' % (nn, width, height, VOID_SEEDS[nn]),
        '  %d cells all void, %d rock cells trimmed -> -1; %d cells drawn, %d of them half void'
        % (len(void_cells), len(trimmed - void_cells), len(cuts), partial),
        '  tile models: %d before, %d after (%d new ids for second cuts: %s)'
        % (len(before), len(tiles), next_id - NEW_ID_BASE,
           ', '.join(str(i) for i in sorted(tiles) if i >= NEW_ID_BASE) or '-'),
        '  %d distinct cuts folded into %d by similarity' % (len(use), len(reps)),
    ]
    for shaft in shafts:
        report.append('  light shaft X %(X)s  Y %(Y)s  width %(Width)s tiles' % shaft)
    return render, tiles, shafts, report


def apply_to_chapter(chapter, root, nn):
    """Replace a chapter document's RenderMatrix with the void-cut one (used by
    install_chapter.py). Returns the report lines."""
    render, _tiles, shafts, report = build(root, nn)
    chapter['RenderMatrix'] = render
    set_shafts(chapter, shafts)
    return report


def set_shafts(chapter, shafts):
    """Write (or drop) the chapter's "LightShafts", right after its Obstacles."""
    chapter.pop('LightShafts', None)
    if not shafts:
        return
    items = list(chapter.items())
    chapter.clear()
    for key, value in items:
        chapter[key] = value
        if key == 'Obstacles':
            chapter['LightShafts'] = shafts
    if 'LightShafts' not in chapter:
        chapter['LightShafts'] = shafts


def main():
    p = argparse.ArgumentParser(description=__doc__,
                                formatter_class=argparse.RawDescriptionHelpFormatter)
    p.add_argument('chapter')
    p.add_argument('--root')
    p.add_argument('--dry-run', action='store_true')
    args = p.parse_args()

    root = args.root or voxlib.workspace_root()
    nn = voxlib.nn(args.chapter)
    render, tiles, shafts, report = build(root, nn)
    for line in report:
        print(line)

    dest = voxlib.chapter_json_path(root, nn)
    folder = out_dir(nn)
    panel = os.path.join(folder, 'panel')
    if args.dry_run:
        print('  would write %d tiles to %s and RenderMatrix to %s' % (len(tiles), panel, dest))
        return

    if os.path.isdir(panel):
        for f in os.listdir(panel):
            os.remove(os.path.join(panel, f))
    else:
        os.makedirs(panel)
    for tid, img in tiles.items():
        img.save(os.path.join(panel, voxlib.tile_png_name(nn, tid)))
    with open(os.path.join(folder, 'UsedTiles.json'), 'w', encoding='utf-8') as f:
        json.dump({'chapter': nn, 'tile_ids': sorted(tiles)}, f, indent=2)
        f.write('\n')

    chapter = load_json(dest)
    if 'RenderMatrix' not in chapter:
        raise SystemExit('%s has no RenderMatrix -- run install_chapter.py first' % dest)
    chapter['RenderMatrix'] = render
    set_shafts(chapter, shafts)
    map_clean.dump_chapter(chapter, dest)
    print('  wrote %d tiles to %s' % (len(tiles), panel))
    print('  wrote RenderMatrix to %s' % dest)


if __name__ == '__main__':
    main()
