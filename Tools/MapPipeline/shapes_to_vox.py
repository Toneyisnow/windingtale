"""Turn 24x24 tile PNGs into the 40^3 shape VOX models the board is built from.

Step 3 of the 2D -> 3D workflow. Every rule below was reverse-engineered from
the hand-built chapter 01 pair (ShapePanel01/Shape_0_*.png -> Shapes_01/vox/
Shape_1_*.vox) and is checked by ``validate_shapes.py``:

  * each pixel colour becomes the nearest entry in the MagicaVoxel default
    palette (indices 1..255)
  * one voxel per tile pixel, at (px, 23 - py, level)
  * level: land 23, sand 22, water 21, deep water 20 -- so the shoreline steps
    down away from the land and deep water is the lowest
  * dark green (51,102,0) is grass: two more voxels of the same colour are
    stacked above it, giving 3-voxel blades
  * a tile whose art contains a tree gets its ground replaced by the plain
    grass tile (Shape_0_52 in panel 01) -- the painted 2D tree disappears --
    and a 3D crown is stamped on top, copied out of one of the six chapter 01
    tree tiles (46, 40, 43, 44, 47, 42, shortest to tallest)

Which tiles have trees is a judgement call made from the art, so it is passed
in rather than detected.

Examples
--------
    # everything the cleaned chapter still uses
    python shapes_to_vox.py 02 --used-tiles Chapter_02_UsedTiles.json

    # ...with a default-height tree on tile 71 and the tall crown on tile 72
    python shapes_to_vox.py 02 --used-tiles Chapter_02_UsedTiles.json \
        --tree 71 --tree 72:44

    # a couple of tiles into a scratch folder, for eyeballing
    python shapes_to_vox.py 01 --tiles 64,65,49,43 -o /tmp/check
"""

import argparse
import json
import os

from PIL import Image

import voxlib

# The six tree/bush tiles that were modelled by hand in Shapes_01. Every one of
# them has its ground replaced by the plain grass tile and a crown standing on
# top, so they double as the crown library for every later chapter. Roughly in
# order of height (their crowns top out at z 29, 30, 32, 35, 37 and 38).
REFERENCE_TREE_TILES = (46, 40, 43, 44, 47, 42)
DEFAULT_TREE_TILE = 43
REF_CHAPTER = '01'
# The plain grass tile a tree tile's ground reverts to. Panel-specific: 52 is
# chapter 01's, later chapters pass --grass-tile.
GRASS_TILE_ID = 52


# --------------------------------------------------------------------------
# tree crowns
# --------------------------------------------------------------------------

def load_tree_template(root, tile_id, ref_chapter=REF_CHAPTER):
    """Read the crown (everything above the ground layer) out of a reference VOX.

    Positions are kept exactly as authored -- each reference tree sits where its
    artist put it inside the 24x24 tile -- so stamping with no offset reproduces
    the reference tile byte for byte.
    """
    p = os.path.join(voxlib.shapes_vox_dir(root, ref_chapter),
                     voxlib.shape_vox_name(ref_chapter, tile_id))
    if not os.path.isfile(p):
        raise SystemExit('tree reference VOX not found: %s' % p)
    model = voxlib.read_vox(p)
    crown = [v for v in model.voxels if v[2] > voxlib.GROUND_Z]
    if not crown:
        raise SystemExit('%s has nothing above the ground layer -- not a tree tile' % p)
    return crown


def crown_centre(template):
    xs = [v[0] for v in template]
    ys = [v[1] for v in template]
    return (min(xs) + max(xs)) // 2, (min(ys) + max(ys)) // 2


def stamp_tree(voxels, template, at=None):
    """Drop a crown onto the tile, clipped to the 40^3 canvas.

    ``at`` re-centres the crown on that (x, y); leave it None to keep the
    reference tree's own position.
    """
    dx = dy = 0
    if at is not None:
        cx, cy = crown_centre(template)
        dx, dy = at[0] - cx, at[1] - cy
    n = 0
    for x, y, z, colour in template:
        x, y = x + dx, y + dy
        if 0 <= x < voxlib.CANVAS and 0 <= y < voxlib.CANVAS and 0 <= z < voxlib.CANVAS:
            voxels.append((x, y, z, colour))
            n += 1
    return n


# --------------------------------------------------------------------------
# tile -> voxels
# --------------------------------------------------------------------------

def tile_to_voxels(image, grass_lift=voxlib.GRASS_LIFT, lift_grass=True):
    """Ground layer (plus grass blades) for one 24x24 tile PNG."""
    px = image.load()
    voxels = []
    for py in range(voxlib.TILE):
        for pxi in range(voxlib.TILE):
            colour = voxlib.palette_index(px[pxi, py])
            rgb = voxlib.PALETTE[colour - 1][:3]
            z = voxlib.TERRAIN_Z[voxlib.classify_terrain(rgb, resolve=False)]
            x, y = pxi, voxlib.TILE - 1 - py
            voxels.append((x, y, z, colour))
            if lift_grass and rgb == voxlib.GRASS_RGB:
                for k in range(1, grass_lift + 1):
                    voxels.append((x, y, z + k, colour))
    return voxels


def build_tile(root, nn, tile_id, tree=None, tree_at=None, grass_lift=voxlib.GRASS_LIFT,
               templates=None, panel_dir=None, grass_tile=GRASS_TILE_ID):
    """Voxels for one tile. ``tree`` is a reference tile id from Shapes_01."""
    panel = panel_dir or voxlib.shape_panel_dir(root, nn)
    source_id = grass_tile if tree else tile_id
    path = os.path.join(panel, voxlib.tile_png_name(nn, source_id))
    if not os.path.isfile(path):
        raise SystemExit('tile PNG not found: %s' % path)
    image = Image.open(path).convert('RGB')
    if image.size != (voxlib.TILE, voxlib.TILE):
        raise SystemExit('%s is %dx%d, expected %dx%d'
                         % (path, image.size[0], image.size[1], voxlib.TILE, voxlib.TILE))

    # A tree tile's ground is the flat grass tile, so no blades are raised on it
    # (that is what the reference tiles 43 / 44 do).
    voxels = tile_to_voxels(image, grass_lift=grass_lift, lift_grass=not tree)
    stamped = 0
    if tree:
        stamped = stamp_tree(voxels, templates[tree], tree_at)
    return voxels, source_id, stamped


# --------------------------------------------------------------------------
# CLI
# --------------------------------------------------------------------------

def parse_tree_arg(value):
    """'71' | '71:44' | '71:44@12,11' -> (tile_id, reference_tile, (x, y) | None).

    The reference tile is one of the chapter 01 tree tiles whose crown is
    copied; @X,Y re-centres that crown inside the 24x24 tile.
    """
    at = None
    if '@' in value:
        value, coords = value.split('@', 1)
        cx, cy = coords.split(',')
        at = (int(cx), int(cy))
    template = DEFAULT_TREE_TILE
    if ':' in value:
        value, template = value.split(':', 1)
        template = int(template)
    if template not in REFERENCE_TREE_TILES:
        raise SystemExit('unknown tree reference tile %r, expected one of %s'
                         % (template, ', '.join(str(t) for t in REFERENCE_TREE_TILES)))
    return int(value), template, at


def resolve_tiles(args, root, nn):
    if args.tiles:
        return [int(t) for t in args.tiles.replace(' ', '').split(',') if t]
    if args.used_tiles:
        with open(args.used_tiles, 'r', encoding='utf-8-sig') as f:
            return sorted(int(t) for t in json.load(f)['tile_ids'])
    path = args.chapter_json or os.path.join(
        os.path.dirname(voxlib.chapter_json_path(root, nn)), 'Chapter_%s_Cleaned.json' % nn)
    if not os.path.isfile(path):
        path = voxlib.chapter_json_path(root, nn)
    with open(path, 'r', encoding='utf-8-sig') as f:
        chapter = json.load(f)
    ids = set()
    for col in chapter['ShapeMatrix']:
        ids.update(col)
    print('tile ids taken from %s' % path)
    return sorted(ids)


def main():
    p = argparse.ArgumentParser(description=__doc__,
                                formatter_class=argparse.RawDescriptionHelpFormatter)
    p.add_argument('chapter')
    p.add_argument('--root')
    p.add_argument('--tiles', help='explicit comma-separated tile ids')
    p.add_argument('--used-tiles', help='Chapter_NN_UsedTiles.json from map_clean.py')
    p.add_argument('--chapter-json', help='read the tile ids from this chapter JSON')
    p.add_argument('--tree', action='append', default=[], metavar='ID[:REF][@X,Y]',
                   help='tile id that carries a tree, optionally naming which chapter 01 '
                        'tree tile to copy the crown from (%s, default %d) and where to '
                        'centre it; repeatable'
                        % ('/'.join(str(t) for t in REFERENCE_TREE_TILES), DEFAULT_TREE_TILE))
    p.add_argument('--grass-lift', type=int, default=voxlib.GRASS_LIFT,
                   help='voxels of grass stacked above the ground (default %d)' % voxlib.GRASS_LIFT)
    p.add_argument('--grass-tile', type=int, default=GRASS_TILE_ID,
                   help='plain grass tile id a tree tile\'s ground reverts to '
                        '(default %d, which is chapter 01\'s)' % GRASS_TILE_ID)
    p.add_argument('--panel-dir', help='override ShapePanel<NN>')
    p.add_argument('-o', '--out', help='output dir (default Resources/Remastered/Shapes/Shapes_NN/vox)')
    p.add_argument('--force', action='store_true', help='overwrite existing VOX files')
    p.add_argument('--dry-run', action='store_true')
    args = p.parse_args()

    root = args.root or voxlib.workspace_root()
    nn = voxlib.nn(args.chapter)
    tiles = resolve_tiles(args, root, nn)
    out_dir = args.out or voxlib.shapes_vox_dir(root, nn)

    trees = {}
    for spec in args.tree:
        tid, template, at = parse_tree_arg(spec)
        trees[tid] = (template, at)
    templates = {ref: load_tree_template(root, ref) for ref in REFERENCE_TREE_TILES} if trees else {}

    print('chapter %s   %d tiles -> %s' % (nn, len(tiles), out_dir))
    if trees:
        print('trees: %s' % ', '.join('%d<-ref%d' % (t, v[0]) for t, v in sorted(trees.items())))

    if not args.dry_run and not os.path.isdir(out_dir):
        os.makedirs(out_dir)

    written = skipped = 0
    for tid in tiles:
        tree, at = trees.get(tid, (None, None))
        voxels, source_id, stamped = build_tile(
            root, nn, tid, tree=tree, tree_at=at, grass_lift=args.grass_lift,
            templates=templates, panel_dir=args.panel_dir, grass_tile=args.grass_tile)
        name = voxlib.shape_vox_name(nn, tid)
        dest = os.path.join(out_dir, name)
        note = ''
        if tree:
            note = '  tree from ref %d (%d crown voxels, ground from %s)' % (
                tree, stamped, voxlib.tile_png_name(nn, source_id))
        if os.path.isfile(dest) and not args.force and not args.dry_run:
            print('  skip  %-16s exists (use --force)' % name)
            skipped += 1
            continue
        if not args.dry_run:
            voxlib.write_vox(dest, (voxlib.CANVAS,) * 3, voxels)
        written += 1
        print('  %-5s %-16s %5d voxels%s'
              % ('would' if args.dry_run else 'write', name, len(voxels), note))

    print('%d written, %d skipped' % (written, skipped))


if __name__ == '__main__':
    main()
