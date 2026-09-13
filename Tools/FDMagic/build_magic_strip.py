"""
Packs each original FlameDragon magic sub-animation (a folder of NN-01.png ... NN-16.png
frames, each a different size) into the uniform-cell strip the Unity battle scene plays.

Each frame is drawn into its own w x h cell at the per-frame offset listed in the
magic's .txt ("子火焰 每一帧的相对坐标"), so at runtime every frame is the same rect and
the sub-animation is placed once by its screen position -- no per-frame offsets left.

Layout (MagicStrip.cs relies on it): cell i's top-left is at (1 + i * (w + 2), 1), a
1px transparent gutter all round every cell, on a power-of-two canvas so no import
setting can rescale it.

    python build_magic_strip.py 101 102
"""

import os
import sys

from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
UNITY_RESOURCES = os.path.join(HERE, "..", "..", "WindingTale2", "Assets", "Resources")

RECIPES = {
    # 火焰术: 16-frame fire column, 22 x 154, offsets from 19-火焰术/火焰术.txt
    101: [
        dict(
            source=os.path.join("FSmagic", "19-火焰术", "火柱"),
            cell=(22, 154),
            offsets=[(11, 0), (10, 5), (10, 110), (11, 134), (11, 148), (5, 147), (0, 145), (0, 144),
                     (0, 144), (0, 70), (0, 60), (0, 67), (0, 73), (0, 26), (0, 0), (0, 0)],
            output=os.path.join("Magics", "101", "FireColumn.png"),
        ),
    ],
    # 烈焰术: a red and a blue 15-frame column, 22 x 154, sharing the offsets from
    # 20-烈焰术/烈焰术.txt
    102: [
        dict(
            source=os.path.join("FSmagic", "20-烈焰术", "红色火柱"),
            cell=(22, 154),
            offsets=[(11, 0), (10, 5), (10, 110), (11, 134), (11, 145), (5, 147), (0, 145), (0, 144),
                     (0, 144), (0, 88), (0, 0), (0, 0), (0, 0), (0, 0), (0, 0)],
            output=os.path.join("Magics", "102", "RedColumn.png"),
        ),
        dict(
            source=os.path.join("FSmagic", "20-烈焰术", "蓝色火柱"),
            cell=(22, 154),
            offsets=[(11, 0), (10, 5), (10, 110), (11, 134), (11, 145), (5, 147), (0, 145), (0, 144),
                     (0, 144), (0, 88), (0, 0), (0, 0), (0, 0), (0, 0), (0, 0)],
            output=os.path.join("Magics", "102", "BlueColumn.png"),
        ),
    ],
}


def next_pot(n):
    p = 1
    while p < n:
        p *= 2
    return p


def build(magic_id):
    for recipe in RECIPES[magic_id]:
        build_strip(recipe)


def build_strip(recipe):
    w, h = recipe["cell"]
    offsets = recipe["offsets"]
    source = os.path.join(HERE, recipe["source"])
    frames = sorted(f for f in os.listdir(source) if f.lower().endswith(".png"))
    if len(frames) != len(offsets):
        raise SystemExit("%d frames but %d offsets" % (len(frames), len(offsets)))

    canvas = Image.new("RGBA", (next_pot(len(frames) * (w + 2)), next_pot(h + 2)), (0, 0, 0, 0))
    for i, (name, (ox, oy)) in enumerate(zip(frames, offsets)):
        frame = Image.open(os.path.join(source, name)).convert("RGBA")
        if ox + frame.width > w or oy + frame.height > h:
            raise SystemExit("%s at %s overflows the %dx%d cell" % (name, (ox, oy), w, h))
        canvas.alpha_composite(frame, (1 + i * (w + 2) + ox, 1 + oy))

    output = os.path.join(UNITY_RESOURCES, recipe["output"])
    os.makedirs(os.path.dirname(output), exist_ok=True)
    canvas.save(output)
    print("wrote %s (%dx%d, %d cells)" % (os.path.normpath(output), canvas.width, canvas.height, len(frames)))


if __name__ == "__main__":
    for arg in sys.argv[1:] or [str(k) for k in RECIPES]:
        build(int(arg))
