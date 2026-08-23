---
name: chapter-events
description: Port a chapter's battle script from the original Objective-C EventChapterN.m into Assets/Scripts/Chapters/ChapterN.cs — turn events, spawns, walk-ins, conversations, win/lose conditions. Use when asked to write or generate a chapter's events, port EventChapterNN.m, add ChapterNN.cs, or make chapter NN playable.
---

# Porting a chapter's events

Every chapter's battle script already exists, written in Objective-C for the
2012 iOS game. **Do not invent a staging.** The positions, the turn numbers, the
enemy roster, who drops what — all of it is in

```
D:\SourceCode\Git\toneyisnow\FlameDragon-master\Classes\EventChapterNN.m
```

(no zero padding: `EventChapter3.m`, `EventChapter12.m`). The job is a
translation, not a design. `EventChapter2.m` → `Chapter2.cs` is the worked
reference; read both side by side before starting.

## Before starting

Read `references/objc_to_csharp.md`. It is the full call-by-call mapping table
and the two traps that silently produce a wrong chapter (the turn-event shift
and the `PushConversationsActivities` parameter names).

The original base class `Classes/EventLoader.m` defines every `[self ...]`
helper the chapter script calls; `Classes/ActionLayers.m` defines every
`[layers ...]` one. Open them when a call is not in the table.

## Inputs

| what | where |
|---|---|
| the script to port | `FlameDragon-master/Classes/EventChapterN.m` |
| dialog text | `Resources/Original/Strings/Maps/Chapter-NN.strings` |
| who speaks each line | `Assets/Resources/Data/Chapters/Chapter_NN_ConversationId.txt` |
| chapter creature defs | `Assets/Resources/Data/Chapters/Chapter_NN_Creature.txt` |
| the map | `Assets/Resources/Data/Chapters/Chapter_NN.json` |
| the C# reference | `Assets/Scripts/Chapters/Chapter2.cs` |

## Outputs

- `Assets/Scripts/Chapters/ChapterN.cs`
- a `case N:` in `ChapterLoader.CreateChapter`
- `BackgroundMusic` and `DefaultTais` in `Chapter_NN.json`, if it has none

## Steps

### 1. Read the original and write down what it does

Work through `EventChapterN.m` top to bottom and produce a plain list before
writing any C#: every event registered in `loadEvents`, and for each handler
every creature added (id, definition, position, drop item), every move, every
cursor slide, every conversation range, and every branch.

The handler chain matters. The original splits one turn's cutscene across
`round1_1` → `round1_2` → `round1_3`, chained by
`appendToCurrentActivityMethod:@selector(round1_2)`. That is a *sequence*, not
separate events: it all belongs in one C# `Action<GameMain>`, in order. Chapter
2's four `round1_*` methods are one `turn1` in `Chapter2.cs`.

### 2. Check the story against the strings

Open `Chapter-NN.strings` and read the dialog the conversation ranges point at.
It is the fastest way to catch a misread range, and `Condition-Win` /
`Condition-Lose` tell you what the chapter is supposed to end on.

**`Condition-Win` is flavour text, not the rule.** Chapter 3's says
"精英战士死亡" but its `loadEvents` registers
`loadTeamEvent:CreatureType_Enemy` — the battle ends when the last enemy falls,
same as every other chapter. Port the `loadEvents` block, not the string.

Cross-check the speakers too: `Chapter_NN_ConversationId.txt` gives a creature
id per line, and every one of those ids must be a creature the script actually
puts on the map (or one the party walks in with). A speaker id nothing spawns
talks with a blank portrait.

### 3. Write ChapterN.cs

Copy the shape of `Chapter2.cs`: a constructor that registers the events with a
running `++eventId`, then one `private Action<GameMain>` field per handler.

Register events in the same order as the original's `loadEvents` — the event id
is just a counter, but keeping the order keeps the diff readable against the
`.m`.

### 4. Register the chapter

Add to `ChapterLoader.CreateChapter`:

```csharp
case 3:
    chapter = new Chapter3(gameMain);
    break;
```

Forgetting this throws "Cannot find definition for chapter N" at battle start —
the map loads, the script never runs.

### 5. Fill in the chapter's JSON, if needed

Only chapters that have been ported carry `BackgroundMusic` and `DefaultTais`;
the rest of `Chapter_NN.json` is generated. Add them next to `Treasures`:

```jsonc
"BackgroundMusic": { "Field": "...", "Enemy": "Battle_Enemy_Turn_1", "Village": "..." },
"DefaultTais": [1, 2, 4, 5, 6, 7, 8, 9, 11, 12, 13, 14, 15],
```

`Field` is the battle track, `Enemy` plays through the enemy phase.

**`Village` is the *next* village's track, not this chapter's.** `VillageScene`
looks it up under `Record.ChapterId`, and winning chapter N sets that to N+1 —
so `Chapter_03.BackgroundMusic.Village` is the music of the village entered
after winning **chapter 2**. Chapters under 10 all share village picture 1, so
they should share its track.

`DefaultTais` is the pool of fight backdrops. Chapters 1 and 2 both use the list
above; there is no per-chapter source for it, so reuse it unless the chapter is
somewhere the outdoor backdrops make no sense.

### 6. Check it compiles

Follow the `unity-compile-check` memory — a reference-set mistake fakes 50
errors that have nothing to do with the change.

## Reporting

Say plainly, at the end:

- which conversation sequences the chapter plays, and whether every one exists
  in the strings file
- every place the port deviates from the `.m` and why (`Around:` positions, walk
  paths you had to author, a cursor target that looks wrong in the original)
- the manual Unity steps still outstanding — the conversations table import and
  the chapter's font atlas both need the editor, see `chapter-conversations`
