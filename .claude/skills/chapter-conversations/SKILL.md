---
name: chapter-conversations
description: Import a chapter's dialog into Unity Localization — turn Resources/Original/Strings/Maps/Chapter-NN.strings into a ChapterStrings-NN table. Use when a chapter's talk dialogs come up empty or say the Conversation-NN-CC-SS key was not found, when asked to import/generate chapter conversation text, or when adding a new chapter's story lines.
---

# Importing a chapter's conversations

A chapter's dialog lives in three places that have to agree. When a talk dialog
comes up blank or complains the key is missing, one of them is out of step.

| what | where | key it uses |
|---|---|---|
| the text | `ChapterStrings-NN` localization table | `Conversation-NN-CC-SS` |
| who says it | `Assets/Resources/Data/Chapters/Chapter_NN_ConversationId.txt` | `Chapter_NN-NN-CC-SSS-Id` |
| when it plays | `Assets/Scripts/Chapters/ChapterN.cs` | `PushConversationsActivities(gameMain, NN, CC, from, to)` |

`CC` is the conversation id, `SS` the sequence. The text key pads both with
`StringUtils.Digit2`, the speaker key pads the sequence with `Digit3` — that
asymmetry is why the two files never look alike.

The master source for all of it is
`Resources/Original/Strings/Maps/Chapter-NN.strings`, ripped from the original
game.

## Generate the CSV

```bash
cd Tools/Localization
python strings_to_csv.py 2
```

Writes `WindingTale2/Assets/Resources/Strings/Localizations/ChapterStrings-NN.csv`
and cross-checks it against `Chapter_NN_ConversationId.txt`, warning about lines
with text but no speaker (they play with the narrator portrait) and speakers
with no text (the chapter script will fail to look those up). Takes several
chapters at once: `python strings_to_csv.py 3 4 5`.

The simplified text is copied into the traditional column too — that is what
chapter 01 does, and there is no traditional source to draw from.

## Import into Unity

1. open `WindingTale2`, let it compile
2. **WindingTale → Localization → Import All Chapter Strings CSV**
   (or right-click a single CSV → **WindingTale → Import Chapter Strings CSV**)
3. **Window → Asset Management → Localization Tables**, pick `ChapterStrings-NN`,
   confirm the rows are there
4. **File → Save Project**

That menu lives in `Assets/Editor/ChapterStringsImporter.cs`. It creates the
table collection when it is missing, which also registers it with Addressables —
hand-writing the `.asset` files skips that registration and the table then loads
as empty at runtime.

Four assets appear per chapter: `ChapterStrings-NN.asset`, `... Shared Data.asset`,
`..._zh-Hans.asset`, `..._zh-Hant.asset`.

## Sequence and conversation ids must stay under 100

`Digit2` renders anything over 99 as `"??"`, so `Conversation-02-03-101` becomes
`Conversation-02-03-??` and is never found. `strings_to_csv.py` refuses to emit
such a key rather than writing a broken one.

Renumber to a free two-digit id instead — 99, 98, ... for the odd ones out — and
change it in all three places at once: the `.strings` file, the
`Chapter_NN_ConversationId.txt` line, and the `ChapterN.cs` call. Chapter 02's
all-villagers-saved variant went `101 → 99` this way.

## The original numbering is not always the shipped numbering

`strings_to_csv.py` converts mechanically, but a chapter's `CC` can be
deliberately remapped — dying lines are usually renumbered to match the npc id
the `LoadDyingEvent` fires on. Chapter 02 needs no remap (its source already
uses 91..96, the villagers' ids). Chapter 01 does: source `01-06-*` ships as
`Conversation-01-91-*`.

So after generating, read the chapter's `PushConversationsActivities` calls and
confirm every `(CC, from..to)` range it asks for exists in the CSV. Where they
disagree, the chapter script wins — rename the keys in the CSV to match it.

Known drift: chapter 01's `Chapter_01_ConversationId.txt` still says `01-90-*`
where its table says `01-91-*`, so those four lines show the narrator instead of
the speaker.

## Boxes instead of characters

Each chapter has its own TMP font asset, `Resources/Fonts/FontAssets/zh/FZB_Chapter-NN`,
packed with **only** the glyphs that chapter's script uses — chapter 01 holds 405,
chapter 02 holds 351, and the two atlases are laid out completely differently.
`TalkDialog.InitConversation` swaps it in per chapter.

That makes boxes a per-chapter failure with two causes:

- **the atlas is missing or stale** — check with
  `python check_font_coverage.py 2` (in `Tools/Localization`), which lists exactly
  which characters the chapter's dialog uses that the atlas lacks. Rebuild the
  atlas from `Resources/Fonts/CharacterList/CharacterList_Chapter-NN.txt` if it
  reports any.
- **only the material got swapped, not the font asset** — TMP looks up
  character → UV in the *font asset's* table. Point the prefab's font asset at
  another chapter's atlas material and every glyph resolves against the wrong
  table: characters the prefab's own chapter never used come out as boxes, the
  rest render as the wrong glyph. Always assign `textMeshComponent.font`, never
  just `fontMaterial`. Setting `font` picks up the matching material by itself.

The second one bit chapter 02: the `ConversationText` object in `TalkDialog.prefab`
is authored with `FZB_Chapter-01`, and the code used to override only the material,
so 136 of chapter 02's 337 characters were boxes even though its atlas was complete.

## Building a chapter's font atlas

**Window → TextMeshPro → Font Asset Creator**, with chapter 01's settings — the
same check script prints them, and warns when a chapter drifts below the
reference:

| field | value |
|---|---|
| Source Font File | `Resources/Fonts/FangZhengBlack.TTF` |
| Sampling Point Size | Auto Sizing |
| Padding | 5 |
| Packing Method | Fast |
| **Atlas Resolution** | **4096 × 4096** |
| Character Set | Characters from File |
| Character File | `Resources/Fonts/CharacterList/CharacterList_Chapter-NN.txt` |
| Render Mode | SDFAA |

The atlas resolution is the one that quietly matters. With Auto Sizing a smaller
texture just yields a smaller sampling point size — chapter 02 was built at
2048² and got point size 101 against chapter 01's 199 at 4096², so its glyphs
were baked at half resolution. Everything else about the two assets, materials
included, was byte-identical.

Half resolution shows up as a **white halo around every character**, for two
reasons at once: the glyphs are magnified ~2× to reach the same on-screen size,
and the SDF padding ramp is a fixed 5 atlas pixels, which is 5% of a 101px glyph
against 2.6% of a 199px one. Always build at 4096².

Note that `FZB_Chapter-02`'s `m_SourceFontFileGUID` points at a font file that is
no longer in the project, so the Font Asset Creator opens with an empty Source
Font File field — set it to `FangZhengBlack` by hand. Its `m_CreationSettings`
still records the right guid, which is where the table above comes from.
