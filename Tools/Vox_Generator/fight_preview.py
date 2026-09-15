"""
QA preview for fight_to_vox.py output.

For every frame of a creature renders, side by side:
    original 2D frame | 3D from the battle camera | turned 35 deg | turned 70 deg
with smooth shading (normals from the blurred occupancy field, roughly what the
--round OBJ export looks like in Unity), and writes
    <dir>/preview/Fight_<id>_<anim>.gif      animated, 120 ms a frame
    <dir>/preview/Fight_<id>_sheet.png       every frame stacked

Reads the voxels fight_to_vox.py saved in <dir>/voxels/.

Usage:
  python fight_preview.py <Fights3D>/<id> [<Fights3D>/<id> ...] [--scale 2] [--jobs 12]
"""

import argparse
import json
import math
import os

import numpy as np
from PIL import Image, ImageDraw
from scipy import ndimage


ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..'))
BG = (72, 84, 72)
VIEWS = ((0, 'battle camera'), (35, 'turn 35'), (70, 'turn 70'))
PITCH = 12                       # degrees the camera looks down


def render(size, vox, pal, yaw_deg, scale, fx=None):
    sx, sy, sz = size
    if len(vox) == 0:                       # the creature is gone; only its fx is left
        vox = np.zeros((0, 4), np.int64)
    occ = np.zeros((sx, sy, sz), np.float32)
    occ[vox[:, 0], vox[:, 1], vox[:, 2]] = 1
    grad = np.gradient(ndimage.gaussian_filter(occ, 1.2))
    normals = -np.stack([g[vox[:, 0], vox[:, 1], vox[:, 2]] for g in grad], 1)
    normals /= np.linalg.norm(normals, axis=1, keepdims=True) + 1e-6

    # surface voxels only
    pad = np.pad(occ, 1)
    x, y, z = vox[:, 0] + 1, vox[:, 1] + 1, vox[:, 2] + 1
    exposed = ((pad[x - 1, y, z] == 0) | (pad[x + 1, y, z] == 0) | (pad[x, y - 1, z] == 0)
               | (pad[x, y + 1, z] == 0) | (pad[x, y, z - 1] == 0) | (pad[x, y, z + 1] == 0))
    vox, normals = vox[exposed], normals[exposed]

    yaw, pitch = math.radians(yaw_deg), math.radians(PITCH)
    p = vox[:, :3].astype(np.float64) + 0.5 - np.array([sx / 2, sy / 2, 0])
    # yaw about vertical Z, then pitch about X; camera looks along +Y
    cy, syw = math.cos(yaw), math.sin(yaw)
    rx = p[:, 0] * cy - p[:, 1] * syw
    ry = p[:, 0] * syw + p[:, 1] * cy
    cp, sp = math.cos(pitch), math.sin(pitch)
    rz = p[:, 2] * cp - ry * sp
    depth = ry * cp + p[:, 2] * sp
    nx = normals[:, 0] * cy - normals[:, 1] * syw
    ny = normals[:, 0] * syw + normals[:, 1] * cy
    nz = normals[:, 2]

    light = np.array([-0.45, -0.7, 0.55])
    light /= np.linalg.norm(light)
    lambert = np.clip(nx * light[0] + ny * light[1] + nz * light[2], 0, 1)
    albedo = pal[vox[:, 3] - 1, :3].astype(np.float64)
    colour = albedo * (0.45 + 0.75 * lambert)[:, None]

    w = int(math.ceil((sx * abs(cy) + sy * abs(syw)) + 4)) * scale
    h = int(sz * 1.08 + sy * sp + 4) * scale
    img = np.tile(np.array(BG, np.float64), (h, w, 1))
    u = ((rx + w / scale / 2) * scale).astype(int)
    v = ((h / scale - 2 - rz) * scale).astype(int)
    order = np.argsort(-depth)                        # far first, near overwrites
    k = scale + 1
    for i in order:
        uu, vv = u[i], v[i]
        img[max(vv - k + 1, 0):vv + 1, max(uu, 0):uu + k] = colour[i]
    if fx is not None:
        fsize, fvox, fpal = fx
        fp = fvox[:, :3].astype(np.float64) + 0.5 - np.array([sx / 2, sy / 2, 0])
        frx = fp[:, 0] * cy - fp[:, 1] * syw
        fry = fp[:, 0] * syw + fp[:, 1] * cy
        frz = fp[:, 2] * cp - fry * sp
        fu = ((frx + w / scale / 2) * scale).astype(int)
        fv = ((h / scale - 2 - frz) * scale).astype(int)
        fcol = fpal[fvox[:, 3] - 1, :3].astype(np.float64)
        glow = np.zeros((h, w, 3))
        for i in range(len(fvox)):
            glow[max(fv[i] - k + 1, 0):fv[i] + 1, max(fu[i], 0):fu[i] + k] = fcol[i]
        lit = glow.any(axis=2)
        img[lit] = np.minimum(img[lit] * 0.35 + glow[lit] * 0.9, 255)   # additive-ish glow
    return Image.fromarray(np.clip(img, 0, 255).astype(np.uint8))


def original_crop(manifest, frame, scale):
    c = manifest['crop']
    src = os.path.join(ROOT, manifest['source'],
                       f"Fight-{manifest['creature']}-{ {'idle': 1, 'attack': 2, 'spell': 3}[frame['animation']] }-{frame['index']:02d}.png")
    im = Image.open(src).convert('RGBA').crop((c['x0'], c['y0'], c['x1'], c['y1']))
    bg = Image.new('RGBA', im.size, BG + (255,))
    bg.alpha_composite(im)
    return bg.convert('RGB').resize((im.width * scale, im.height * scale), Image.NEAREST)


def compose(panels, labels):
    gap = 8
    h = max(p.height for p in panels) + 18
    w = sum(p.width for p in panels) + gap * (len(panels) - 1)
    out = Image.new('RGB', (w, h), (40, 44, 40))
    d = ImageDraw.Draw(out)
    x = 0
    for p, label in zip(panels, labels):
        out.paste(p, (x, h - p.height))
        d.text((x + 4, 2), label, fill=(230, 230, 230))
        x += p.width + gap
    return out


def load_frame(folder, manifest, frame, palette):
    data = np.load(os.path.join(folder, 'voxels', frame['voxels']))
    size = tuple(manifest['size'])
    fx = (size, data['fx'].astype(np.int64), palette) if len(data['fx']) else None
    return size, data['body'].astype(np.int64), palette, fx


def preview_creature(folder, scale):
    cid = os.path.basename(os.path.normpath(folder))
    manifest = json.load(open(os.path.join(folder, f'Fight_{cid}.json'), encoding='utf-8'))
    out_dir = os.path.join(folder, 'preview')
    os.makedirs(out_dir, exist_ok=True)
    png = Image.open(os.path.join(folder, 'smoothed', f'Fight_{cid}_palette.png')).convert('RGBA')
    pal = np.array(png, np.uint8).reshape(256, 4)

    by_anim, rows = {}, []
    for fr in manifest['frames']:
        size, vox, pal, fx = load_frame(folder, manifest, fr, pal)
        panels = [original_crop(manifest, fr, scale)]
        panels += [render(size, vox, pal, yaw, scale, fx) for yaw, _ in VIEWS]
        labels = [f"{fr['animation']} {fr['index']:02d}  2D"] + [n for _, n in VIEWS]
        img = compose(panels, labels)
        by_anim.setdefault(fr['animation'], []).append(img)
        rows.append(img)

    for anim, imgs in by_anim.items():
        path = os.path.join(out_dir, f'Fight_{cid}_{anim}.gif')
        imgs[0].save(path, save_all=True, append_images=imgs[1:], duration=120, loop=0)
    sheet = Image.new('RGB', (max(r.width for r in rows), sum(r.height for r in rows)))
    yy = 0
    for r in rows:
        sheet.paste(r, (0, yy))
        yy += r.height
    sheet.save(os.path.join(out_dir, f'Fight_{cid}_sheet.png'))
    return cid


def run_one(job):
    try:
        return preview_creature(*job), None
    except Exception:
        import traceback
        return job[0], traceback.format_exc()


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument('folders', nargs='+')
    ap.add_argument('--scale', type=int, default=2)
    ap.add_argument('--jobs', type=int, default=max(1, (os.cpu_count() or 2) - 2))
    args = ap.parse_args()
    jobs = [(folder, args.scale) for folder in args.folders]
    failed = False
    if args.jobs > 1 and len(jobs) > 1:
        from multiprocessing import Pool
        with Pool(min(args.jobs, len(jobs))) as pool:
            results = list(pool.imap_unordered(run_one, jobs))
    else:
        results = [run_one(job) for job in jobs]
    for name, error in results:
        print('preview', name, ('FAILED\n' + error) if error else 'ok')
        failed = failed or error is not None
    if failed:
        raise SystemExit(1)


if __name__ == '__main__':
    main()
