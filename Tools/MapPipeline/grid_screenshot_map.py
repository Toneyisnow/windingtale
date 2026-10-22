"""Cut a free-field map out of a map-editor screenshot: 24 px tiles under a blue
grid with a grey "0" printed in every tile (Maps 33 and 34).

The art is already at the game's 24 px tile size, so nothing is rescaled; what
has to go is what the editor drew over it:

1. the 1 px blue grid line on each tile's first row and column;
2. the grey "0" label, found as the pixels that are grey in most tiles at the
   same spot (plus a one-pixel rim for its anti-aliasing).

Every such pixel takes the colour of the nearest clean pixel along its row or
column, so the grass noise and the paths running under the label carry on.

``--pad N`` grows the map by N tiles on every side (Map 33 was too small): the new
cells are filled ring by ring outwards with the map's own plain ground tiles (no
paths or dirt), each picked at random among those whose edges carry on the colours
of the neighbours already placed, most often the commonest grass, so the joins do
not show. ``--seed`` fixes the draw. Positions in the scene file count on the grown
map.

Trees (``--trees free_maps/map_NN_trees.json``) become tree obstacles -- the
existing tree_<colour> models -- standing on the tile below each painted cone,
and every tile a tree is painted on is cleared to the nearest plain tile. The
cells then go through screenshot_map.write_map like Map 32's.

    python grid_screenshot_map.py 33 --image ../../Resources/Remastered/Maps/Map_33/Map-33-screenshot.png \\
        --origin 1,1 --size 18x8 --pad 8
    python grid_screenshot_map.py 34 --image ../../Resources/Remastered/Maps/Map_34/Map-34-screenshot.png \\
        --origin 2,4 --size 20x11 --trees free_maps/map_34_trees.json --pad 5
"""
import argparse
import json
import os

import numpy as np
from PIL import Image

from screenshot_map import write_map

HERE = os.path.dirname(os.path.abspath(__file__))
TILE = 24


def grey(t):
    sat = t.max(axis=2) - t.min(axis=2)
    mean = t.mean(axis=2)
    return (sat < 24) & (mean > 40) & (mean < 150)


def label_mask(tiles):
    """The "0": the tile pixels that are grey in most tiles, grown by one pixel."""
    votes = np.mean([grey(t) for t in tiles], axis=0) > 0.5
    grown = votes.copy()
    grown[1:, :] |= votes[:-1, :]
    grown[:-1, :] |= votes[1:, :]
    grown[:, 1:] |= votes[:, :-1]
    grown[:, :-1] |= votes[:, 1:]
    return grown


def fill(img, bad):
    """Each bad pixel takes the nearest good pixel's colour along its row or column
    (or a diagonal, where nothing is clean along either)."""
    out = img.copy()
    h, w = bad.shape
    ys, xs = np.nonzero(bad)
    for y, x in zip(ys, xs):
        best = None
        # Along the row and column first; the diagonals only matter where two grid
        # lines cross, since both axes through that pixel are grid line.
        for dy, dx in ((0, 1), (0, -1), (1, 0), (-1, 0), (1, 1), (1, -1), (-1, 1), (-1, -1)):
            for k in range(1, 12):
                yy, xx = y + dy * k, x + dx * k
                if not (0 <= yy < h and 0 <= xx < w):
                    break
                if not bad[yy, xx]:
                    if best is None or k < best[0]:
                        best = (k, yy, xx)
                    break
        if best is not None:
            out[y, x] = img[best[1], best[2]]
    return out


def edge_profile(px, side):
    """The colours along one edge, averaged over 3 px deep and 4 px long blocks, so a
    tile's grain does not count, only its tone (grass light or dark, sand)."""
    t = px.astype(float)
    strip = {"left": t[:, :3], "right": t[:, -3:], "top": t[:3, :].transpose(1, 0, 2),
             "bottom": t[-3:, :].transpose(1, 0, 2)}[side]
    return strip.mean(axis=1).reshape(6, 4, 3).mean(axis=1)


def is_plain(px):
    """Ground with nothing on it: no path or dirt pixels to speak of. The dirt is a red
    brown (red well above green); the sand is a yellow tan, and counts as ground."""
    t = px.astype(int)
    r, g, b = t[..., 0], t[..., 1], t[..., 2]
    brown = (r - g > 25) & (r - b > 40)
    return brown.mean() < 0.02


def sand_fraction(px):
    """How much of the tile is sand: the tan, lighter and much less green than grass."""
    t = px.astype(int)
    r, g, b = t[..., 0], t[..., 1], t[..., 2]
    return ((r > 95) & (r >= g - 10) & (r - b > 30) & (r - g <= 25)).mean()


# How much worse than the best join a padding tile's edges may be and still be drawn,
# in colour levels of the block-averaged edge (see edge_profile).
JOIN_TOLERANCE = 2.5


def pad_cells(cells, cols, rows, pad, seed):
    """Grow the map by ``pad`` tiles on every side; see the module docstring."""
    rng = np.random.RandomState(seed)
    library = {}
    counts = {}
    for key, px in cells.items():
        if not is_plain(px):
            continue
        sig = px.tobytes()
        library.setdefault(sig, px)
        counts[sig] = counts.get(sig, 0) + 1
    sigs = list(library)
    # The commoner a tile the likelier, and the less sand on it the likelier: the map
    # grows grass, and a sand patch at the old edge is closed off rather than spread.
    weight = np.array([counts[sg] for sg in sigs], dtype=float) ** 3
    weight *= np.array([(1.0 - sand_fraction(library[sg])) ** 6 + 1e-4 for sg in sigs])
    profiles = {sg: {side: edge_profile(library[sg], side) for side in ("left", "right", "top", "bottom")}
                for sg in sigs}

    W, H = cols + 2 * pad, rows + 2 * pad
    grid = {}
    for (c, r), px in cells.items():
        grid[(c + pad, r + pad)] = px

    def ring(c, r):
        return max(pad - c, c - (pad + cols - 1), pad - r, r - (pad + rows - 1), 0)

    order = sorted(((c, r) for c in range(W) for r in range(H) if (c, r) not in grid),
                   key=lambda p: (ring(*p), rng.rand()))
    for c, r in order:
        cost = np.zeros(len(sigs))
        for dc, dr, mine, theirs in ((-1, 0, "left", "right"), (1, 0, "right", "left"),
                                     (0, -1, "top", "bottom"), (0, 1, "bottom", "top")):
            other = grid.get((c + dc, r + dr))
            if other is None:
                continue
            edge = edge_profile(other, theirs)
            cost += np.array([np.abs(profiles[sg][mine] - edge).mean() for sg in sigs])
        # Anything within a few levels of the best join is fair game; among those the
        # commoner tiles (the plain grass) are drawn far more often.
        ok = cost <= cost.min() + JOIN_TOLERANCE
        p = weight * ok
        grid[(c, r)] = library[sigs[rng.choice(len(sigs), p=p / p.sum())]].copy()
    return grid, W, H


def trees_from(spec, cols, rows):
    """(obstacles, painted tiles) from the hand-read cone / top-body lists."""
    stands = {}
    painted = set()
    for colour, cells in spec.get("TopBodies", {}).items():
        for x, y in cells:
            stands.setdefault((x, y), colour)
            painted.add((x, y))
    for colour, cells in spec.get("Cones", {}).items():
        for x, y in cells:
            painted.add((x, y))
            if y + 1 <= rows:
                # The cone's own tree stands below it; a body seen there is that tree.
                stands[(x, y + 1)] = colour
    painted |= set(stands)
    obstacles = []
    for i, ((x, y), colour) in enumerate(sorted(stands.items(), key=lambda kv: (kv[0][1], kv[0][0])), start=1):
        obstacles.append({"Id": i, "DefinitionKey": spec["Models"][colour], "Position": {"X": x, "Y": y}})
    return obstacles, painted


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("map_id", type=int)
    ap.add_argument("--image", required=True)
    ap.add_argument("--origin", required=True, help="pixel x,y of the top-left tile (its grid line)")
    ap.add_argument("--size", required=True, help="COLSxROWS")
    ap.add_argument("--trees", default=None)
    ap.add_argument("--pad", type=int, default=0, help="grow the map by this many tiles on every side")
    ap.add_argument("--seed", type=int, default=33)
    ap.add_argument("--out", default=None)
    args = ap.parse_args()

    x0, y0 = (int(v) for v in args.origin.split(","))
    cols, rows = (int(v) for v in args.size.lower().split("x"))
    out = args.out or os.path.join(HERE, "out", "map_%02d" % args.map_id)
    os.makedirs(out, exist_ok=True)

    img = np.asarray(Image.open(args.image).convert("RGB")).astype(np.int16)
    region = img[y0:y0 + rows * TILE, x0:x0 + cols * TILE].copy()
    if region.shape[:2] != (rows * TILE, cols * TILE):
        raise SystemExit("the grid runs off the screenshot")

    tiles = [region[r * TILE:(r + 1) * TILE, c * TILE:(c + 1) * TILE] for r in range(rows) for c in range(cols)]
    label = label_mask(tiles)
    bad = np.tile(label, (rows, cols))
    bad[::TILE, :] = True          # grid rows
    bad[:, ::TILE] = True          # grid columns
    # ...and any stray pure-blue pixel of the grid (the crossings draw one more).
    bad |= (region[..., 2] > 150) & (region[..., 0] < 90) & (region[..., 1] < 90)
    clean = fill(region, bad)
    Image.fromarray(clean.astype(np.uint8)).save(os.path.join(out, "cleaned_screenshot.png"))

    cells = {}
    for r in range(rows):
        for c in range(cols):
            cells[(c, r)] = clean[r * TILE:(r + 1) * TILE, c * TILE:(c + 1) * TILE].copy()

    obstacles = []
    if args.trees:
        with open(args.trees, encoding="utf-8") as f:
            spec = json.load(f)
        obstacles, painted = trees_from(spec, cols, rows)
        plain = [(c, r) for r in range(rows) for c in range(cols) if (c + 1, r + 1) not in painted]
        for (x, y) in painted:
            # Nearest unpainted tile; the same row wins a tie, so the lawn's shading
            # (lighter on the left, darker to the bottom right) carries on.
            nc, nr = min(plain, key=lambda p: (abs(p[0] - (x - 1)) + abs(p[1] - (y - 1)), abs(p[1] - (y - 1))))
            cells[(x - 1, y - 1)] = cells[(nc, nr)].copy()

    if args.pad > 0:
        cells, cols, rows = pad_cells(cells, cols, rows, args.pad, args.seed)
        for o in obstacles:
            o["Position"]["X"] += args.pad
            o["Position"]["Y"] += args.pad

    n = write_map(args.map_id, cells, obstacles, cols, rows, out)
    print("map %d: %d x %d, %d tiles, %d obstacles -> %s" % (args.map_id, cols, rows, n, len(obstacles), out))


if __name__ == "__main__":
    main()
