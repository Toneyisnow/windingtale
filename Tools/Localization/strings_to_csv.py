"""Turn Resources/Original/Strings/Maps/Chapter-NN.strings into the
ChapterStrings-NN.csv that Unity Localization's CSV importer reads.

Source line:   "02-01-001" = "text";      (the "-Id" lines are speaker ids,
                                           they live in Chapter_NN_ConversationId.txt)
Output key:    Conversation-02-01-01     (every field goes through Digit2)

The generated CSV is only the import source; the StringTable assets the game
actually loads are produced by the Unity menu
"WindingTale -> Localization -> Import All Chapter Strings CSV".
"""
import argparse
import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
SRC = ROOT / "Resources" / "Original" / "Strings" / "Maps"
OUT = ROOT / "WindingTale2" / "Assets" / "Resources" / "Strings" / "Localizations"
CONV_IDS = ROOT / "WindingTale2" / "Assets" / "Resources" / "Data" / "Chapters"

HEADER = "Key,Id,Chinese (Simplified)(zh-Hans),Chinese (Traditional)(zh-Hant)"
LINE = re.compile(r'^"(\d{2})-(\d{2,3})-(\d{3})"\s*=\s*"(.*)";\s*$')
ID_LINE = re.compile(r"^Chapter_\d{2}-(\d{2})-(\d{2,3})-(\d{3})-Id\s")


class SequenceTooLarge(Exception):
    pass


def digit2(n):
    """Mirror of WindingTale.Core.Common.StringUtils.Digit2.

    Digit2 returns "??" for anything over 99, so an id past that produces a key
    the game can never look up. Rather than emit the broken key, refuse: pick a
    free two-digit id instead (99, 98, ... are conventionally the odd ones out).
    """
    if n > 99:
        raise SequenceTooLarge(
            f"id {n} does not fit in two digits -- Digit2 would render it as '??'. "
            f"Renumber it to a free id under 100 in the .strings file, in "
            f"Chapter_NN_ConversationId.txt and in the Chapter class."
        )
    return "%02d" % n


def csv_escape(v):
    return '"' + v.replace('"', '""') + '"'


def read_source(chapter):
    """(key, text) for every dialog line in the chapter's .strings file."""
    src = SRC / f"Chapter-{chapter:02d}.strings"
    for raw in src.read_text(encoding="utf-8").splitlines():
        m = LINE.match(raw.strip())
        if not m:
            continue
        ch, conv, seq, text = m.groups()
        yield f"Conversation-{digit2(int(ch))}-{digit2(int(conv))}-{digit2(int(seq))}", text


def read_speaker_ids(chapter):
    """The keys Chapter_NN_ConversationId.txt supplies a speaker for.

    Returned in Conversation-* form so they can be compared with the text keys.
    """
    path = CONV_IDS / f"Chapter_{chapter:02d}_ConversationId.txt"
    if not path.exists():
        return None

    keys = set()
    for raw in path.read_text(encoding="utf-8").splitlines():
        m = ID_LINE.match(raw.strip())
        if m:
            ch, conv, seq = m.groups()
            keys.add(f"Conversation-{digit2(int(ch))}-{digit2(int(conv))}-{digit2(int(seq))}")
    return keys


def generate(chapter):
    entries = list(read_source(chapter))

    out = OUT / f"ChapterStrings-{chapter:02d}.csv"
    rows = [f"{key},,{csv_escape(text)},{csv_escape(text)}" for key, text in entries]
    out.write_text("\n".join([HEADER] + rows) + "\n", encoding="utf-8", newline="\n")
    print(f"{out}  ->  {len(rows)} entries")

    # A line with text but no speaker shows up with the narrator portrait, and a
    # speaker with no text is a line the chapter script will fail to look up.
    # Neither is fatal here, but both are worth knowing about before importing.
    speakers = read_speaker_ids(chapter)
    if speakers is None:
        print("  note: no Chapter_%02d_ConversationId.txt, skipping cross-check" % chapter)
        return

    text_keys = {key for key, _ in entries}
    for key in sorted(speakers - text_keys):
        print(f"  warning: {key} has a speaker id but no text")
    for key in sorted(text_keys - speakers):
        print(f"  warning: {key} has text but no speaker id (will use the narrator)")


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("chapters", nargs="+", type=int, help="chapter numbers, e.g. 2 3 4")
    args = parser.parse_args()

    try:
        for chapter in args.chapters:
            generate(chapter)
    except SequenceTooLarge as error:
        sys.exit(f"error: {error}")


if __name__ == "__main__":
    main()
