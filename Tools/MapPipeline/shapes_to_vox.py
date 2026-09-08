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
    tree tiles (46, 40, 43, 44, 47, 42, shortest to tallest), optionally
    stretched vertically with --tree-stretch when a chapter wants a taller
    forest

Which tiles have trees is a judgement call made from the art, so it is passed
in rather than detected.

A forest drawn from one tile id is one tree repeated, which is exactly what
the 2D art does and what a 3D board must not. ``--variant STRETCH,WIDTH`` (with
``--variant-tiles``) writes alternative crowns for a tile as
``Shape_<NN>_<id>_v<k>.vox`` beside the base model; ShapesLayer picks one per
map position (chapter 09's red/blue pines).

Examples
--------
    # everything the cleaned chapter still uses
    python shapes_to_vox.py 02 --used-tiles Chapter_02_UsedTiles.json

    # ...with a default-height tree on tile 71 and the tall crown on tile 72
    python shapes_to_vox.py 02 --used-tiles Chapter_02_UsedTiles.json \
        --tree 71 --tree 72:44

    # a taller forest -- every crown stretched to 1.4x (chapter 04)
    python shapes_to_vox.py 04 --used-tiles Chapter_04_UsedTiles.json         --grass-tile 20 --tree-stretch 1.4 --tree 131:42

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

# How far a tinted crown's darkest and lightest foliage sit either side of the
# colour it was tinted with -- see tint_crown().
TINT_DARKEST = 0.62
TINT_LIGHTEST = 1.30


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


def stretch_crown(template, factor):
    """Make a crown taller by repeating its layers, keeping it rooted on the ground.

    Layer ``k`` of the output (counted from the first layer above the ground)
    is a copy of source layer ``floor(k / factor)``, so nothing is left hollow
    and the tree's own silhouette -- the conifer's steps, the round tree's taper
    -- is simply drawn out vertically. ``factor`` 1.0 returns the crown as
    authored.
    """
    if factor == 1.0:
        return template
    if factor <= 0:
        raise SystemExit('--tree-stretch must be positive')
    base = voxlib.GROUND_Z + 1
    layers = {}
    for x, y, z, colour in template:
        layers.setdefault(z - base, []).append((x, y, colour))
    height = max(layers) + 1
    out = []
    for k in range(int(round(height * factor))):
        src = min(int(k / factor), height - 1)
        for x, y, colour in layers.get(src, ()):
            out.append((x, y, base + k, colour))
    return out


def widen_crown(template, factor):
    """Make a crown wider or narrower by ``factor`` about its own centre.

    Every layer is resampled: output voxel (x, y) takes the colour of the source
    voxel nearest to ``centre + (v - centre) / factor``, so a factor above 1
    spreads the crown out and one below 1 pulls it in, with no holes either
    way. The trunk scales with it. The result is clipped to the 24x24 tile so a
    wide crown can never push the exported model off its tile centre (the OBJ
    exporter centres on the bounding box).
    """
    if factor == 1.0:
        return template
    if factor <= 0:
        raise SystemExit('--variant width must be positive')
    cx, cy = crown_centre(template)
    layers = {}
    for x, y, z, colour in template:
        layers.setdefault(z, {})[(x, y)] = colour
    out = []
    for z, layer in layers.items():
        xs = [x for x, _y in layer]
        ys = [y for _x, y in layer]
        x0 = max(0, int(cx + (min(xs) - cx) * factor) - 1)
        x1 = min(voxlib.TILE - 1, int(cx + (max(xs) - cx) * factor) + 1)
        y0 = max(0, int(cy + (min(ys) - cy) * factor) - 1)
        y1 = min(voxlib.TILE - 1, int(cy + (max(ys) - cy) * factor) + 1)
        for x in range(x0, x1 + 1):
            sx = int(round(cx + (x - cx) / factor))
            for y in range(y0, y1 + 1):
                sy = int(round(cy + (y - cy) / factor))
                colour = layer.get((sx, sy))
                if colour is not None:
                    out.append((x, y, z, colour))
    return out


def tint_crown(template, rgb):
    """Recolour a crown's foliage to ``rgb``, keeping its shading.

    The six reference crowns are all the green ones chapter 01 happened to have,
    but chapter 05's forest is deliberately three colours -- green, blue-green
    and autumn red -- and stamping green over all of it throws that away.

    Each distinct foliage colour is replaced by ``rgb`` scaled by where that
    colour sits in the crown's own light-to-dark range, so the silhouette's
    shading survives and only the hue changes. Scaling each channel by its own
    ratio instead -- the obvious thing -- blows the quiet channels out: a crown
    green with 24 of blue in its shadows and 40 in its highlights, retinted
    blue, ends up with the highlights at pure 255.

    Trunk voxels are left alone: anything that is not green-dominant is bark,
    and an autumn tree still has a brown trunk.
    """
    def foliage(colour):
        r, g, b = voxlib.PALETTE[colour - 1][:3]
        return g > r and g >= b

    def luma(colour):
        r, g, b = voxlib.PALETTE[colour - 1][:3]
        return 0.30 * r + 0.59 * g + 0.11 * b

    greens = set(c for _x, _y, _z, c in template if foliage(c))
    if not greens:
        return template
    lo = min(luma(c) for c in greens)
    hi = max(luma(c) for c in greens)
    span = max(1.0, hi - lo)

    remap = {}
    for c in greens:
        f = TINT_DARKEST + (TINT_LIGHTEST - TINT_DARKEST) * (luma(c) - lo) / span
        remap[c] = voxlib.palette_index(
            tuple(min(255, int(round(ch * f))) for ch in rgb))
    return [(x, y, z, remap.get(c, c)) for x, y, z, c in template]


def crown_top_z(template):
    return max(v[2] for v in template)


def stamp_tree(voxels, template, at=None, canvas_z=voxlib.CANVAS):
    """Drop a crown onto the tile, clipped to the canvas.

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
        if 0 <= x < voxlib.CANVAS and 0 <= y < voxlib.CANVAS and 0 <= z < canvas_z:
            voxels.append((x, y, z, colour))
            n += 1
    return n


# --------------------------------------------------------------------------
# tile -> voxels
# --------------------------------------------------------------------------

def tile_to_voxels(image, grass_lift=voxlib.GRASS_LIFT, lift_grass=True, flat=False):
    """Ground layer (plus grass blades) for one 24x24 tile PNG.

    ``flat`` puts every pixel at ground level regardless of colour. The terrain
    classes are colour tables read off chapter 01's shoreline, and a chapter
    with no shore can paint those colours for something else entirely --
    chapter 10's cave has no sand at all, but the orange of its fire pools is
    the sand family's (255,153,0) once resolved to the palette, and would
    otherwise sink one voxel into the floor.
    """
    px = image.load()
    voxels = []
    for py in range(voxlib.TILE):
        for pxi in range(voxlib.TILE):
            colour = voxlib.palette_index(px[pxi, py])
            rgb = voxlib.PALETTE[colour - 1][:3]
            z = voxlib.GROUND_Z if flat else voxlib.TERRAIN_Z[voxlib.classify_terrain(rgb, resolve=False)]
            x, y = pxi, voxlib.TILE - 1 - py
            voxels.append((x, y, z, colour))
            if lift_grass and rgb == voxlib.GRASS_RGB:
                for k in range(1, grass_lift + 1):
                    voxels.append((x, y, z + k, colour))
    return voxels


def build_tile(root, nn, tile_id, tree=None, tree_at=None, grass_lift=voxlib.GRASS_LIFT,
               templates=None, panel_dir=None, grass_tile=GRASS_TILE_ID,
               canvas_z=voxlib.CANVAS, flat=False):
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
    voxels = tile_to_voxels(image, grass_lift=grass_lift, lift_grass=not tree, flat=flat)
    stamped = 0
    if tree:
        stamped = stamp_tree(voxels, templates[tree], tree_at, canvas_z=canvas_z)
    return voxels, source_id, stamped


# --------------------------------------------------------------------------
# CLI
# --------------------------------------------------------------------------

def parse_tree_arg(value):
    """'71' | '71:44' | '71:44@12,11' | '71:44#3c5c9c' -> (id, ref, at, tint).

    The reference tile is one of the chapter 01 tree tiles whose crown is
    copied; @X,Y re-centres that crown inside the 24x24 tile, and #RRGGBB
    recolours its foliage to that colour (see :func:`tint_crown`).
    """
    tint = None
    if '#' in value:
        value, hexrgb = value.split('#', 1)
        hexrgb = hexrgb.strip()
        if len(hexrgb) != 6:
            raise SystemExit('tree tint must be #RRGGBB, got %r' % hexrgb)
        tint = tuple(int(hexrgb[i:i + 2], 16) for i in (0, 2, 4))
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
    return int(value), template, at, tint


def parse_variant_arg(value):
    """'1.6,0.85' -> (stretch, width); '1.6' alone keeps the width at 1."""
    parts = value.replace(' ', '').split(',')
    if len(parts) not in (1, 2):
        raise SystemExit('--variant must be STRETCH[,WIDTH], got %r' % value)
    stretch = float(parts[0])
    width = float(parts[1]) if len(parts) == 2 else 1.0
    if stretch <= 0 or width <= 0:
        raise SystemExit('--variant factors must be positive, got %r' % value)
    return stretch, width


def variant_vox_name(nn, tile_id, k):
    """Shape_<NN>_<id>_v<k>.vox -- one alternative crown for a tree tile.

    ShapesLayer looks these up next to the tile's own model and picks one per
    map position, so a forest of one tile id is not a forest of identical
    trees. The base model (no suffix) is always one of the choices.
    """
    return 'Shape_%d_%d_v%d.vox' % (int(nn), tile_id, k)


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
    p.add_argument('--tree', action='append', default=[],
                   metavar='ID[:REF][@X,Y][#RRGGBB]',
                   help='tile id that carries a tree, optionally naming which chapter 01 '
                        'tree tile to copy the crown from (%s, default %d), where to '
                        'centre it, and what colour to tint its foliage; repeatable'
                        % ('/'.join(str(t) for t in REFERENCE_TREE_TILES), DEFAULT_TREE_TILE))
    p.add_argument('--tree-stretch', type=float, default=1.0, metavar='F',
                   help='make every stamped crown F times taller by repeating its '
                        'layers (default 1.0 = the chapter 01 references as authored). '
                        'The VOX canvas grows in Z to fit.')
    p.add_argument('--variant', action='append', default=[], metavar='STRETCH[,WIDTH]',
                   help='also write an alternative model for every --variant-tiles tile, '
                        'with its crown stretched to STRETCH times its authored height and '
                        'WIDTH times its authored width (Shape_NN_<id>_v<k>.vox, k counting '
                        'from 1 in the order given). The game picks one of the base model '
                        'and its variants per map position. Repeatable.')
    p.add_argument('--variant-tiles', default='',
                   help='comma-separated tile ids that get the --variant models; they must '
                        'also be --tree tiles')
    p.add_argument('--flat', action='store_true',
                   help='every pixel at ground level: no sand/water steps. For a chapter '
                        'with no shoreline whose art reuses the shore colours (chapter 10 '
                        'paints fire in the sand oranges)')
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

    # A crown is identified by (reference tile, tint), so one reference stamped in
    # three colours is three templates and each tile picks the one it asked for.
    trees = {}
    wanted = {}
    for spec in args.tree:
        tid, ref, at, tint = parse_tree_arg(spec)
        trees[tid] = ((ref, tint), at)
        wanted[(ref, tint)] = (ref, tint)
    variants = [parse_variant_arg(v) for v in args.variant]
    variant_tiles = sorted(int(t) for t in args.variant_tiles.replace(' ', '').split(',') if t)
    if variant_tiles and not variants:
        raise SystemExit('--variant-tiles needs at least one --variant')
    if variants and not variant_tiles:
        raise SystemExit('--variant needs --variant-tiles')
    for tid in variant_tiles:
        if tid not in trees:
            raise SystemExit('--variant-tiles %d is not a --tree tile' % tid)

    # Templates are keyed by (reference tile, tint) and, for the variants, by
    # the variant number on top of that. The base template carries the chapter's
    # --tree-stretch; a variant replaces that stretch with its own.
    templates = {}
    variant_templates = {}
    if trees:
        authored = {ref: load_tree_template(root, ref) for ref in REFERENCE_TREE_TILES}

        def make(ref, tint, stretch, width):
            tpl = authored[ref]
            if stretch != 1.0:
                tpl = stretch_crown(tpl, stretch)
            if width != 1.0:
                tpl = widen_crown(tpl, width)
            return tint_crown(tpl, tint) if tint else tpl

        for key, (ref, tint) in wanted.items():
            templates[key] = make(ref, tint, args.tree_stretch, 1.0)
        for tid in variant_tiles:
            key = trees[tid][0]
            for k, (stretch, width) in enumerate(variants, 1):
                if (key, k) not in variant_templates:
                    variant_templates[(key, k)] = make(key[0], key[1], stretch, width)

    # Tall trees need headroom: the canvas grows in Z so a stretched crown is not
    # clipped. Every tile in the chapter gets the same size, and the exported OBJ
    # is unaffected (it is centred on X/Y and grounded on the lowest voxel).
    canvas_z = voxlib.CANVAS
    if trees:
        needed = max(crown_top_z(templates[v[0]]) for v in trees.values()) + 1
        for tpl in variant_templates.values():
            needed = max(needed, crown_top_z(tpl) + 1)
        canvas_z = max(canvas_z, needed)

    print('chapter %s   %d tiles -> %s' % (nn, len(tiles), out_dir))
    if trees:
        print('trees: %s' % ', '.join(
            '%d<-ref%d%s' % (t, key[0], '' if key[1] is None else '#%02x%02x%02x' % key[1])
            for t, (key, _at) in sorted(trees.items())))
        print('tree stretch: %.2fx   canvas 40x40x%d' % (args.tree_stretch, canvas_z))
    if variants:
        print('variants: %s   on tiles %s' % (
            ', '.join('v%d=%.2fx tall %.2fx wide' % (k, s, w) for k, (s, w) in enumerate(variants, 1)),
            ', '.join(str(t) for t in variant_tiles)))

    if not args.dry_run and not os.path.isdir(out_dir):
        os.makedirs(out_dir)

    written = skipped = 0
    for tid in tiles:
        tree, at = trees.get(tid, (None, None))
        voxels, source_id, stamped = build_tile(
            root, nn, tid, tree=tree, tree_at=at, grass_lift=args.grass_lift,
            templates=templates, panel_dir=args.panel_dir, grass_tile=args.grass_tile,
            canvas_z=canvas_z, flat=args.flat)
        name = voxlib.shape_vox_name(nn, tid)
        dest = os.path.join(out_dir, name)
        note = ''
        if tree:
            note = '  tree from ref %d (%d crown voxels, ground from %s)' % (
                tree[0], stamped, voxlib.tile_png_name(nn, source_id))
        if os.path.isfile(dest) and not args.force and not args.dry_run:
            print('  skip  %-16s exists (use --force)' % name)
            skipped += 1
            continue
        if not args.dry_run:
            voxlib.write_vox(dest, (voxlib.CANVAS, voxlib.CANVAS, canvas_z), voxels)
        written += 1
        print('  %-5s %-16s %5d voxels%s'
              % ('would' if args.dry_run else 'write', name, len(voxels), note))

        if tid not in variant_tiles:
            continue
        for k in range(1, len(variants) + 1):
            vname = variant_vox_name(nn, tid, k)
            vdest = os.path.join(out_dir, vname)
            vtemplates = {tree: variant_templates[(tree, k)]}
            vvoxels, _src, vstamped = build_tile(
                root, nn, tid, tree=tree, tree_at=at, grass_lift=args.grass_lift,
                templates=vtemplates, panel_dir=args.panel_dir, grass_tile=args.grass_tile,
                canvas_z=canvas_z)
            if not args.dry_run:
                voxlib.write_vox(vdest, (voxlib.CANVAS, voxlib.CANVAS, canvas_z), vvoxels)
            written += 1
            print('  %-5s %-16s %5d voxels  variant %d (%d crown voxels)'
                  % ('would' if args.dry_run else 'write', vname, len(vvoxels), k, vstamped))

    print('%d written, %d skipped' % (written, skipped))


if __name__ == '__main__':
    main()
