"""
Fight animation (single-view 2D frames) -> per-frame VOX models.

The icon pipeline (png4_to_vox.py) has FOUR orthogonal views of every walk
frame, so it can carve a body out of the front and side silhouettes. A battle
Fight frame has only ONE view: a 320x210 drawing from the battle camera's
3/4 angle (friends seen from behind, enemies from the front). There is no
side view to carve with, so depth has to be inferred from that one picture.

Pipeline, per creature (all frames share one crop, one palette, one origin,
so the models overlay exactly and can be swapped frame by frame):

  1. SEGMENT every frame into three layers
       shadow : the dithered (48,48,48) checkerboard under the feet.  Dropped;
                the 3D model gets a real shadow from the scene light.
       fx     : sword trails / slash glows -- regions of bright colours that
                the attack frame uses far more than any idle frame does, and
                that have a near-white hot core (so gold armour or a gold
                cloak that opens up during the swing is NOT mistaken for fx).
                Written to a separate, 1-voxel-thin `_fx.vox` sheet so Unity
                can give it an additive / emissive material.
       body   : everything else.

  2. STRIP the black silhouette outline (same rule as the icons): outline
     pixels on the body's border take the nearest inner colour, so the model
     is not encased in a black shell.  Black lines INSIDE the body are kept
     and act as creases in step 3.

  3. INFLATE the body silhouette into a thickness map by solving
         laplace(h) = -2   inside the silhouette,   h = 0 outside
     and taking  half_depth = sqrt(h).  For a strip of width w this is an
     exact half-circle of radius w/2, so a sword (w=1) is 1 voxel thick, an
     arm (w=8) about 8, the torso wider still -- thickness follows the local
     width everywhere with no per-part hand work.  Interior outline pixels
     can add a soft pull toward 0 (CREASE_K) to separate overlapping parts,
     but the line art is dense enough that this flattens the whole figure,
     so it is off by default.  Then SLIM by height: the torso inflates as deep
     as arms + cloak + body are wide together, so thickness beyond a few voxels
     is scaled down across the upper body and further at the waist.

  4. EXTRUDE each pixel into a voxel column around a common mid-plane,
     front half (toward the camera) and back half scaled independently,
     every voxel in the column taking the pixel's colour -- so the battle
     camera sees the original drawing pixel for pixel.

Coordinate system (same as the icons, MagicaVoxel convention):
  +X -> screen right in the original frame
  +Y -> away from the battle camera (y small = front)
  +Z -> up
One source pixel = one voxel.

Output per creature, in <out>/<id>/:
  smoothed/Fight_<id>_A_FF.obj (+ _fx.obj), one shared Fight_<id>.mtl + palette png
  voxels/Fight_<id>_A_FF.npz   the voxels, for fight_preview.py
  Fight_<id>_A_FF.vox          for MagicaVoxel, only when the crop fits 255 voxels
  Fight_<id>.json              crop, mid plane, frame list (read by FightModel3D)
Frames whose source image is identical share one model.

Usage:
  python fight_to_vox.py all --jobs 12             # every creature, in parallel
  python fight_to_vox.py 001 [002 ...]            # all frames of each creature
  python fight_to_vox.py 001 --src <dir> --out <dir>
"""

import argparse
import glob
import hashlib
import json
import os
import re
import struct
import sys
from collections import Counter

import numpy as np
from PIL import Image
from scipy import ndimage
from scipy.sparse import coo_matrix
from scipy.sparse.linalg import spsolve


ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..'))
DEFAULT_SRC = os.path.join(ROOT, 'WindingTale', 'Assets', 'Resources', 'Fights')
DEFAULT_OUT = os.path.join(ROOT, 'Resources', 'Remastered', 'Fights3D')

FRAME_SECONDS = 0.12            # CreateFightAnimations.cs keys every frame 0.12s apart
FRAME_RE = re.compile(r'^Fight-(\d{3})-(\d)-(\d{2})\.png$', re.IGNORECASE)
ANIM_NAMES = {1: 'idle', 2: 'attack', 3: 'spell'}

SHADOW_RGB = (48, 48, 48)
OUTLINE_SUM = 12                # r+g+b at or below this is "black outline"

# fx detection
FX_MIN_BRIGHT = 240             # max(r,g,b) of a glow colour
FX_MIN_LUMA = 170
FX_EXTRA_PIXELS = 30            # frame must use the colour this much more than any idle frame
FX_MIN_COMPONENT = 8            # smaller glow specks stay on the body
FX_CORE_MIN_CHANNEL = 180       # a glow's hot core is near white: min(r,g,b) at least this
FX_MIN_CORE = 6                 # a region needs this many core pixels to be fx (gold armour has none)
FX_ATTACH_DIST = 4              # coreless glow fragments this close to fx join it
FX_RAMP_HUE_DEG = 20            # fx grows into same-hue pixels (its darker ramp) ...
FX_RAMP_MIN_SAT = 0.6           # ... that are saturated (skin, steel, cloth stay put)
FX_RAMP_MIN_VAL = 0.3
FX_RAMP_DIST = 8                # ... and no further than this from the bright part

# depth
DEPTH_SCALE = 1.0               # 1.0 = circular cross-sections; <1 flattens toward a relief
FRONT_FACTOR = 1.0              # toward the camera
BACK_FACTOR = 0.8               # away from the camera (never seen in battle)
MAX_HALF = 24                   # cap on half-thickness, voxels
CREASE_K = 0.0                  # pull toward 0 on interior outline pixels. Off: the line art is so
                                # dense that even 0.1 flattens the figure into a relief; 0.03 at most
# slimming by height. Inflation makes the torso as deep as the whole silhouette is wide
# there -- arms, cloak and body merged into one blob -- so the chest and above all the
# waist come out barrel-shaped. Height t runs 0 at the feet to 1 at the top of the first
# idle frame; only thickness beyond SLIM_KEEP is scaled, so arms and weapons keep theirs.
# The upper/waist values below are the humanoid profile; each creature's actual profile
# comes from fight_body_types.json (flying, mounted, dragons ... are not slimmed like a body).
BODY_TYPES_FILE = os.path.join(os.path.dirname(os.path.abspath(__file__)), 'fight_body_types.json')
SLIM_KEEP = 3.0                 # voxels of half-thickness never slimmed
UPPER_DEPTH = 0.7               # depth kept across the upper body ...
UPPER_FROM, UPPER_TO = 0.35, 0.85   # ... between these heights (head and legs untouched)
UPPER_RAMP = 0.1                # fade in/out width either side of that band
WAIST_DEPTH = 0.6               # extra depth kept at the waist, on top of UPPER_DEPTH
WAIST_AT, WAIST_WIDTH = 0.5, 0.08   # waist height and gaussian sigma
ALL_DEPTH = 1.0                 # depth kept at every height (flying: wings + body inflate as one blob)

# vox_to_obj_exporter.py's default --scale; Unity reads it back from the manifest to
# turn mesh units into voxels
OBJ_SCALE = 0.1

MIN_BODY_COMPONENT = 4
BODY_TYPE = 'humanoid'          # set per creature from fight_body_types.json          # drop dust specks smaller than this


# --------------------------------------------------------------------------- #
# loading / segmentation                                                      #
# --------------------------------------------------------------------------- #

def list_frames(src_dir, cid):
    frames = []
    for path in sorted(glob.glob(os.path.join(src_dir, cid, f'Fight-{cid}-*.png'))):
        m = FRAME_RE.match(os.path.basename(path))
        if m:
            frames.append((int(m.group(2)), int(m.group(3)), path))
    if not frames:
        raise SystemExit(f'no Fight-{cid}-A-FF.png frames under {os.path.join(src_dir, cid)}')
    return frames


def load_rgba(path):
    return np.array(Image.open(path).convert('RGBA'), dtype=np.uint8)


def colour_keys(rgba):
    """uint32 key per pixel (r<<16|g<<8|b), -1 where transparent."""
    rgb = rgba[..., :3].astype(np.int64)
    keys = (rgb[..., 0] << 16) | (rgb[..., 1] << 8) | rgb[..., 2]
    keys[rgba[..., 3] == 0] = -1
    return keys


def shadow_mask(rgba):
    """The dithered ground shadow: (48,48,48) pixels with no 4-neighbour of the same colour."""
    grey = ((rgba[..., 0] == SHADOW_RGB[0]) & (rgba[..., 1] == SHADOW_RGB[1])
            & (rgba[..., 2] == SHADOW_RGB[2]) & (rgba[..., 3] > 0))
    cross = np.array([[0, 1, 0], [1, 0, 1], [0, 1, 0]])
    grey_neighbours = ndimage.convolve(grey.astype(np.int32), cross, mode='constant')
    return grey & (grey_neighbours == 0)


def is_glow_colour(key):
    r, g, b = (key >> 16) & 255, (key >> 8) & 255, key & 255
    return max(r, g, b) >= FX_MIN_BRIGHT and (0.299 * r + 0.587 * g + 0.114 * b) >= FX_MIN_LUMA


def fx_mask(rgba, idle_baseline):
    keys = colour_keys(rgba)
    counts = Counter(keys[keys >= 0].tolist())
    fx_keys = [k for k, n in counts.items()
               if is_glow_colour(k) and n - idle_baseline.get(k, 0) >= FX_EXTRA_PIXELS]
    if not fx_keys:
        return np.zeros(keys.shape, bool)
    cand = np.isin(keys, fx_keys)
    core = cand & (rgba[..., :3].min(axis=2) >= FX_CORE_MIN_CHANNEL)
    labels, n = ndimage.label(cand, structure=np.ones((3, 3)))
    index = np.arange(1, n + 1)
    sizes = ndimage.sum(cand, labels, index=index)
    cores = ndimage.sum(core, labels, index=index)
    keep = np.zeros(n + 1, bool)
    keep[1:] = (sizes >= FX_MIN_COMPONENT) & (cores >= FX_MIN_CORE)
    # a trail's fading tail breaks off without a white core of its own; pull in
    # any glow fragment that lies close to a region already accepted as fx
    near = ndimage.binary_dilation(keep[labels], iterations=FX_ATTACH_DIST)
    keep[np.unique(labels[near & cand])] = True
    keep[0] = False
    fx = keep[labels]
    if not fx.any():
        return fx
    # ... and the darker end of its colour ramp, which is not "bright" at all:
    # grow through connected saturated pixels of the glow's own hue.
    hue, sat, val = hsv(rgba[..., :3])
    ramp = fx & (sat >= FX_RAMP_MIN_SAT)
    if not ramp.any():
        return fx
    seed_hue = circular_mean_deg(hue[ramp])
    same_hue = ((rgba[..., 3] > 0) & (sat >= FX_RAMP_MIN_SAT) & (val >= FX_RAMP_MIN_VAL)
                & (hue_distance(hue, seed_hue) <= FX_RAMP_HUE_DEG))
    reach = ndimage.binary_dilation(fx, iterations=FX_RAMP_DIST)
    return ndimage.binary_propagation(fx, mask=fx | (same_hue & reach), structure=np.ones((3, 3)))


def hsv(rgb):
    c = rgb.astype(np.float64) / 255.0
    mx, mn = c.max(axis=2), c.min(axis=2)
    d = mx - mn
    r, g, b = c[..., 0], c[..., 1], c[..., 2]
    safe = np.where(d == 0, 1, d)
    hue = np.where(mx == r, ((g - b) / safe) % 6,
                   np.where(mx == g, (b - r) / safe + 2, (r - g) / safe + 4)) * 60.0
    sat = np.where(mx == 0, 0, d / np.where(mx == 0, 1, mx))
    return hue, sat, mx


def circular_mean_deg(h):
    a = np.radians(h)
    return float(np.degrees(np.arctan2(np.sin(a).mean(), np.cos(a).mean())) % 360)


def hue_distance(h, ref):
    d = np.abs(h - ref) % 360
    return np.minimum(d, 360 - d)


def drop_specks(mask, min_size):
    labels, n = ndimage.label(mask, structure=np.ones((3, 3)))
    if n == 0:
        return mask
    sizes = ndimage.sum(mask, labels, index=np.arange(1, n + 1))
    keep = np.zeros(n + 1, bool)
    keep[1:] = sizes >= min_size
    return keep[labels]


def strip_border_outline(rgb, body):
    """Border outline pixels take the colour of the nearest non-outline body pixel."""
    dark = body & (rgb.astype(np.int32).sum(axis=2) <= OUTLINE_SUM)
    # dark pixels within 2 px of the outside: a 2-px thick outline is fully
    # stripped, while the interior lines that join it are left alone.
    outline = dark & (ndimage.distance_transform_cdt(body, metric='taxicab') <= 2)
    donors = body & ~outline
    out = rgb.copy()
    if outline.any() and donors.any():
        _, (iy, ix) = ndimage.distance_transform_edt(~donors, return_indices=True)
        out[outline] = rgb[iy[outline], ix[outline]]
    interior_lines = dark & ~outline
    return out, interior_lines


# --------------------------------------------------------------------------- #
# inflation                                                                   #
# --------------------------------------------------------------------------- #

def smoothstep(e0, e1, x):
    t = np.clip((x - e0) / (e1 - e0), 0.0, 1.0)
    return t * t * (3 - 2 * t)


def slim_factor(t):
    """Depth multiplier at body height t (0 feet .. 1 top), for thickness beyond SLIM_KEEP."""
    band = (smoothstep(UPPER_FROM - UPPER_RAMP, UPPER_FROM, t)
            * (1 - smoothstep(UPPER_TO, UPPER_TO + UPPER_RAMP, t)))
    upper = 1 - (1 - UPPER_DEPTH) * band
    waist = 1 - (1 - WAIST_DEPTH) * np.exp(-0.5 * ((t - WAIST_AT) / WAIST_WIDTH) ** 2)
    return ALL_DEPTH * upper * waist


def slim(half, feet_v, top_v):
    rows = np.arange(half.shape[0], dtype=np.float64)
    t = (feet_v - rows) / max(feet_v - top_v, 1)
    factor = slim_factor(t)[:, None]
    excess = np.maximum(half - SLIM_KEEP, 0.0)
    return np.minimum(half, SLIM_KEEP) + excess * factor


def inflate(body, creases):
    """Solve laplace(h) = -2 on the silhouette (Dirichlet 0 outside); return sqrt(h)."""
    ys, xs = np.nonzero(body)
    n = len(ys)
    half = np.zeros(body.shape, np.float64)
    if n == 0:
        return half
    index = -np.ones(body.shape, np.int64)
    index[ys, xs] = np.arange(n)

    rows = [np.arange(n)]
    cols = [np.arange(n)]
    vals = [np.full(n, 4.0) + CREASE_K * creases[ys, xs]]
    h, w = body.shape
    for dy, dx in ((0, 1), (0, -1), (1, 0), (-1, 0)):
        ny, nx = ys + dy, xs + dx
        ok = (ny >= 0) & (ny < h) & (nx >= 0) & (nx < w)
        nb = np.full(n, -1)
        nb[ok] = index[ny[ok], nx[ok]]
        inside = nb >= 0
        rows.append(np.arange(n)[inside])
        cols.append(nb[inside])
        vals.append(np.full(inside.sum(), -1.0))
    a = coo_matrix((np.concatenate(vals), (np.concatenate(rows), np.concatenate(cols))),
                   shape=(n, n)).tocsr()
    sol = spsolve(a, np.full(n, 2.0))
    # discrete strip of width w peaks at ((w+1)/2)^2, so sqrt - 0.5 is w/2
    half[ys, xs] = np.maximum(np.sqrt(np.maximum(sol, 0.0)) - 0.5, 0.0)
    return half


# --------------------------------------------------------------------------- #
# vox writing                                                                 #
# --------------------------------------------------------------------------- #

def write_vox(path, size, voxels, palette):
    def chunk(cid, content, children=b''):
        return cid + struct.pack('<ii', len(content), len(children)) + content + children

    size_chunk = chunk(b'SIZE', struct.pack('<iii', *size))
    body = bytearray(struct.pack('<i', len(voxels)))
    body += np.asarray(voxels, dtype=np.uint8).tobytes() if len(voxels) else b''
    xyzi_chunk = chunk(b'XYZI', bytes(body))
    rgba = bytearray()
    for i in range(256):
        r, g, b = palette[i] if i < len(palette) else (0, 0, 0)
        rgba += bytes([r, g, b, 255])
    rgba_chunk = chunk(b'RGBA', bytes(rgba))
    main = chunk(b'MAIN', b'', size_chunk + xyzi_chunk + rgba_chunk)
    with open(path, 'wb') as f:
        f.write(b'VOX ' + struct.pack('<i', 150) + main)


class Palette:
    def __init__(self):
        self.colours = []
        self.index = {}

    def __call__(self, rgb):
        key = (int(rgb[0]), int(rgb[1]), int(rgb[2]))
        if key in self.index:
            return self.index[key]
        if len(self.colours) < 255:
            self.colours.append(key)
            self.index[key] = len(self.colours)
            return self.index[key]
        best = min(range(len(self.colours)),
                   key=lambda i: sum((self.colours[i][c] - key[c]) ** 2 for c in range(3)))
        self.index[key] = best + 1
        return best + 1


# --------------------------------------------------------------------------- #
# obj export                                                                  #
# --------------------------------------------------------------------------- #
#
# The OBJs are written here, in-process, rather than by running
# vox_to_obj_exporter.py on the .vox files: a sweeping attack can be up to 322
# pixels wide and a VOX model stops at 255 voxels an axis. The rounded mesh has
# no such limit; the flat exporter's VoxelStruct only packs x into 8 bits of its
# key, which WideVoxelStruct replaces.

sys.path.insert(0, os.path.join(ROOT, 'Tools', 'Vox_to_Obj'))
import vox_to_obj_exporter as vte   # noqa: E402
import rounded_mesh                 # noqa: E402

ROUND_LAMBDA, ROUND_ITERATIONS, ROUND_SHARP = 0.65, 1, 60.0   # the exporter's --round defaults
MATERIAL = 'palette'


class WideVoxelStruct(vte.VoxelStruct):
    def setVoxel(self, voxel):
        self.voxels[(voxel.x, voxel.y, voxel.z)] = voxel

    def getVoxel(self, x, y, z):
        return self.voxels.get((x, y, z), None)

    def _index(self, x, y, z):
        return (x, y, z)


def to_struct(voxels):
    vs = WideVoxelStruct()
    for x, y, z, c in voxels.tolist():
        vs.setVoxel(vte.Voxel(x, y, z, c))
    return vs


def write_obj(path, voxels, mtl_name, rounded):
    vs = to_struct(voxels)
    with open(path, 'w') as fh:
        if rounded:
            mesh = rounded_mesh.RoundedMesh(vs, lam=ROUND_LAMBDA, iterations=ROUND_ITERATIONS,
                                            sharp_degrees=ROUND_SHARP)
            rounded_mesh.write_obj(fh, mesh, mtl_lib=mtl_name, material_name=MATERIAL,
                                   scale=OBJ_SCALE, center=False, ground=False, y_up=True)
        else:
            vte.exportObjToStream(fh, vs.toQuads(), mtl_lib=mtl_name, material_name=MATERIAL,
                                  scale=OBJ_SCALE, center=False, ground=False, y_up=True)


# --------------------------------------------------------------------------- #
# creature                                                                    #
# --------------------------------------------------------------------------- #

def build_creature(cid, src_dir, out_dir):
    frames = list_frames(src_dir, cid)
    images = {(a, f): load_rgba(p) for a, f, p in frames}

    # idle baseline: the most pixels any idle frame spends on each colour
    idle_baseline = {}
    for (a, _), im in images.items():
        if a != 1:
            continue
        keys = colour_keys(im)
        for k, n in Counter(keys[keys >= 0].tolist()).items():
            idle_baseline[k] = max(idle_baseline.get(k, 0), n)

    layers = {}
    for key, im in images.items():
        opaque = im[..., 3] > 0
        shadow = shadow_mask(im)
        fx = fx_mask(im, idle_baseline) if key[0] != 1 else np.zeros(opaque.shape, bool)
        body = drop_specks(opaque & ~shadow & ~fx, MIN_BODY_COMPONENT)
        layers[key] = (body, fx)

    # body height reference for slimming: feet and top of the first idle frame -- or of
    # the first frame with a body at all, since some creatures idle invisible (761)
    with_body = [k for k in sorted(layers) if layers[k][0].any()]
    if not with_body:
        raise ValueError(f'{cid}: no frame has a body')
    ref_body = layers[with_body[0]][0]
    ref_rows = np.nonzero(ref_body.any(axis=1))[0]
    feet_v, top_v = int(ref_rows.max()), int(ref_rows.min())

    # one crop for every frame of the creature, so the models share an origin
    union = np.zeros(next(iter(images.values())).shape[:2], bool)
    for body, fx in layers.values():
        union |= body | fx
    vs, us = np.nonzero(union)
    u0, u1 = int(max(us.min() - 1, 0)), int(min(us.max() + 2, union.shape[1]))
    v0, v1 = int(max(vs.min() - 1, 0)), int(min(vs.max() + 2, union.shape[0]))
    width, height = u1 - u0, v1 - v0
    depth = 2 * MAX_HALF + 3
    mid = MAX_HALF + 1
    fits_vox = max(width, height, depth) <= 255

    palette = Palette()
    os.makedirs(out_dir, exist_ok=True)
    obj_dir = os.path.join(out_dir, 'smoothed')
    npz_dir = os.path.join(out_dir, 'voxels')
    for d in (obj_dir, npz_dir):
        if os.path.isdir(d):
            for old in os.listdir(d):
                os.remove(os.path.join(d, old))
        os.makedirs(d, exist_ok=True)
    for old in glob.glob(os.path.join(out_dir, f'Fight_{cid}_*.vox')):
        os.remove(old)
    mtl_name = f'Fight_{cid}.mtl'
    png_name = f'Fight_{cid}_palette.png'

    manifest_frames = []
    pivot = None
    front_y = mid
    written = {}        # source-image hash -> (body name, fx name): identical frames share models
    seen_npz = {}

    for (a, f), im in sorted(images.items()):
        name = f'Fight_{cid}_{a}_{f:02d}'
        visible = im.copy()
        visible[visible[..., 3] == 0] = 0          # hidden rgb under alpha 0 differs between copies
        digest = hashlib.md5(visible.tobytes()).hexdigest()
        body, fx = layers[(a, f)]
        crop = (slice(v0, v1), slice(u0, u1))
        body_c, fx_c = body[crop], fx[crop]

        if pivot is None and (a, f) == with_body[0]:
            # feet: bottom row of the reference frame's body, centred on it
            bottom = np.nonzero(body_c.any(axis=1))[0].max()
            cols = np.nonzero(body_c[bottom])[0]
            pivot = [int(round(cols.mean())), mid, height - 1 - int(bottom)]

        if digest in written:
            body_name, fx_name = written[digest]
        else:
            rgb, creases = strip_border_outline(im[..., :3], body)
            half = inflate(body, creases.astype(np.float64)) * DEPTH_SCALE
            half = np.minimum(slim(half, feet_v, top_v), MAX_HALF)
            rgb_c, half_c, raw_c = rgb[crop], half[crop], im[..., :3][crop]

            columns = []
            vv, uu = np.nonzero(body_c)
            for v, u in zip(vv.tolist(), uu.tolist()):
                c = palette(rgb_c[v, u])
                z = height - 1 - v
                hf = int(half_c[v, u] * FRONT_FACTOR)
                hb = int(half_c[v, u] * BACK_FACTOR)
                front_y = min(front_y, mid - hf)
                ys = np.arange(mid - hf, mid + hb + 1)
                columns.append(np.stack([np.full_like(ys, u), ys, np.full_like(ys, z),
                                         np.full_like(ys, c)], 1))
            voxels = (np.concatenate(columns) if columns else np.zeros((0, 4))).astype(np.uint16)

            body_name = name if len(voxels) else None      # the creature can vanish mid-spell
            fx_name = None
            fx_voxels = np.zeros((0, 4), np.uint16)
            if fx_c.any():
                vv, uu = np.nonzero(fx_c)
                fx_voxels = np.array([(u, mid, height - 1 - v, palette(raw_c[v, u]))
                                      for v, u in zip(vv.tolist(), uu.tolist())], np.uint16)
                fx_name = name + '_fx'

            np.savez_compressed(os.path.join(npz_dir, name + '.npz'), body=voxels, fx=fx_voxels)
            if body_name:
                write_obj(os.path.join(obj_dir, body_name + '.obj'), voxels, mtl_name, rounded=True)
            if fx_name:
                write_obj(os.path.join(obj_dir, fx_name + '.obj'), fx_voxels, mtl_name, rounded=False)
            if fits_vox and body_name:
                write_vox(os.path.join(out_dir, body_name + '.vox'), (width, depth, height),
                          voxels.astype(np.uint8), palette.colours)
                if fx_name:
                    write_vox(os.path.join(out_dir, fx_name + '.vox'), (width, depth, height),
                              fx_voxels.astype(np.uint8), palette.colours)
            written[digest] = (body_name, fx_name)
            seen_npz[digest] = name + '.npz'

        manifest_frames.append({
            'animation': ANIM_NAMES.get(a, str(a)), 'index': f,
            'time': round(FRAME_SECONDS * (f - 1), 3),
            'body': body_name + '.obj' if body_name else None,
            'fx': fx_name + '.obj' if fx_name else None,
            'voxels': seen_npz[digest],
            'fxPixels': int(fx_c.sum()),
        })

    # every frame of the creature shares one palette, so one material and one texture
    palette_rgba = [c + (255,) for c in palette.colours] + [(0, 0, 0, 255)] * (256 - len(palette.colours))
    vte.writePalettePng(os.path.join(obj_dir, png_name), palette_rgba)
    vte.writeMtl(os.path.join(obj_dir, mtl_name), png_name, MATERIAL)

    manifest = {
        'creature': cid,
        'source': os.path.relpath(os.path.join(src_dir, cid), ROOT).replace('\\', '/'),
        'crop': {'x0': u0, 'y0': v0, 'x1': u1, 'y1': v1,
                 'note': 'pixel rect in the source frame; vox x = px - x0, vox z = y1-1 - py'},
        'size': [int(width), int(depth), int(height)],
        'hasVox': fits_vox,
        'midPlaneY': mid,
        'frontY': int(front_y),
        'objScale': OBJ_SCALE,
        'pivot': pivot,
        'frameSeconds': FRAME_SECONDS,
        'bodyType': BODY_TYPE,
        'params': {'DEPTH_SCALE': DEPTH_SCALE, 'FRONT_FACTOR': FRONT_FACTOR,
                   'BACK_FACTOR': BACK_FACTOR, 'MAX_HALF': MAX_HALF, 'CREASE_K': CREASE_K,
                   'SLIM_KEEP': SLIM_KEEP, 'UPPER_DEPTH': UPPER_DEPTH, 'WAIST_DEPTH': WAIST_DEPTH, 'ALL_DEPTH': ALL_DEPTH,
                   'slimFeetRow': feet_v, 'slimTopRow': top_v},
        'paletteColours': len(palette.colours),
        'models': len(written),
        'frames': manifest_frames,
    }
    with open(os.path.join(out_dir, f'Fight_{cid}.json'), 'w', encoding='utf-8') as fh:
        json.dump(manifest, fh, indent=2)
    return manifest


def body_type_of(cid):
    """(type name, {'upper': .., 'waist': ..}) for a creature, from fight_body_types.json."""
    with open(BODY_TYPES_FILE, encoding='utf-8') as fh:
        table = json.load(fh)
    kind = table['creatures'].get(cid, table['default'])
    return kind, table['profiles'][kind]


def run_one(job):
    """One creature; module globals are re-imported in a worker, so the tuned values ride along."""
    cid, src, out, params = job
    params = dict(params)
    kind, profile = body_type_of(cid)
    # an explicit --upper / --waist on the command line wins over the body type's profile
    if params.get('UPPER_DEPTH') is None:
        params['UPPER_DEPTH'] = profile['upper']
    if params.get('WAIST_DEPTH') is None:
        params['WAIST_DEPTH'] = profile['waist']
    if params.get('ALL_DEPTH') is None:
        params['ALL_DEPTH'] = profile.get('all', 1.0)
    params['BODY_TYPE'] = kind
    globals().update(params)
    try:
        m = build_creature(cid, src, os.path.join(out, cid))
        return (cid, f'{kind}: {len(m["frames"])} frames, {m["models"]} models, size {m["size"]}'
                     + ('' if m['hasVox'] else ' (too wide for .vox: obj only)'), None)
    except Exception:            # report and carry on with the other creatures
        import traceback
        return (cid, None, traceback.format_exc())


if __name__ == '__main__':
    ap = argparse.ArgumentParser(description='Fight animation frames -> per-frame 3D models')
    ap.add_argument('creatures', nargs='+', help="3-digit animation ids, e.g. 001 701, or 'all'")
    ap.add_argument('--src', default=DEFAULT_SRC, help='folder holding <id>/Fight-<id>-A-FF.png')
    ap.add_argument('--out', default=DEFAULT_OUT, help='output root; files go to <out>/<id>/')
    ap.add_argument('--jobs', type=int, default=max(1, (os.cpu_count() or 2) - 2))
    ap.add_argument('--depth-scale', type=float, default=DEPTH_SCALE)
    ap.add_argument('--max-half', type=int, default=MAX_HALF)
    ap.add_argument('--crease', type=float, default=CREASE_K)
    ap.add_argument('--back', type=float, default=BACK_FACTOR)
    ap.add_argument('--upper', type=float, default=None,
                    help='depth kept across the upper body (1 = off); default from fight_body_types.json')
    ap.add_argument('--all', type=float, default=None, dest='all_depth',
                    help='depth kept at every height (1 = off); default from fight_body_types.json')
    ap.add_argument('--waist', type=float, default=None,
                    help='extra depth kept at the waist (1 = off); default from fight_body_types.json')
    args = ap.parse_args()
    params = {'DEPTH_SCALE': args.depth_scale, 'MAX_HALF': args.max_half, 'CREASE_K': args.crease,
              'BACK_FACTOR': args.back, 'UPPER_DEPTH': args.upper, 'WAIST_DEPTH': args.waist,
              'ALL_DEPTH': args.all_depth}

    if [c.lower() for c in args.creatures] == ['all']:
        ids = sorted(d for d in os.listdir(args.src) if re.fullmatch(r'\d{3}', d))
    else:
        ids = [c.zfill(3) for c in args.creatures]

    jobs = [(cid, args.src, args.out, params) for cid in ids]
    failed = []

    def report(result):
        cid, summary, error = result
        print(f'== {cid}: ' + (summary if summary else 'FAILED\n' + error), flush=True)
        if error:
            failed.append(cid)

    if args.jobs > 1 and len(jobs) > 1:
        from multiprocessing import Pool
        with Pool(min(args.jobs, len(jobs))) as pool:
            for result in pool.imap_unordered(run_one, jobs):
                report(result)
    else:
        for job in jobs:
            report(run_one(job))
    if failed:
        print('failed:', ' '.join(sorted(failed)))
        sys.exit(1)
