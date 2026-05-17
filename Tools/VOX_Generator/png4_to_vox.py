"""
PNG (4 views) -> VOX generator.

Goal: a smooth, rounded 3D voxel character that looks like a natural
figurine when rotated, not a boxy 4-view extrusion.

Pipeline:

  1. Strip black silhouette-border pixels from each PNG so the model is
     not encased in a black shell. Interior dark pixels (eyes, pupils)
     are kept because they are NOT adjacent to alpha=0.

  2. SHAPE.  For each Z slice we read the silhouette extent in front
     (X range) and side (Y range) views and build a *smooth ellipse*
     cross-section. That gives a rounded body whose width-vs-depth at
     every height comes from the source views — head is small, chest is
     wide, etc. — but each horizontal section is a clean ellipse with
     no boxy 4-way corners.

  3. COLOUR.  Each voxel takes its colour from the SINGLE view whose
     viewing direction is closest to the voxel's outward surface normal
     (winner-takes-all by cosine weight, not a blend). This keeps
     features sharp instead of averaging them into mush.

     Surface normal at voxel (x, y, z) on the per-Z ellipse:
        n = ( (x+0.5-cx)/a^2 , (y+0.5-cy)/b^2 , 0 ),   normalised in XY.
     View directions (outward, where the camera looks back from):
        Front     :  (0, -1, 0)
        Back      :  (0, +1, 0)
        Side_lo   : (-1,  0, 0)    # camera at character's right
        Side_hi   : (+1,  0, 0)    # camera at character's left
     The view with max(n . v) owns this voxel.

  4. FEATURE consolidation.  Without help, the side view's "eye" pixel
     would paint a duplicate eye onto the side surface. We avoid that:

       a. In each view's Z-row we identify the local "body colour" (the
          most-common opaque colour) and the "feature colours" (rare
          colours, far in RGB from body).
       b. For each cross-view feature, the 3D position is the
          intersection of where the feature appears in front and where
          it appears in side. That voxel gets stamped with the feature
          colour and becomes the unique 3D location of the feature.
       c. On every OTHER voxel that would have taken a feature colour
          from its dominant view, we substitute that view's body colour
          instead — so the feature doesn't smear across the surface.

     Result: the eye is ONE 3D voxel near the front-side corner of the
     head, visible from both front and any front-3/4 angle, and the
     pure side view shows a clean hood instead of a duplicated eye.

Coordinate system (MagicaVoxel convention):
  +X -> viewer's right in front view
  +Y -> deeper into the scene  (y=0 = character's front, y=23 = back)
  +Z -> up                      (z=0 = feet, z=23 = head)
"""

import math
import struct
import os
from collections import deque, Counter
from PIL import Image


DIM = 24
ALPHA_T = 64
OUTLINE_SUM = 30
CROSS_VIEW_MATCH_SQ = 4000      # how strict cross-view feature colour matching is
FEATURE_BODY_DIST_SQ = 200      # a colour is a feature only if it's this far from the row's body colour
FEATURE_MAX_OCCURRENCES = 3     # appearing more often than this means it's not a feature

# When a column X sits at the fringe of the ellipse (i.e. |ex| >= FRINGE_THRESH),
# we cap its Y extent to FRINGE_HALF_DEPTH voxels half-width. This stops a single
# weapon pixel in the front view from being smeared through the full body depth
# (which was making one stick appear as 3 separate "ghost" sticks from the side).
FRINGE_THRESH = 0.80
FRINGE_HALF_DEPTH = 1.5         # max |dy| at fringe -> 3 voxels deep

# After all voxels are placed, find same-colour clusters that are close but not
# touching and connect them with bridge voxels of the same colour. Fixes the
# common case of a stick / bow / staff that got broken into 2-3 fragments.
BRIDGE_MAX_DIST = 4             # Manhattan distance threshold; 0 disables

# Asymmetric depth compression along the Y axis (vox Y = character's front-back).
# After all voxels are placed and the front face is locked in, each voxel's Y
# is scaled around the model's Y midpoint:
#     y < y_mid   ->  y_mid + (y - y_mid) * DEPTH_COMPRESS_FRONT
#     y >= y_mid  ->  y_mid + (y - y_mid) * DEPTH_COMPRESS_BACK
# Voxels that collide on the same destination Y are merged "outer wins": the
# voxel further from y_mid takes priority, so front and back surface colours
# survive. Set both to 1.0 to disable.
DEPTH_COMPRESS_FRONT = 0.5
DEPTH_COMPRESS_BACK  = 1.0       # 1.0 == do not touch the back half


# --------------------------------------------------------------------------- #
# small helpers                                                               #
# --------------------------------------------------------------------------- #

def load_png(path):
    im = Image.open(path).convert('RGBA')
    if im.size != (DIM, DIM):
        im = im.resize((DIM, DIM), Image.NEAREST)
    return im


def color_dist_sq(a, b):
    return (a[0] - b[0]) ** 2 + (a[1] - b[1]) ** 2 + (a[2] - b[2]) ** 2


def strip_outline(im):
    """Replace silhouette-border dark pixels with nearest inner colour (alpha kept)."""
    w, h = im.size
    px = im.load()

    is_outline = [[False] * h for _ in range(w)]
    for x in range(w):
        for y in range(h):
            p = px[x, y]
            if p[3] < ALPHA_T:
                continue
            if p[0] + p[1] + p[2] >= OUTLINE_SUM:
                continue
            for dx, dy in ((-1, 0), (1, 0), (0, -1), (0, 1)):
                nx, ny = x + dx, y + dy
                if not (0 <= nx < w and 0 <= ny < h) or px[nx, ny][3] < ALPHA_T:
                    is_outline[x][y] = True
                    break

    cleaned = im.copy()
    cpx = cleaned.load()
    for x in range(w):
        for y in range(h):
            if not is_outline[x][y]:
                continue
            q = deque([(x, y)])
            seen = {(x, y)}
            found = None
            while q and found is None:
                cx, cy = q.popleft()
                for dx, dy in ((-1, 0), (1, 0), (0, -1), (0, 1)):
                    nx, ny = cx + dx, cy + dy
                    if (nx, ny) in seen:
                        continue
                    seen.add((nx, ny))
                    if not (0 <= nx < w and 0 <= ny < h):
                        continue
                    if px[nx, ny][3] >= ALPHA_T and not is_outline[nx][ny]:
                        found = px[nx, ny]
                        break
                    q.append((nx, ny))
            if found is not None:
                cpx[x, y] = (found[0], found[1], found[2], px[x, y][3])
    return cleaned


# --------------------------------------------------------------------------- #
# per-row colour analysis                                                     #
# --------------------------------------------------------------------------- #

def _connected_components(voxel_set):
    """6-neighbour 3D connected components over a flat iterable of (x,y,z)."""
    unvisited = set(voxel_set)
    components = []
    NEIGH = ((-1, 0, 0), (1, 0, 0), (0, -1, 0),
            (0, 1, 0), (0, 0, -1), (0, 0, 1))
    while unvisited:
        seed = next(iter(unvisited))
        comp = []
        stack = [seed]
        while stack:
            v = stack.pop()
            if v not in unvisited:
                continue
            unvisited.remove(v)
            comp.append(v)
            x, y, z = v
            for dx, dy, dz in NEIGH:
                n = (x + dx, y + dy, z + dz)
                if n in unvisited:
                    stack.append(n)
        components.append(comp)
    return components


def _compress_depth(voxel_color, front_factor, back_factor):
    """Squash voxels along Y axis (vox = character's front-back) asymmetrically.

    Steps:
      1. Find y_mid = midpoint of the model's Y bounding box.
      2. For every voxel, scale its Y offset from y_mid:
            front half (y < y_mid):  new_dy = dy * front_factor
            back  half (y >= y_mid): new_dy = dy * back_factor
      3. When several source Y's round to the same destination Y, the OUTER
         voxel wins — process front-half ascending Y and back-half descending
         Y so the front-most / back-most pixel is placed first, and skip if
         the destination already has a voxel. This preserves surface colours
         (front-face PNG override, back-face hood, etc.).
    """
    if not voxel_color:
        return
    ys = [pos[1] for pos in voxel_color]
    y_min, y_max = min(ys), max(ys)
    y_mid = (y_min + y_max) / 2.0

    def remap(y):
        dy = y - y_mid
        scaled = dy * (front_factor if dy < 0 else back_factor)
        return int(round(y_mid + scaled))

    # Outer-first ordering:
    #   front half (y < y_mid): smallest y first  -> tuple key (0, y)
    #   back  half (y >= y_mid): largest y first  -> tuple key (1, -y)
    items = sorted(voxel_color.items(),
                   key=lambda kv: (0, kv[0][1]) if kv[0][1] < y_mid else (1, -kv[0][1]))

    new_voxel_color = {}
    for (x, y, z), c in items:
        ny = remap(y)
        if 0 <= ny < DIM:
            pos = (x, ny, z)
            if pos not in new_voxel_color:
                new_voxel_color[pos] = c
    voxel_color.clear()
    voxel_color.update(new_voxel_color)


def _bridge_same_color(voxel_color, max_dist):
    """Connect same-colour 3D fragments that sit close together.

    For every colour that exists as 2+ disjoint clusters in the model:
    sort clusters by size, then for each smaller cluster find the
    closest larger cluster within `max_dist` Manhattan voxels and draw
    a straight line of bridge voxels of the same colour between their
    nearest endpoints. Pure fill — does not overwrite voxels of a
    different colour, only fills empty cells.
    """
    by_color = {}
    for pos, c in voxel_color.items():
        by_color.setdefault(c, set()).add(pos)

    bridges_added = 0
    for color, voxels in by_color.items():
        if len(voxels) < 2:
            continue
        comps = _connected_components(voxels)
        if len(comps) < 2:
            continue
        comps.sort(key=len, reverse=True)
        # Bridge every smaller cluster to whichever already-kept cluster
        # is closest (greedy union-find-ish, good enough for small N).
        kept = [comps[0]]
        for small in comps[1:]:
            best_pair = None
            best_d = 10 ** 9
            for big in kept:
                # Use cheaper bbox-distance prefilter, then exact pair scan.
                bx = [v[0] for v in big]; by = [v[1] for v in big]; bz = [v[2] for v in big]
                sx = [v[0] for v in small]; sy = [v[1] for v in small]; sz = [v[2] for v in small]
                bbox_d = (max(0, max(sx) - max(bx), min(bx) - min(sx))
                          + max(0, max(sy) - max(by), min(by) - min(sy))
                          + max(0, max(sz) - max(bz), min(bz) - min(sz)))
                if bbox_d > max_dist:
                    continue
                for va in small:
                    for vb in big:
                        d = abs(va[0] - vb[0]) + abs(va[1] - vb[1]) + abs(va[2] - vb[2])
                        if d < best_d:
                            best_d = d
                            best_pair = (va, vb)
            if best_pair is None or best_d > max_dist:
                # Even unreachable, keep the cluster so it doesn't get re-bridged.
                kept.append(small)
                continue
            va, vb = best_pair
            steps = max(abs(va[0] - vb[0]),
                        abs(va[1] - vb[1]),
                        abs(va[2] - vb[2]))
            for t in range(1, steps):
                f = t / steps
                bx = int(round(va[0] + f * (vb[0] - va[0])))
                by = int(round(va[1] + f * (vb[1] - va[1])))
                bz = int(round(va[2] + f * (vb[2] - va[2])))
                if (bx, by, bz) not in voxel_color:
                    voxel_color[(bx, by, bz)] = color
                    bridges_added += 1
            kept.append(small)
    return bridges_added


def analyze_row(px, v):
    """For one horizontal row at pixel-y = v, classify the colours.

    Returns:
        colors_at  : {u -> (r,g,b)}      every opaque pixel in the row
        body_color : (r,g,b) or None     the dominant body colour at this row
        features   : {color -> [u, ...]} colours that are rare AND distinct
                                         from the body colour
    """
    cs = {}
    for u in range(DIM):
        p = px[u, v]
        if p[3] >= ALPHA_T:
            cs[u] = (p[0], p[1], p[2])
    if not cs:
        return cs, None, {}
    cnt = Counter(cs.values())
    body = cnt.most_common(1)[0][0]
    features = {}
    for c, n in cnt.items():
        if n <= FEATURE_MAX_OCCURRENCES and color_dist_sq(c, body) >= FEATURE_BODY_DIST_SQ:
            features[c] = [u for u, cc in cs.items() if cc == c]
    return cs, body, features


# --------------------------------------------------------------------------- #
# build_vox                                                                   #
# --------------------------------------------------------------------------- #

def build_vox(front_path, back_path, side_low_path, side_high_path, out_path):
    front  = strip_outline(load_png(front_path))
    back   = strip_outline(load_png(back_path))
    side_lo = strip_outline(load_png(side_low_path))
    side_hi = strip_outline(load_png(side_high_path))

    fpx = front.load();   bpx = back.load()
    lpx = side_lo.load(); hpx = side_hi.load()

    # palette --------------------------------------------------------------- #
    palette = []
    color_to_idx = {}

    def palette_index(rgb):
        key = (int(rgb[0]), int(rgb[1]), int(rgb[2]), 255)
        if key in color_to_idx:
            return color_to_idx[key]
        if len(palette) < 255:
            palette.append(key)
            color_to_idx[key] = len(palette)
            return len(palette)
        best_i, best_d = 1, 10 ** 12
        for i, p in enumerate(palette):
            d = (p[0] - key[0]) ** 2 + (p[1] - key[1]) ** 2 + (p[2] - key[2]) ** 2
            if d < best_d:
                best_d, best_i = d, i + 1
        return best_i

    voxel_color = {}      # (x,y,z) -> (r,g,b)
    feature_stamp = {}    # (x,y,z) -> (r,g,b)  cross-view-confirmed feature voxels

    # per-Z pass ------------------------------------------------------------ #
    for z in range(DIM):
        v_pix = (DIM - 1) - z

        f_cs, f_body, f_feat = analyze_row(fpx, v_pix)
        b_cs_raw, b_body, b_feat_raw = analyze_row(bpx, v_pix)
        l_cs, l_body, l_feat = analyze_row(lpx, v_pix)
        h_cs_raw, h_body, h_feat_raw = analyze_row(hpx, v_pix)

        # remap back-view and high-side-view rows from pixel-u to world coord
        # back: pixel u -> world X = DIM-1-u
        b_cs    = {(DIM - 1) - u: c for u, c in b_cs_raw.items()}
        b_feat  = {c: [(DIM - 1) - u for u in us] for c, us in b_feat_raw.items()}
        # side_hi: pixel u -> world Y = DIM-1-u
        h_cs    = {(DIM - 1) - u: c for u, c in h_cs_raw.items()}
        h_feat  = {c: [(DIM - 1) - u for u in us] for c, us in h_feat_raw.items()}

        # ellipse from silhouette extents ---------------------------------- #
        xs = list(f_cs.keys()) + list(b_cs.keys())
        ys = list(l_cs.keys()) + list(h_cs.keys())
        if not xs or not ys:
            continue
        x_min, x_max = min(xs), max(xs)
        y_min, y_max = min(ys), max(ys)
        cx = (x_min + x_max + 1) / 2.0
        cy = (y_min + y_max + 1) / 2.0
        a  = max((x_max - x_min + 1) / 2.0, 0.6)
        b  = max((y_max - y_min + 1) / 2.0, 0.6)

        # ------ cross-view feature matching at this Z ------- #
        # A feature colour shared by front (or back) + a side view defines
        # a 3D position (x_world, y_world, z).
        local_stamps = {}      # (x, y) -> color
        for fc, fxs in f_feat.items():
            for lc, lys in l_feat.items():
                if color_dist_sq(fc, lc) <= CROSS_VIEW_MATCH_SQ:
                    for xw in fxs:
                        for yw in lys:
                            local_stamps[(xw, yw)] = fc
            for hc, hys in h_feat.items():
                if color_dist_sq(fc, hc) <= CROSS_VIEW_MATCH_SQ:
                    for xw in fxs:
                        for yw in hys:
                            local_stamps[(xw, yw)] = fc
        for bc, bxs in b_feat.items():
            for lc, lys in l_feat.items():
                if color_dist_sq(bc, lc) <= CROSS_VIEW_MATCH_SQ:
                    for xw in bxs:
                        for yw in lys:
                            local_stamps[(xw, yw)] = bc
            for hc, hys in h_feat.items():
                if color_dist_sq(bc, hc) <= CROSS_VIEW_MATCH_SQ:
                    for xw in bxs:
                        for yw in hys:
                            local_stamps[(xw, yw)] = bc

        # Suppress features-only-in-one-view from showing up on side surfaces:
        # collect the set of feature colours we will explicitly stamp at a
        # specific 3D point so we know to substitute body colour elsewhere.
        suppress_F = {c for c in f_feat}
        suppress_B = {c for c in b_feat}
        suppress_L = {c for c in l_feat}
        suppress_H = {c for c in h_feat}

        # ------ fill ellipse with hard surface colouring ------- #
        for x in range(DIM):
            for y in range(DIM):
                ex = (x + 0.5 - cx) / a
                ey = (y + 0.5 - cy) / b
                if ex * ex + ey * ey > 1.0:
                    continue

                # Fringe narrowing: at the X-extreme columns of the slice
                # the ellipse already tapers in Y, but for a weapon column
                # (1-2 voxels wide protruding from the body) that's still
                # too much depth — the front view's stick pixel ends up
                # painted on a 5-deep slab. Cap |dy| at fringe X positions
                # so the stick collapses to a thin 3-voxel-deep ribbon
                # centred on the body's mid-depth.
                if abs(ex) >= FRINGE_THRESH:
                    half_dy = abs(y + 0.5 - cy)
                    if half_dy > FRINGE_HALF_DEPTH:
                        continue

                # outward normal in XY plane of the ellipse
                # n = (ex/a, ey/b) before normalisation; the exact length
                # doesn't matter for the dot-product comparisons below.
                nx = ex / a
                ny = ey / b

                # dot products with each view direction
                # Front view: camera at -Y, sees voxels whose outward normal has -Y component
                # so the view-direction vector pointing FROM camera into scene is +Y;
                # an outward normal aligned to -Y means we look back at it. We use
                # the outward normal aligned with the view "from camera" — i.e. higher
                # is the more this voxel faces that view.
                d_F = -ny    # front view sees front-facing voxels (ny < 0)
                d_B =  ny
                d_L = -nx    # side_lo (camera at -X) sees voxels with outward -X
                d_R =  nx

                # Skip the centre singularity
                if max(d_F, d_B, d_L, d_R) <= 0:
                    # interior voxel: pick any opaque view's colour at this slice
                    for c in (f_body, l_body, h_body, b_body):
                        if c is not None:
                            voxel_color[(x, y, z)] = c
                            break
                    continue

                # winner-takes-all (with simple tie-break)
                winners = [('F', d_F, f_cs.get(x),               f_body, suppress_F),
                           ('B', d_B, b_cs.get(x),               b_body, suppress_B),
                           ('L', d_L, l_cs.get(y),               l_body, suppress_L),
                           ('R', d_R, h_cs.get(y),               h_body, suppress_H)]
                # discard views whose pixel is transparent
                winners = [w for w in winners if w[2] is not None]
                if not winners:
                    continue
                winners.sort(key=lambda w: -w[1])
                _, _, c_top, body_top, suppress_top = winners[0]

                # feature suppression: if the chosen colour is a "feature"
                # colour from this view, swap to that view's body colour so the
                # feature doesn't smear across the surface.  The actual feature
                # location is stamped separately below.
                if c_top in suppress_top and body_top is not None:
                    c_top = body_top

                voxel_color[(x, y, z)] = c_top

        # stamp cross-view-confirmed features at their unique 3D positions,
        # but only if that voxel is inside the ellipse (otherwise the feature
        # would be floating in empty space).
        for (xw, yw), c in local_stamps.items():
            exs = (xw + 0.5 - cx) / a
            eys = (yw + 0.5 - cy) / b
            if exs * exs + eys * eys <= 1.0:
                feature_stamp[(xw, yw, z)] = c

    # apply feature stamps (they override any prior colouring) -------------- #
    for pos, c in feature_stamp.items():
        voxel_color[pos] = c

    # FRONT-FACE OVERRIDE: the front-most voxel in every column (x, *, z)
    # is painted with the EXACT front-PNG colour at (x, 23-z). This makes
    # the front of the VOX pixel-for-pixel identical to the front PNG —
    # eyes, head-band, mouth all land at the correct front-surface voxels.
    # Side/back surfaces are NOT touched, so the feature-suppression that
    # prevents duplicated eyes on the side surfaces is preserved.
    for x in range(DIM):
        for z in range(DIM):
            v_pix = (DIM - 1) - z
            p = fpx[x, v_pix]
            if p[3] < ALPHA_T:
                continue
            front_color = (p[0], p[1], p[2])
            for y in range(DIM):
                if (x, y, z) in voxel_color:
                    voxel_color[(x, y, z)] = front_color
                    break

    # DEPTH COMPRESSION: squash the model along Y (character's front-back)
    # asymmetrically — front half loses 50%, back half loses ~33% (defaults).
    # Done AFTER the front-face override so the now-front-most voxel still
    # carries its exact PNG colour, and BEFORE bridging so any same-colour
    # bridges are drawn in the compressed space and stay continuous.
    if DEPTH_COMPRESS_FRONT < 1.0 or DEPTH_COMPRESS_BACK < 1.0:
        _compress_depth(voxel_color, DEPTH_COMPRESS_FRONT, DEPTH_COMPRESS_BACK)

    # SAME-COLOUR BRIDGE: weapons (sticks, bows, staffs) often end up
    # fragmented into 2-3 clusters because the per-Z ellipse pinches at
    # certain heights. Find every set of clusters that share a colour
    # and sit within BRIDGE_MAX_DIST Manhattan voxels, then draw a
    # line of same-colour bridge voxels between their closest endpoints.
    if BRIDGE_MAX_DIST > 0:
        _bridge_same_color(voxel_color, BRIDGE_MAX_DIST)

    # emit voxels ----------------------------------------------------------- #
    voxels = []
    for (x, y, z), c in voxel_color.items():
        voxels.append((x, y, z, palette_index(c)))

    # write .vox ----------------------------------------------------------- #
    def chunk(cid, content, children=b''):
        return cid + struct.pack('<ii', len(content), len(children)) + content + children

    size_chunk = chunk(b'SIZE', struct.pack('<iii', DIM, DIM, DIM))

    xyzi_body = bytearray(struct.pack('<i', len(voxels)))
    for (x, y, z, c) in voxels:
        xyzi_body += bytes([x, y, z, c])
    xyzi_chunk = chunk(b'XYZI', bytes(xyzi_body))

    rgba_body = bytearray()
    for i in range(256):
        if i < len(palette):
            r, g, b, _ = palette[i]
        else:
            r, g, b = 0, 0, 0
        rgba_body += bytes([r, g, b, 255])
    rgba_chunk = chunk(b'RGBA', bytes(rgba_body))

    main = chunk(b'MAIN', b'', size_chunk + xyzi_chunk + rgba_chunk)
    out = b'VOX ' + struct.pack('<i', 150) + main
    with open(out_path, 'wb') as f:
        f.write(out)

    return len(voxels), len(palette)


def build_frame(src_dir, out_dir, frame, char_id='006'):
    """Build a single animation frame's VOX from the 4-direction PNG set.

    Frame layout in the source sheet (4 directions x 3 walk frames = 12 images):
      front  : 01 02 03    (rows of the sheet)
      left   : 04 05 06
      back   : 07 08 09
      right  : 10 11 12

    For walk-frame `f` in [1..3]:
      front_id = f           (1 / 2 / 3)
      left_id  = 3 + f       (4 / 5 / 6)
      back_id  = 6 + f       (7 / 8 / 9)
      right_id = 9 + f       (10 / 11 / 12)

    Note on view labels: Row-3 in this sheet is the BACK view (no face)
    and Row-4 is the right side, so we feed Row-3 as back_path and Row-4
    as side_high_path (camera at +X).
    """
    front_id = frame
    left_id  = 3 + frame
    back_id  = 6 + frame
    right_id = 9 + frame

    def path(n):
        return os.path.join(src_dir, f'Icon-{char_id}-{n:02d}.png')

    dst = os.path.join(out_dir, f'Icon_{char_id}_{frame:02d}.vox')
    n_vox, n_col = build_vox(
        front_path=path(front_id),
        back_path=path(back_id),
        side_low_path=path(left_id),
        side_high_path=path(right_id),
        out_path=dst,
    )
    print(f'wrote {dst}: {n_vox} voxels, {n_col} palette colors  '
          f'(used {front_id:02d} front, {left_id:02d} left, '
          f'{back_id:02d} back, {right_id:02d} right)')
    return dst


def detect_char_id(src_dir):
    """Find the character id from the first 'Icon-XXX-NN.png' in src_dir.

    Accepts ids that are pure digits (e.g. '006') or arbitrary tokens
    without dashes; we just take whatever sits between the two dashes
    of an Icon-...-NN.png filename.
    """
    import re
    pat = re.compile(r'^Icon-([^-]+)-(\d{1,3})\.png$', re.IGNORECASE)
    found = {}
    for name in sorted(os.listdir(src_dir)):
        m = pat.match(name)
        if m:
            cid, n = m.group(1), int(m.group(2))
            found.setdefault(cid, []).append(n)
    if not found:
        raise SystemExit(
            f"No 'Icon-XXX-NN.png' files found in {src_dir!r}")
    # if multiple character ids share the folder, the one with the most
    # frames wins (and we tell the user which we picked).
    cid = max(found, key=lambda k: len(found[k]))
    return cid, sorted(found[cid])


if __name__ == '__main__':
    import argparse
    parser = argparse.ArgumentParser(
        description=(
            'Generate VOX files from a 4-direction × 3-frame PNG sprite '
            'set. Default behaviour generates all 3 frames into the '
            'input folder.'),
        formatter_class=argparse.RawDescriptionHelpFormatter,
        epilog=(
            'Input folder must contain 12 PNGs named Icon-<id>-01.png .. '
            'Icon-<id>-12.png\n'
            '(rows: 01-03 front, 04-06 left, 07-09 back, 10-12 right).\n\n'
            'Examples:\n'
            '  python png4_to_vox.py ./icons/006\n'
            '  python png4_to_vox.py ./icons/006 ./vox_out\n'
            '  python png4_to_vox.py ./icons/006 --frame 2\n'
        ),
    )
    parser.add_argument('input',
        help='folder containing the 12 Icon-XXX-NN.png source PNGs')
    parser.add_argument('output', nargs='?', default=None,
        help='output folder for the .vox files (default: same as input)')
    parser.add_argument('--frame', type=int, action='append', metavar='N',
        help='generate only frame N (1-3); pass multiple times for several. '
             'Default: generates 1, 2, 3.')
    parser.add_argument('--char-id', default=None,
        help='override the auto-detected character id (e.g. "006")')
    args = parser.parse_args()

    src_dir = os.path.abspath(args.input)
    if not os.path.isdir(src_dir):
        raise SystemExit(f'input folder does not exist: {src_dir!r}')
    out_dir = os.path.abspath(args.output) if args.output else src_dir
    os.makedirs(out_dir, exist_ok=True)

    if args.char_id:
        char_id = args.char_id
    else:
        char_id, available = detect_char_id(src_dir)
        print(f'detected char_id = {char_id}  (frames available: {available})')

    frames = args.frame or [1, 2, 3]
    for f in frames:
        build_frame(src_dir, out_dir, f, char_id=char_id)
