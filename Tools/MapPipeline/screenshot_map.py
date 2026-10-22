"""Cut a free-field map out of a screenshot into 24x24 tiles.

Map 32 (the royal throne hall) exists only as a stitched screenshot, not as a
ShapePanel + ShapeMatrix like the battle chapters. This script turns it into the
same kind of data the rest of the pipeline eats:

1. the screenshot is cut on its tile grid -- the grid the creatures stand on,
   13 columns of 670/13 px, rows found from the guards' feet;
2. the creatures painted into it (king, queen, guards, the hero) are erased by
   copying clean ground over them from one tile aside, where the ground repeats;
3. the screenshot's own scroll handles on the right edge are erased the same way;
4. the props that already exist as obstacle models (statues, ball pillars, tall
   columns, shrines) are punched out and replaced by plain floor;
5. every cell is box-filtered down to 24x24 and near-identical cells are merged
   into one tile id.

Writes out/map_NN/panel/Shape_<NN-1>_<id>.png (the name shapes_to_vox expects),
out/map_NN/UsedTiles.json, out/map_NN/preview.png (the cleaned map, labelled)
and out/map_NN/map.json (RenderMatrix + obstacles), which build_free_map.py
turns into Data/Maps/Map_NN.json.

    python screenshot_map.py 32 --image <screenshot.png>
"""
import argparse
import json
import os

import numpy as np
from PIL import Image, ImageDraw

HERE = os.path.dirname(os.path.abspath(__file__))

# ---- map 32 -----------------------------------------------------------------
COLS = 13
ROWS = 39
# Row r's top edge in screenshot pixels is ROW0 + r * TILE. Row 0 is only half
# on screen (the top wall); its missing top is padded from its first line.
TILE = 670.0 / COLS
ROW0 = 541.0 - 11 * TILE

# Creatures painted into the screenshot, as pixel boxes (x0, y0, x1, y1) and
# where to copy clean ground from: (dx, dy) in pixels.
T = TILE
CREATURE_PATCHES = [
    # upper carpet guards: carpet repeats sideways
    ((94, 536, 160, 598), (66, 0)), ((456, 536, 522, 598), (-66, 0)),
    ((94, 638, 160, 700), (66, 0)), ((456, 638, 522, 700), (-66, 0)),
    ((94, 898, 160, 959), (66, 0)), ((456, 898, 522, 959), (-66, 0)),
    # the hero (four times -- the screenshot was stitched while he walked)
    ((255, 272, 312, 328), (-60, 0)), ((255, 838, 312, 894), (-60, 0)),
    ((255, 935, 312, 991), (-60, 0)), ((255, 1755, 312, 1811), (-60, 0)),
    # lower guards straddle the carpet edge: the edge repeats downwards
    ((150, 1058, 208, 1112), (0, 2 * T)), ((410, 1058, 466, 1112), (0, 2 * T)),
    ((150, 1263, 208, 1318), (0, 2 * T)), ((410, 1263, 466, 1318), (0, 2 * T)),
    ((150, 1470, 208, 1525), (0, -2 * T)), ((410, 1470, 466, 1525), (0, -2 * T)),
    ((150, 1678, 208, 1733), (0, -2 * T)), ((410, 1678, 466, 1733), (0, -2 * T)),
    ((150, 1885, 208, 1940), (0, -2 * T)), ((410, 1885, 466, 1940), (0, -2 * T)),
]

# The screenshot's scroll handles hugging the right edge: x from, and their
# row spans (found as the dark runs in the last 10 px that are not map art).
EDGE_HANDLE_X = 654
EDGE_HANDLE_ROWS = [(191, 229), (421, 463), (791, 828), (1086, 1123), (1397, 1434), (1777, 1814)]

# Thrones: the king and queen sit on them. The seated figure is painted over
# with the throne's own seat colour; the creature models sit there anyway.
THRONE_FIGURES = [(205, 38, 258, 98), (360, 38, 412, 98)]

# Props that are obstacle models already, 1-based (X, Y) of the tile they stand
# on, and the rectangle of painted rows they cover (cleared to floor).
OBSTACLES = [
    # key, stand (X, Y), clear (X, Y, cols, rows)
    ("stone_statue_1", (13, 3), (13, 2, 1, 3)),
    ("stone_pillar_1", (1, 13), (1, 12, 1, 3)),
    ("stone_pillar_1", (12, 13), (12, 12, 1, 3)),
    ("stone_column_1", (3, 24), (3, 21, 1, 6)),
    ("stone_column_1", (10, 24), (10, 21, 1, 6)),
    ("stone_column_1", (3, 30), (3, 27, 1, 5)),
    ("stone_column_1", (10, 30), (10, 27, 1, 5)),
    ("stone_pillar_1", (3, 33), (3, 32, 1, 3)),
    ("stone_pillar_1", (10, 33), (10, 32, 1, 3)),
    ("stone_shrine_1", (1, 37), (1, 35, 3, 5)),
    ("stone_shrine_1", (10, 37), (10, 35, 3, 5)),
]

# A cell of plain floor to fill cleared obstacle tiles with (1-based).
FLOOR_SAMPLE = (12, 20)

# Two cells are one tile when they differ by little on average AND nowhere by
# much: a one-pixel carpet border is a small mean but a large peak.
MERGE_MEAN = 4.0
# The pools' water, snapped to one exact colour so shapes_to_vox --lower can
# sink it (it matches colours exactly; the screenshot's compression does not).
POOL_RGB = (105, 137, 204)
POOL_SNAP = 22
MERGE_PEAK = 40


def erase_creatures(img):
    a = img.copy()
    for (x0, y0, x1, y1), (dx, dy) in CREATURE_PATCHES:
        x0, y0, x1, y1 = int(x0), int(y0), int(x1), int(y1)
        dx, dy = int(round(dx)), int(round(dy))
        a[y0:y1, x0:x1] = img[y0 + dy:y1 + dy, x0 + dx:x1 + dx]
    return a


def erase_edge_handles(a):
    """Copy the column just left of each handle over its rows."""
    for y0, y1 in EDGE_HANDLE_ROWS:
        for y in range(y0 - 4, y1 + 5):
            a[y, EDGE_HANDLE_X:, :] = a[y, EDGE_HANDLE_X - 1, :]
    return a


def erase_throne_figures(a):
    for x0, y0, x1, y1 in THRONE_FIGURES:
        seat = a[y0 + 22:y1 - 8, x0 - 6:x0 - 2].reshape(-1, 3)
        colour = np.median(seat, axis=0).astype(np.uint8)
        a[y0:y1, x0:x1] = colour
    return a


def cell_box(col, row):
    x0 = col * TILE
    y0 = ROW0 + row * TILE
    return x0, y0, x0 + TILE, y0 + TILE


def cut_cells(a):
    h = a.shape[0]
    pad = int(np.ceil(max(0.0, -ROW0))) + 1
    padded = np.concatenate([np.repeat(a[:1], pad, axis=0), a], axis=0)
    im = Image.fromarray(padded)
    cells = {}
    for col in range(COLS):
        for row in range(ROWS):
            x0, y0, x1, y1 = cell_box(col, row)
            box = (x0, y0 + pad, x1, y1 + pad)
            if y1 > h:
                raise SystemExit("row %d runs off the screenshot" % row)
            cells[(col, row)] = np.asarray(
                im.resize((24, 24), Image.BOX, box=box)).astype(np.int16)
    return cells



def write_map(map_id, cells, obstacles, cols, rows, out):
    """Merge near-identical cells into tiles and write what the rest of the pipeline
    reads: panel/Shape_<NN-1>_<id>.png, UsedTiles.json, map.json, plus preview.png
    (the map rebuilt from its tiles, ids and obstacles marked) and tiles.png.
    Returns the number of tiles."""
    panel = os.path.join(out, "panel")
    os.makedirs(panel, exist_ok=True)
    for f in os.listdir(panel):
        os.remove(os.path.join(panel, f))
    # Merge near-identical cells; ids in reading order of first appearance.
    tiles = []          # list of (id, pixels, count)
    matrix = [[0] * rows for _ in range(cols)]   # [x][y], as ShapeMatrix
    for row in range(rows):
        for col in range(cols):
            px = cells[(col, row)]
            best = None
            for k, (tid, ref, n) in enumerate(tiles):
                diff = np.abs(px - ref).max(axis=2)
                d = diff.mean()
                if d < MERGE_MEAN and np.percentile(diff, 98) < MERGE_PEAK and (best is None or d < best[0]):
                    best = (d, k)
            if best is None:
                tiles.append((len(tiles), px, 1))
                matrix[col][row] = len(tiles) - 1
            else:
                tid, ref, n = tiles[best[1]]
                tiles[best[1]] = (tid, ref, n + 1)
                matrix[col][row] = tid

    for tid, px, n in tiles:
        Image.fromarray(px.astype(np.uint8)).save(
            os.path.join(panel, "Shape_%d_%d.png" % (map_id - 1, tid)))

    with open(os.path.join(out, "UsedTiles.json"), "w") as f:
        json.dump({"tile_ids": sorted(t[0] for t in tiles)}, f)
    with open(os.path.join(out, "map.json"), "w") as f:
        json.dump({"Width": cols, "Height": rows, "RenderMatrix": matrix,
                   "Obstacles": obstacles}, f)

    # Labelled preview at 2x, plus a contact sheet of the tiles.
    s = 48
    prev = Image.new("RGB", (cols * s, rows * s))
    d = ImageDraw.Draw(prev)
    for col in range(cols):
        for row in range(rows):
            tid = matrix[col][row]
            im = Image.fromarray(tiles[tid][1].astype(np.uint8)).resize((s, s), Image.NEAREST)
            prev.paste(im, (col * s, row * s))
    for col in range(cols):
        for row in range(rows):
            d.text((col * s + 2, row * s + 2), str(matrix[col][row]), fill=(255, 255, 0))
    for o in obstacles:
        x, y = o["Position"]["X"] - 1, o["Position"]["Y"] - 1
        d.rectangle([x * s, y * s, x * s + s - 1, y * s + s - 1], outline=(255, 0, 255), width=2)
    prev.save(os.path.join(out, "preview.png"))

    per = 16
    sheet = Image.new("RGB", (per * 64, ((len(tiles) + per - 1) // per) * 76), (60, 60, 60))
    d = ImageDraw.Draw(sheet)
    for tid, px, n in tiles:
        x, y = (tid % per) * 64, (tid // per) * 76
        sheet.paste(Image.fromarray(px.astype(np.uint8)).resize((48, 48), Image.NEAREST), (x + 8, y + 14))
        d.text((x + 8, y + 1), "%d x%d" % (tid, n), fill=(255, 255, 255))
    sheet.save(os.path.join(out, "tiles.png"))

    return len(tiles)


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("map_id", type=int)
    ap.add_argument("--image", required=True)
    ap.add_argument("--out", default=None)
    args = ap.parse_args()
    if args.map_id != 32:
        raise SystemExit("only map 32 is described in this script")

    out = args.out or os.path.join(HERE, "out", "map_%02d" % args.map_id)
    os.makedirs(out, exist_ok=True)

    a = np.asarray(Image.open(args.image).convert("RGB")).copy()
    a = erase_creatures(a)
    a = erase_edge_handles(a)
    a = erase_throne_figures(a)
    Image.fromarray(a).save(os.path.join(out, "cleaned_screenshot.png"))

    cells = cut_cells(a)
    for key, px in cells.items():
        near = np.abs(px - np.array(POOL_RGB)).sum(axis=2) < POOL_SNAP
        px[near] = POOL_RGB

    floor = cells[(FLOOR_SAMPLE[0] - 1, FLOOR_SAMPLE[1] - 1)]
    obstacles = []
    for i, (key, (sx, sy), (cx, cy, cw, chh)) in enumerate(OBSTACLES, start=1):
        for x in range(cx, cx + cw):
            for y in range(cy, cy + chh):
                if 1 <= x <= COLS and 1 <= y <= ROWS:
                    cells[(x - 1, y - 1)] = floor
        obstacles.append({"Id": i, "DefinitionKey": key, "Position": {"X": sx, "Y": sy}})

    n = write_map(args.map_id, cells, obstacles, COLS, ROWS, out)
    print("map %d: %d x %d, %d tiles, %d obstacles -> %s" % (
        args.map_id, COLS, ROWS, n, len(obstacles), out))

if __name__ == "__main__":
    main()
