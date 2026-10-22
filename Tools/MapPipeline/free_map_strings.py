"""The dialog of a free-field map, taken out of an original Chapter-NN.strings.

A free-field map has no .strings file of its own: its talk is part of some
chapter's script (Map 32's audience with the king is conversations 1-2 of
Resources/Original/Strings/Maps/Chapter-00.strings, Map 33's is 3-11). This
picks the wanted conversations out, renumbers them for the map, and writes

  WindingTale2/Assets/Resources/Strings/Localizations/ChapterStrings-MM.csv
      Conversation-MM-CC-SS, the layout strings_to_csv.py writes (import it in
      Unity: WindingTale > Localization > Import All Chapter Strings CSV)
  WindingTale2/Assets/Resources/Fonts/CharacterList/CharacterList_Chapter-MM.txt
      the glyphs those lines use (WindingTale > Localization > Build Missing
      Chapter Font Atlases bakes FZB_Chapter-MM from it)

build_free_map.py uses the same functions, and also reads each line's speaker
("-Id") from the source to pick its portrait.

    python free_map_strings.py 33 --source 0 --conversations 3-11 --first 1
"""
import argparse
import os
import re

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.normpath(os.path.join(HERE, "..", ".."))
SRC = os.path.join(ROOT, "Resources", "Original", "Strings", "Maps")
ASSETS = os.path.join(ROOT, "WindingTale2", "Assets", "Resources")

LINE = re.compile(r'^"(\d{2})-(\d{2,3})-(\d{3})(-Id)?"\s*=\s*"(.*)";\s*$')


def read_source(chapter):
    """{conversation: [(sequence, speaker id, text), ...]} of Chapter-NN.strings."""
    path = os.path.join(SRC, "Chapter-%02d.strings" % chapter)
    texts, speakers = {}, {}
    with open(path, encoding="utf-8") as f:
        for raw in f:
            m = LINE.match(raw.strip())
            if not m:
                continue
            _, conv, seq, is_id, value = m.groups()
            key = (int(conv), int(seq))
            if is_id:
                speakers[key] = int(value)
            else:
                texts[key] = value

    out = {}
    for (conv, seq) in sorted(texts):
        out.setdefault(conv, []).append((seq, speakers.get((conv, seq), 0), texts[(conv, seq)]))
    return out


def write_table(map_id, lines):
    """lines: [(conversation, sequence, text)] already numbered for the map."""
    for conv, seq, _ in lines:
        if conv > 99 or seq > 99:
            raise SystemExit("Conversation-%02d-%d-%d does not fit Digit2" % (map_id, conv, seq))

    csv_path = os.path.join(ASSETS, "Strings", "Localizations", "ChapterStrings-%02d.csv" % map_id)
    with open(csv_path, "w", encoding="utf-8", newline="") as f:
        f.write("Key,Id,Chinese (Simplified)(zh-Hans),Chinese (Traditional)(zh-Hant)\n")
        for conv, seq, text in lines:
            q = '"%s"' % text.replace('"', '""')
            f.write("Conversation-%02d-%02d-%02d,,%s,%s\n" % (map_id, conv, seq, q, q))

    chars_path = os.path.join(ASSETS, "Fonts", "CharacterList", "CharacterList_Chapter-%02d.txt" % map_id)
    with open(chars_path, "w", encoding="utf-8") as f:
        f.write("\n".join(text for _, _, text in lines) + "\n")

    return csv_path, chars_path


def pick(source, conversation_map):
    """[(map conversation, sequence, speaker id, text)] for {source conv: map conv}."""
    out = []
    for src_conv, map_conv in sorted(conversation_map.items(), key=lambda kv: kv[1]):
        if src_conv not in source:
            raise SystemExit("conversation %d is not in the source" % src_conv)
        for seq, speaker, text in source[src_conv]:
            out.append((map_conv, seq, speaker, text))
    return out


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("map_id", type=int)
    ap.add_argument("--source", type=int, required=True, help="the Chapter-NN.strings to read")
    ap.add_argument("--conversations", required=True, help="source conversations, e.g. 3-11")
    ap.add_argument("--first", type=int, default=1, help="map conversation id of the first one")
    args = ap.parse_args()

    lo, _, hi = args.conversations.partition("-")
    lo, hi = int(lo), int(hi or lo)
    conversation_map = {c: args.first + (c - lo) for c in range(lo, hi + 1)}

    picked = pick(read_source(args.source), conversation_map)
    csv_path, chars_path = write_table(args.map_id, [(c, s, t) for c, s, _, t in picked])
    print("map %02d: %d lines from Chapter-%02d.strings (%s)" % (
        args.map_id, len(picked), args.source,
        ", ".join("%d->%d" % kv for kv in sorted(conversation_map.items()))))
    print("  " + csv_path)
    print("  " + chars_path)


if __name__ == "__main__":
    main()
