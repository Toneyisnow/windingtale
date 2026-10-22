"""Make a new creature icon from an existing one by recolouring palette entries.

A creature icon is three OBJ frames (Icon_NNN_01..03) whose colours all come from
a 256x1 palette PNG beside each one. Recolouring a part of the creature -- the
hair, say -- is therefore only a matter of changing the palette entries that
part uses; the meshes are copied as they are.

    python recolor_icon.py 008 902 --from-hue 300-350 --to-hue 275

copies WindingTale2/Assets/Resources/Icons/008 to Icons/902 (renaming the files
and the mtllib / map_Kd references inside them, and giving each .meta a new
guid so the import settings carry over), then turns every palette entry whose
hue falls in --from-hue (and that is not grey) to --to-hue, keeping its
saturation and brightness so the shading survives. The same files are written
to Resources/Remastered/Icons/NNN as the source copy.
"""
import argparse
import colorsys
import os
import re
import shutil
import uuid

from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.normpath(os.path.join(HERE, "..", ".."))
UNITY_ICONS = os.path.join(ROOT, "WindingTale2", "Assets", "Resources", "Icons")
REMASTERED_ICONS = os.path.join(ROOT, "Resources", "Remastered", "Icons")

MIN_SATURATION = 0.25


def recolor_palette(path, lo, hi, to_hue):
    im = Image.open(path).convert("RGBA")
    px = list(im.getdata())
    changed = []
    for i, (r, g, b, a) in enumerate(px):
        h, s, v = colorsys.rgb_to_hsv(r / 255.0, g / 255.0, b / 255.0)
        deg = h * 360.0
        if s < MIN_SATURATION or not (lo <= deg <= hi):
            continue
        nr, ng, nb = colorsys.hsv_to_rgb(to_hue / 360.0, s, v)
        px[i] = (int(round(nr * 255)), int(round(ng * 255)), int(round(nb * 255)), a)
        changed.append(((r, g, b), px[i][:3]))
    im.putdata(px)
    im.save(path)
    return changed


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("src")
    ap.add_argument("dst")
    ap.add_argument("--from-hue", required=True, help="hue range in degrees, e.g. 300-350")
    ap.add_argument("--to-hue", type=float, required=True)
    args = ap.parse_args()
    lo, hi = (float(x) for x in args.from_hue.split("-"))

    src_dir = os.path.join(UNITY_ICONS, args.src)
    dst_dir = os.path.join(UNITY_ICONS, args.dst)
    os.makedirs(dst_dir, exist_ok=True)

    old, new = "Icon_%s_" % args.src, "Icon_%s_" % args.dst
    for name in sorted(os.listdir(src_dir)):
        if not name.startswith(old):
            continue
        target = os.path.join(dst_dir, name.replace(old, new))
        source = os.path.join(src_dir, name)
        if name.endswith(".meta"):
            text = open(source, encoding="utf-8").read()
            text = re.sub(r"(?m)^guid: [0-9a-f]{32}$", "guid: " + uuid.uuid4().hex, text, count=1)
            open(target, "w", encoding="utf-8", newline="").write(text)
        elif name.endswith((".obj", ".mtl")):
            text = open(source, encoding="utf-8").read().replace(old, new)
            open(target, "w", encoding="utf-8", newline="").write(text)
        else:
            shutil.copyfile(source, target)

    folder_meta = os.path.join(UNITY_ICONS, args.dst + ".meta")
    if not os.path.exists(folder_meta):
        open(folder_meta, "w", newline="\n").write(
            "fileFormatVersion: 2\nguid: %s\nfolderAsset: yes\nDefaultImporter:\n"
            "  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n"
            % uuid.uuid4().hex)

    for name in sorted(os.listdir(dst_dir)):
        if name.endswith(".png"):
            changed = recolor_palette(os.path.join(dst_dir, name), lo, hi, args.to_hue)
            print("%s: %d palette entries recoloured" % (name, len(changed)))
            for before, after in changed:
                print("    %s -> %s" % (before, after))

    remastered = os.path.join(REMASTERED_ICONS, args.dst)
    os.makedirs(remastered, exist_ok=True)
    for name in os.listdir(dst_dir):
        if not name.endswith(".meta"):
            shutil.copyfile(os.path.join(dst_dir, name), os.path.join(remastered, name))
    print("-> %s\n-> %s" % (dst_dir, remastered))


if __name__ == "__main__":
    main()
