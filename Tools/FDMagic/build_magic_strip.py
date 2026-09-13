"""
Packs each original FlameDragon magic sub-animation (a folder of NN-01.png ... NN-16.png
frames, each a different size) into the uniform-cell strip the Unity battle scene plays.

Each frame is drawn into its own w x h cell at the per-frame offset listed in the
magic's .txt ("子火焰 每一帧的相对坐标"), so at runtime every frame is the same rect and
the sub-animation is placed once by its screen position -- no per-frame offsets left.

Layout (MagicEffect.CreateStripAssets relies on it): cells run in rows of
columns = max(1, 2048 // (w + 2)), so cell i's top-left is at
(1 + (i % columns) * (w + 2), 1 + (i // columns) * (h + 2)) -- a 1px transparent gutter all
round every cell, on a power-of-two canvas no bigger than 2048 wide, so no import setting
can rescale it. Narrow strips fit on one row.

A recipe may also fade frames out towards the original screen's edges (fade=...), for
magics drawn coming in from off screen: in Unity the original screen's edge is in the middle
of the view, and a hard cut there would show.

    python build_magic_strip.py 101 102 103 104 105 106 107 108 109
"""

import os
import sys

from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
UNITY_RESOURCES = os.path.join(HERE, "..", "..", "WindingTale2", "Assets", "Resources")

# 轰雷术's five balls at full spread, relative to where they gather (181, 85).
RING_107 = [(-30, -26), (-30, 25), (11, -41), (11, 41), (36, 0)]

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
    # 炎龙术: the dragon's head reaching out of the top right corner of the screen (prepare),
    # the fire it breathes (burn) and the head pulling back (end). Each frame has its own
    # screen position in 27-炎龙术/炎龙术.txt; a strip's cell is the box round all of them,
    # at the "origin" screen position, and the head fades out towards the screen edges it
    # was cut off at.
    103: [
        dict(
            source=os.path.join("FSmagic", "27-炎龙术", "0 准备阶段"),
            cell=(205, 142),
            origin=(115, 0),
            positions=[(291, 34), (267, 31), (225, 0), (178, 0), (145, 0), (137, 0), (149, 0), (174, 0),
                       (175, 0), (135, 0), (115, 0)],
            fade=dict(right=40, top=24),
            output=os.path.join("Magics", "103", "DragonPrepare.png"),
        ),
        dict(
            source=os.path.join("FSmagic", "27-炎龙术", "1 灼烧"),
            cell=(191, 122),
            origin=(1, 40),
            positions=[(1, 43), (1, 40)],
            output=os.path.join("Magics", "103", "DragonBurn.png"),
        ),
        dict(
            source=os.path.join("FSmagic", "27-炎龙术", "2 结束阶段"),
            cell=(214, 118),
            origin=(106, 0),
            positions=[(109, 0), (106, 0), (118, 0), (145, 0), (177, 0)],
            fade=dict(right=40, top=24),
            output=os.path.join("Magics", "103", "DragonEnd.png"),
        ),
    ],
    # 天火术 (the simplified version 40-天火术/天火术.txt describes): 11-frame fire bombs that
    # fall and burst, and red and blue diagonal beams from the sky, each in its own cell with
    # the .txt's per-frame offsets. The beams are cut off at the top of the screen, and the
    # blue ones -- standing at x -30 -- at its left edge too, so they fade out towards those.
    104: [
        dict(
            source=os.path.join("FSmagic", "40-天火术", "火弹"),
            cell=(178, 156),
            offsets=[(142, 0), (68, 22), (22, 68), (11, 95), (6, 90), (2, 87), (1, 86), (1, 85), (1, 85),
                     (1, 85), (1, 85)],
            output=os.path.join("Magics", "104", "FireBomb.png"),
        ),
        dict(
            source=os.path.join("FSmagic", "40-天火术", "红色 火柱"),
            cell=(171, 146),
            origin=(30, 0),
            positions=[(30 + x, y) for x, y in [(127, 0), (0, 0), (0, 0), (0, 0), (0, 0), (0, 0), (0, 0), (0, 0),
                                               (0, 42), (0, 83), (0, 91)]],
            fade=dict(top=24),
            output=os.path.join("Magics", "104", "RedBeam.png"),
        ),
        dict(
            source=os.path.join("FSmagic", "40-天火术", "蓝色 火柱"),
            cell=(171, 146),
            origin=(-30, 0),
            positions=[(-30 + x, y) for x, y in [(127, 0), (0, 0), (0, 0), (0, 0), (0, 0), (0, 0), (0, 0), (0, 0),
                                                (0, 42), (0, 83), (0, 91)]],
            fade=dict(top=24, left=24),
            output=os.path.join("Magics", "104", "BlueBeam.png"),
        ),
    ],
    # 落雷术: two 6-frame lightning bolts like 电击术's. The .txt has its A and B offset lists
    # the wrong way round (its "A" list is the 闪电B folder's frames) and a few pixels out, so
    # these were measured off 25-落雷术/落雷术.gif: each frame relative to its bolt frame, in
    # an 82 x 149 cell.
    106: [
        dict(
            source=os.path.join("FSmagic", "25-落雷术", "闪电A"),
            cell=(82, 149),
            offsets=[(12, 0), (4, 0), (1, 0), (7, 30), (0, 47), (41, 79)],
            output=os.path.join("Magics", "106", "LightningA.png"),
        ),
        dict(
            source=os.path.join("FSmagic", "25-落雷术", "闪电B"),
            cell=(82, 149),
            offsets=[(64, 0), (62, 0), (1, 0), (0, 25), (5, 39), (23, 55)],
            output=os.path.join("Magics", "106", "LightningB.png"),
        ),
    ],
    # 轰雷术: dark balls gathering round the caster (prepare/end), lightning bolts fired from
    # them and thunder bombs bursting on the target, offsets from 33-轰雷术/轰雷术.txt. The
    # prepare/end frames are not in the folder: they are the one ball image drawn five times
    # in a ring spreading out from (181, 85) -- the .txt's region positions are that ring's
    # corner at spread 0, 1/6, 1/3, 1/2, 2/3, 1 -- with the ring measured off the GIF.
    107: [
        dict(
            composite=os.path.join("FSmagic", "33-轰雷术", "0－电球.png"),
            cell=(99, 115),
            origin=(151, 44),
            positions=[[(round(181 + s * dx), round(85 + s * dy)) for dx, dy in RING_107] for s in
                       (0, 1 / 6, 1 / 3, 1 / 2, 2 / 3, 1)],
            output=os.path.join("Magics", "107", "BallRing.png"),
        ),
        dict(
            composite=os.path.join("FSmagic", "33-轰雷术", "0－电球.png"),
            cell=(33, 33),
            origin=(0, 0),
            positions=[[(0, 0)]],
            output=os.path.join("Magics", "107", "Ball.png"),
        ),
        dict(
            source=os.path.join("FSmagic", "33-轰雷术", "闪电"),
            cell=(185, 95),
            offsets=[(0, 0), (87, 39), (102, 41), (141, 45), (141, 45)],
            output=os.path.join("Magics", "107", "Lightning.png"),
        ),
        dict(
            source=os.path.join("FSmagic", "33-轰雷术", "雷弹"),
            cell=(143, 111),
            offsets=[(33, 26), (17, 12), (12, 8), (6, 4), (0, 0)],
            output=os.path.join("Magics", "107", "ThunderBomb.png"),
        ),
    ],
    # 神雷术: a 5-frame bolt from the sky. The .txt's offsets for frames 1-4 are 3-7px left of
    # where 38-神雷术/神雷术.gif draws them; these are measured off the GIF, in a 62 x 148 cell.
    108: [
        dict(
            source=os.path.join("FSmagic", "38-神雷术", "落雷"),
            cell=(62, 148),
            offsets=[(21, 0), (3, 0), (18, 0), (20, 0), (32, 0)],
            output=os.path.join("Magics", "108", "HolyBolt.png"),
        ),
    ],
    # 圣光弹: beams of light shot in from four directions (a tip frame, then the whole beam)
    # and the light orbs they leave on the target, which burst. Positions from
    # 29-圣光弹/圣光弹.txt, except two measured off its GIF: the bottom beam's tip is at x 22 of
    # its region, not 35, and the burst frames sit 1-2px right/down of the .txt's offsets.
    # The beams come in from beyond the screen's right and bottom edges, so they fade out
    # towards those (and the upper one towards the top).
    109: [
        dict(
            source=os.path.join("FSmagic", "29-圣光弹", "右上 光柱"),
            cell=(251, 60),
            origin=(69, 0),
            positions=[(69 + 156, 0), (69, 0)],
            fade=dict(right=40, top=24),
            output=os.path.join("Magics", "109", "BeamUpper.png"),
        ),
        dict(
            source=os.path.join("FSmagic", "29-圣光弹", "右中 光柱"),
            cell=(253, 70),
            origin=(67, 73),
            positions=[(67 + 200, 73 + 15), (67, 73)],
            fade=dict(right=40),
            output=os.path.join("Magics", "109", "BeamMiddle.png"),
        ),
        dict(
            source=os.path.join("FSmagic", "29-圣光弹", "右下 光柱"),
            cell=(250, 128),
            origin=(70, 72),
            positions=[(70 + 128, 72 + 86), (70, 72)],
            fade=dict(right=40, bottom=30),
            output=os.path.join("Magics", "109", "BeamLower.png"),
        ),
        dict(
            source=os.path.join("FSmagic", "29-圣光弹", "下中 光柱"),
            cell=(116, 132),
            origin=(39, 68),
            positions=[(39 + 22, 68 + 95), (39, 68)],
            fade=dict(bottom=30),
            output=os.path.join("Magics", "109", "BeamBottom.png"),
        ),
        dict(
            source=os.path.join("FSmagic", "29-圣光弹", "击中 光弹"),
            cell=(145, 112),
            offsets=[(60, 39), (35, 27), (19, 13), (14, 9), (8, 5), (2, 1)],
            output=os.path.join("Magics", "109", "LightOrb.png"),
        ),
    ],
    # 电击术: two 6-frame lightning bolts. The .txt's per-frame x offsets and 43px cell do not
    # match its own GIF, so these were measured off 23-电击术/电击术.gif instead: every frame's
    # position relative to the bolt's column frame, in a 47 x 150 cell (y offsets as in the .txt).
    105: [
        dict(
            source=os.path.join("FSmagic", "23-电击术", "闪电A"),
            cell=(47, 150),
            offsets=[(9, 0), (1, 0), (4, 17), (0, 65), (14, 32), (14, 0)],
            output=os.path.join("Magics", "105", "LightningA.png"),
        ),
        dict(
            source=os.path.join("FSmagic", "23-电击术", "闪电B"),
            cell=(47, 150),
            offsets=[(10, 0), (9, 0), (4, 17), (0, 65), (14, 32), (14, 0)],
            output=os.path.join("Magics", "105", "LightningB.png"),
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


SCREEN_WIDTH = 320
SCREEN_HEIGHT = 200
MAX_CANVAS = 2048


def fade_towards_screen_edges(frame, screen_x, screen_y, fade):
    """Fades a frame's alpha to nothing over the last fade["right"] pixels before the
    screen's right edge, and the first fade["top"] / fade["left"] / fade["bottom"] pixels
    inside its other edges. Pixels past an edge (off the original screen) vanish."""
    pixels = frame.load()
    for y in range(frame.height):
        for x in range(frame.width):
            r, g, b, a = pixels[x, y]
            if a == 0:
                continue
            k = 1.0
            if "right" in fade:
                k = min(k, (SCREEN_WIDTH - (screen_x + x) - 0.5) / fade["right"])
            if "top" in fade:
                k = min(k, (screen_y + y + 0.5) / fade["top"])
            if "left" in fade:
                k = min(k, (screen_x + x + 0.5) / fade["left"])
            if "bottom" in fade:
                k = min(k, (SCREEN_HEIGHT - (screen_y + y) - 0.5) / fade["bottom"])
            k = max(0.0, min(1.0, k))
            pixels[x, y] = (r, g, b, int(round(a * k * k)))


def composite_frames(recipe):
    """Frames drawn from one image: frame i is the image at each of positions[i]."""
    image = Image.open(os.path.join(HERE, recipe["composite"])).convert("RGBA")
    w, h = recipe["cell"]
    origin_x, origin_y = recipe["origin"]
    frames = []
    for placements in recipe["positions"]:
        frame = Image.new("RGBA", (w, h), (0, 0, 0, 0))
        for x, y in placements:
            frame.alpha_composite(image, (x - origin_x, y - origin_y))
        frames.append(frame)
    return frames


def build_strip(recipe):
    w, h = recipe["cell"]
    if "composite" in recipe:
        frames = composite_frames(recipe)
        columns = max(1, MAX_CANVAS // (w + 2))
        rows = (len(frames) + columns - 1) // columns
        canvas = Image.new("RGBA", (next_pot(min(len(frames), columns) * (w + 2)), next_pot(rows * (h + 2))), (0, 0, 0, 0))
        for i, frame in enumerate(frames):
            canvas.alpha_composite(frame, (1 + (i % columns) * (w + 2), 1 + (i // columns) * (h + 2)))
        save(canvas, recipe, len(frames))
        return

    if "positions" in recipe:
        origin_x, origin_y = recipe["origin"]
        offsets = [(x - origin_x, y - origin_y) for x, y in recipe["positions"]]
    else:
        origin_x, origin_y = 0, 0
        offsets = recipe["offsets"]
    source = os.path.join(HERE, recipe["source"])
    frames = sorted(f for f in os.listdir(source) if f.lower().endswith(".png"))
    if len(frames) != len(offsets):
        raise SystemExit("%d frames but %d offsets" % (len(frames), len(offsets)))

    columns = max(1, MAX_CANVAS // (w + 2))
    rows = (len(frames) + columns - 1) // columns
    canvas = Image.new("RGBA", (next_pot(min(len(frames), columns) * (w + 2)), next_pot(rows * (h + 2))), (0, 0, 0, 0))
    for i, (name, (ox, oy)) in enumerate(zip(frames, offsets)):
        frame = Image.open(os.path.join(source, name)).convert("RGBA")
        if ox < 0 or oy < 0 or ox + frame.width > w or oy + frame.height > h:
            raise SystemExit("%s at %s overflows the %dx%d cell" % (name, (ox, oy), w, h))
        if "fade" in recipe:
            fade_towards_screen_edges(frame, origin_x + ox, origin_y + oy, recipe["fade"])
        canvas.alpha_composite(frame, (1 + (i % columns) * (w + 2) + ox, 1 + (i // columns) * (h + 2) + oy))

    save(canvas, recipe, len(frames))


def save(canvas, recipe, count):
    output = os.path.join(UNITY_RESOURCES, recipe["output"])
    os.makedirs(os.path.dirname(output), exist_ok=True)
    canvas.save(output)
    print("wrote %s (%dx%d, %d cells)" % (os.path.normpath(output), canvas.width, canvas.height, count))


if __name__ == "__main__":
    for arg in sys.argv[1:] or [str(k) for k in RECIPES]:
        build(int(arg))
