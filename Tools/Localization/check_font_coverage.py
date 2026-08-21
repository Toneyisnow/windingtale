"""Check a chapter's TMP font atlas against the dialog it has to render.

Two things go wrong with these atlases:

- coverage: each FZB_Chapter-NN.asset is packed with only the glyphs that
  chapter's script uses, so a chapter whose text changed after the atlas was
  built renders the new characters as boxes.
- resolution: the atlas is built with Auto Sizing, so a smaller atlas texture
  silently yields a smaller sampling point size. The glyphs are then magnified
  to reach the same on-screen size, and the SDF padding ramp -- a fixed 5 atlas
  pixels -- is magnified with them into a visible halo. Chapter 01 is the
  reference look: 4096x4096, which lands around point size 199.

    python check_font_coverage.py 2
"""
import argparse
import csv
import io
import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
CSV_DIR = ROOT / "WindingTale2" / "Assets" / "Resources" / "Strings" / "Localizations"
FONT_DIR = ROOT / "WindingTale2" / "Assets" / "Resources" / "Fonts" / "FontAssets" / "zh"

# Chapter 01 shipped first and defines how the dialog text is meant to look.
REFERENCE_CHAPTER = 1


def read_font_asset(chapter):
    """The chapter's font asset as YAML text, or None when it has not been built.

    These run to tens of megabytes -- the baked atlas is inlined as one enormous
    line -- so read each one only once and hand the text to the parsers below.
    """
    path = FONT_DIR / f"FZB_Chapter-{chapter:02d}.asset"
    if not path.exists():
        return None
    return io.open(path, encoding="utf-8", errors="replace").read()


def font_glyphs(asset):
    """The unicode code points present in the chapter's atlas."""
    return {int(m) for m in re.findall(r"m_Unicode:\s*(\d+)", asset)}


def creation_settings(asset):
    """The atlas size and sampling point size the font asset was baked with."""
    return {
        name: int(re.search(rf"\n\s*{name}: (\d+)", asset).group(1))
        for name in ("pointSize", "atlasWidth", "padding")
    }


def text_characters(chapter):
    """Every character used by the chapter's localized dialog."""
    path = CSV_DIR / f"ChapterStrings-{chapter:02d}.csv"
    used = set()
    with io.open(path, encoding="utf-8", newline="") as handle:
        reader = csv.reader(handle)
        next(reader)
        for row in reader:
            # Hand-edited CSVs carry trailing blank lines.
            if len(row) > 2:
                used |= set(row[2])
    return used


def check(chapter, reference):
    asset = read_font_asset(chapter)
    if asset is None:
        print(f"chapter {chapter:02d}: no FZB_Chapter-{chapter:02d}.asset -- the atlas has to be built first")
        return False

    glyphs = font_glyphs(asset)
    used = text_characters(chapter)
    missing = sorted(c for c in used if ord(c) not in glyphs)

    print(f"chapter {chapter:02d}: {len(used)} distinct characters, atlas holds {len(glyphs)} glyphs")

    settings = creation_settings(asset)
    print("  atlas {atlasWidth}px, sampling point size {pointSize}, padding {padding}".format(**settings))

    ok = True
    if missing:
        print(f"  {len(missing)} missing -- these render as boxes:")
        print("  " + "".join(missing))
        ok = False

    # Below the reference point size the padding ramp is proportionally wider and
    # the glyphs get magnified, which shows up as a white halo around every character.
    if settings["pointSize"] < reference["pointSize"]:
        print(
            "  point size is under chapter {0:02d}'s {1} -- rebuild at {2}x{2} "
            "to match its look".format(REFERENCE_CHAPTER, reference["pointSize"], reference["atlasWidth"])
        )
        ok = False

    if ok:
        print("  all covered, settings match the reference")
    return ok


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("chapters", nargs="+", type=int)
    args = parser.parse_args()

    reference = creation_settings(read_font_asset(REFERENCE_CHAPTER))

    if not all([check(c, reference) for c in args.chapters]):
        sys.exit(1)


if __name__ == "__main__":
    main()
