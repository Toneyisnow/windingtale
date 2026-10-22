"""Install a free-field map (one cut out of a screenshot by screenshot_map.py).

Reads out/map_NN/map.json + out/map_NN/panel (the tiles) and
free_maps/map_NN_scene.json (creatures, events, and which conversations of
which original Chapter-NN.strings the map talks with), and writes:

  WindingTale2/Assets/Resources/Data/Maps/Map_NN.json
      Index, Width, Height, ShapeSet "Map_NN", ShapeMatrix (walkability),
      RenderMatrix (which tile model to draw), Shapes, Obstacles, Hero,
      Creatures, Events -- what GameFreeFieldScene loads.
  WindingTale2/Assets/Resources/Strings/Localizations/ChapterStrings-NN.csv
      those conversations as Conversation-NN-CC-SS (import it in Unity with
      WindingTale > Localization > Import All Chapter Strings CSV).
  WindingTale2/Assets/Resources/Fonts/CharacterList/CharacterList_Chapter-NN.txt
      the glyphs those lines use (bake with WindingTale > Localization >
      Build Missing Chapter Font Atlases).

The two matrices work as in the battle chapters: ShapeMatrix decides where the
hero may walk, RenderMatrix what is drawn. A tile drawn as plain floor can still
be blocked -- the floor under a statue -- so blocked cells point at one extra
shape id, BLOCKED_ID, that is Blocked and never drawn.

    python build_free_map.py 32
"""
import argparse
import json
import os

import numpy as np
from PIL import Image

import free_map_strings

HERE = os.path.dirname(os.path.abspath(__file__))
ASSETS = os.path.normpath(os.path.join(HERE, "..", "..", "WindingTale2", "Assets", "Resources"))

BLOCKED_ID = 1000

# Rows of each obstacle that block, relative to the row it stands on.
OBSTACLE_BLOCK = {
    "stone_statue_1": (1, -1, 0),      # (cols, first row offset, last row offset)
    "stone_pillar_1": (1, -1, 0),
    "stone_column_1": (1, -2, 0),
    "stone_shrine_1": (3, -2, 1),
}
# Every tree (tree_<colour>, pine_<colour>) blocks the one tile it stands on.
TREE_BLOCK = (1, 0, 0)


def classify(png):
    """Blocked when the tile is mostly the white beyond the stairs or the pools."""
    a = np.asarray(Image.open(png).convert("RGB")).astype(int)
    r, g, b = a[..., 0], a[..., 1], a[..., 2]
    white = ((r > 225) & (g > 225) & (b > 225)).mean()
    pool = ((b > 150) & (b > r + 50) & (r < 150)).mean()
    return white > 0.45 or pool > 0.3


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("map_id", type=int)
    args = ap.parse_args()
    nn = "%02d" % args.map_id

    src = os.path.join(HERE, "out", "map_%s" % nn)
    with open(os.path.join(src, "map.json")) as f:
        cut = json.load(f)
    with open(os.path.join(HERE, "free_maps", "map_%s_scene.json" % nn), encoding="utf-8") as f:
        scene = json.load(f)

    w, h = cut["Width"], cut["Height"]
    render = cut["RenderMatrix"]
    used = sorted({render[x][y] for x in range(w) for y in range(h)})

    shapes = {}
    blocked_tile = {}
    for tid in used:
        png = os.path.join(src, "panel", "Shape_%d_%d.png" % (args.map_id - 1, tid))
        blocked_tile[tid] = classify(png)
        shapes[str(tid)] = {"Type": 1 if blocked_tile[tid] else 0, "bg": 0, "tai": 0}
    shapes[str(BLOCKED_ID)] = {"Type": 1, "bg": 0, "tai": 0}

    shape = [[render[x][y] for y in range(h)] for x in range(w)]

    def block(x, y):
        if 1 <= x <= w and 1 <= y <= h:
            shape[x - 1][y - 1] = BLOCKED_ID

    for y in scene.get("BlockedRows", []):
        for x in range(1, w + 1):
            block(x, y)
    for p in scene.get("BlockedExtra", []):
        block(p["X"], p["Y"])
    for o in cut["Obstacles"]:
        key = o["DefinitionKey"]
        cols, top, bottom = TREE_BLOCK if key.startswith(("tree_", "pine_")) else OBSTACLE_BLOCK[key]
        px, py = o["Position"]["X"], o["Position"]["Y"]
        for x in range(px, px + max(cols, 1)):
            for y in range(py + top, py + bottom + 1):
                block(x, y)

    # The talk comes from an original chapter's .strings ("Strings" in the scene file):
    # its conversations are renumbered for the map, and each line's speaker id there is
    # turned into the portrait (animation id) to show -- "Speakers" maps the ids that
    # are not already one (the king, 99, is portrait 901).
    strings = scene["Strings"]
    conversation_map = {int(k): v for k, v in strings["Conversations"].items()}
    speaker_map = {int(k): v for k, v in strings.get("Speakers", {}).items()}
    picked = free_map_strings.pick(free_map_strings.read_source(strings["Source"]), conversation_map)

    talks = {}
    for conv, seq, speaker, text in picked:
        talks.setdefault(conv, []).append((seq, speaker_map.get(speaker, speaker)))

    events = []
    for e in scene["Events"]:
        event_talks = []
        for entry in e["Conversations"]:
            # A whole conversation by id, or part of one: {"Id": 8, "From": 2, "To": 6}.
            conv = entry["Id"] if isinstance(entry, dict) else entry
            if conv not in talks:
                raise SystemExit("event plays conversation %d, which Strings does not bring in" % conv)
            seqs = talks[conv]
            if isinstance(entry, dict):
                seqs = [(sq, sp) for sq, sp in seqs
                        if entry.get("From", 1) <= sq <= entry.get("To", seqs[-1][0])]
            event_talks.append({"ConversationId": conv, "From": seqs[0][0], "To": seqs[-1][0],
                                "Speakers": [sp for _, sp in seqs]})
        positions = e.get("Positions") or []
        for y in e.get("Rows", []):
            # A whole row of the map: every tile X 1..Width on line Y.
            positions = positions + [{"X": x, "Y": y} for x in range(1, w + 1)]
        for x in e.get("Columns", []):
            # A whole column: every tile Y 1..Height on line X.
            positions = positions + [{"X": x, "Y": y} for y in range(1, h + 1)]
        event = {"Trigger": e.get("Trigger", "Face"), "Once": bool(e.get("Once", False)),
                 "Positions": positions, "Talks": event_talks}
        for key in ("Id", "Requires", "Then", "Actions"):
            if key in e:
                event[key] = e[key]
        events.append(event)

    lines = [(c, s, t) for c, s, _, t in picked]

    out = {
        "Index": args.map_id,
        "Width": w,
        "Height": h,
        "ShapeSet": "Map_%s" % nn,
        "Sky": scene.get("Sky", ""),
        "StringsTable": strings.get("Table", args.map_id),
        "Hint": scene.get("Hint", ""),
        "Hero": scene["Hero"],
        "Creatures": scene["Creatures"],
        "Events": events,
        "Obstacles": cut["Obstacles"],
        "Shapes": shapes,
        "ShapeMatrix": shape,
        "RenderMatrix": render,
    }
    maps_dir = os.path.join(ASSETS, "Data", "Maps")
    os.makedirs(maps_dir, exist_ok=True)
    with open(os.path.join(maps_dir, "Map_%s.json" % nn), "w", encoding="utf-8") as f:
        json.dump(out, f, ensure_ascii=False, indent=1)

    # A map may talk out of another map's table ("Table" in Strings): that map's build
    # writes it, so this one leaves it alone.
    if strings.get("Table", args.map_id) == args.map_id:
        free_map_strings.write_table(args.map_id, lines)

    walk = sum(1 for x in range(w) for y in range(h) if shapes[str(shape[x][y])]["Type"] == 0)
    print("map %s: %d tiles, %d walkable cells of %d, %d events, %d lines" % (
        nn, len(used), walk, w * h, len(events), len(lines)))
    print("  blocked tiles:", [t for t in used if blocked_tile[t]])


if __name__ == "__main__":
    main()
