# EventChapterN.m → ChapterN.cs

Every row below was read off the pair `Classes/EventChapter2.m` →
`Assets/Scripts/Chapters/Chapter2.cs`, which is a complete, checked port. When
something is not here, the original helper is defined in
`Classes/EventLoader.m` (the `[self ...]` calls) or `Classes/ActionLayers.m`
(the `[layers ...]` ones).

## Registering events — `loadEvents`

| Objective-C | C# |
|---|---|
| `[self loadTurnEvent:T Turn:N Action:@selector(m)]` | `LoadTurnEvent(++eventId, <shifted>, <faction>, m)` — see below |
| `[self loadDieEvent:id Action:@selector(m)]` | `LoadDeadEvent(++eventId, id, m)` |
| `[self loadDyingEvent:id Action:@selector(m)]` | `LoadDyingEvent(++eventId, id, m)` |
| `[self loadTeamEvent:CreatureType_Enemy Action:@selector(m)]` | `LoadTeamEvent(++eventId, CreatureFaction.Enemy, m)` |
| `[self loadTeamEvent:CreatureType_Npc Action:@selector(m)]` | `LoadTeamEvent(++eventId, CreatureFaction.Npc, m)` |
| `[self loadPositionEvent:id AtPosition:p Action:@selector(m)]` | `ReachPositionEvent` — no `ChapterEvents` wrapper yet, add one |

`eventId` is a plain counter in the constructor; the original allocated it the
same way (`generatedEventId++`).

### The turn-event shift — get this wrong and every cutscene is one phase late

The original's `TurnCondition` only matched **after every creature of that
faction had acted**, so its turn events fired at the *end* of a phase. The C#
`FDTurnEvent` fires at the *start* of one. Each boundary therefore moves:

| Objective-C | C# |
|---|---|
| `TurnType_Friend Turn:0` | `LoadTurnEvent(id, 1, CreatureFaction.Friend, ...)` — the opening cutscene |
| `TurnType_Friend Turn:N` | `LoadTurnEvent(id, N, CreatureFaction.Npc, ...)` |
| `TurnType_NPC Turn:N` | `LoadTurnEvent(id, N, CreatureFaction.Enemy, ...)` |
| `TurnType_Enemy Turn:N` | `LoadTurnEvent(id, N + 1, CreatureFaction.Friend, ...)` |

`Turn:0` is the special "before the battle starts" slot, not a round — the
original raised `turnNo` to 1 before the first friend phase, so both numbering
schemes are 1-based and the turn number itself never shifts.

This is also written out in the doc comment on `ChapterEvents.LoadTurnEvent`.

## Putting creatures on the map

| Objective-C | C# |
|---|---|
| `[self settleFriend:i At:CGPointMake(x, y)]` | `AddCreatureToMap(gameMain, CreatureFaction.Friend, i, i, FDPosition.At(x, y))` |
| `[field addNpc:[[FDNpc alloc] initWithDefinition:d Id:i] Position:p]` | `AddCreatureToMap(gameMain, CreatureFaction.Npc, i, d, FDPosition.At(x, y))` |
| `[field addEnemy:[[FDEnemy alloc] initWithDefinition:d Id:i] Position:p]` | `AddCreatureToMap(gameMain, CreatureFaction.Enemy, i, d, FDPosition.At(x, y))` |
| `... initWithDefinition:d Id:i DropItem:t` | same, with `t` as the `dropItemId` argument |
| `[(FDEnemy*)[field getCreatureById:i] setDropItem:t]` | fold `t` into that creature's `AddCreatureToMap` call |
| `[field addEnemy:... Around:p]` | `AddCreatureAroundToMap(gameMain, ..., FDPosition.At(x, y))` |
| `[self removeCreature:i]` / `[layers ...]` | `gameMain.gameMap.RemoveCreature(i)` |

`settleFriend:i` takes the *friend slot*, which for an unmodified party is the
creature id, and the definition id equals the creature id for party members. It
pulls the creature out of the unsettled list — the C# `AddCreatureToMap` does
the equivalent by restoring it from `PartyRecord` when the party carries it, and
returns null for a party member who is still waiting to be revived.

### `Around:` vs `Position:` — they are different calls, keep them different

`Position:` places exactly, stacking if the tile is taken; that is normal for a
group that spawns together and immediately walks apart (chapter 2 drops six
enemies on `(10, 21)`). `Around:` was `BattleField.addEnemy:Around:`: it tries the
tile itself and then the eight around it, clockwise from the east, and takes the
first with no creature on it. Reinforcements that appear mid-battle with no walk
use it, because the tile they are aimed at may well have a party member standing
on it by then.

`ChapterEvents.AddCreatureAroundToMap` is the port of that search, offsets and
order included. Like the original it tests occupancy only, **not** walkability —
so still check the literal coordinate against the map — and it returns null when
all nine tiles are taken, which is the original's silent skip.

Do not flatten an `Around:` into a plain `AddCreatureToMap`. It reads like a
simplification and is a behaviour change: it drops an enemy on top of whoever is
standing there, and `FDMap.GetCreatureAt` then resolves that tile to whichever of
the two is earlier in the list.

## Movement

`[layers moveCreatureId:i To:CGPointMake(x, y) showMenu:FALSE]` becomes

```csharp
ActivityFactory.CreatureWalkActivity(i, FDMovePath.Create(from, ..., to))
```

Two differences to handle:

- **The original pathfinds; the C# takes an explicit path.** `FDMovePath.Create`
  takes 1–3 `FDPosition` vertices (`Push` adds more), and they are corner
  waypoints, not every tile. The first vertex is where the creature is standing
  now. Author an L-shaped path that stays on walkable tiles — read the map's
  `ShapeMatrix` + `Shapes[id].Type` (`1` is `Blocked`) rather than guessing.
- **Branch activities are a `ParallelActivity`.** The original writes one main
  move, then a run of

  ```objc
  [layers appendNewActivity:[[FDEmptyActivity alloc] init]];
  [layers moveCreatureId:N To:... showMenu:FALSE];
  ```

  Each `appendNewActivity:` opens a *parallel* branch beside the main line. All
  of them, main move included, collapse into one C# `ParallelActivity`:

  ```csharp
  gameMain.PushActivity(new ParallelActivity(new ActivityBase[] {
      ActivityFactory.CreatureWalkActivity(1, ...),
      ActivityFactory.CreatureWalkActivity(2, ...),
  }));
  ```

  Everything the original queued on the *main* line after the move — the talk,
  the next `round1_N` — runs after the whole `ParallelActivity`, which is what
  the original's join did too.

## Conversations

```objc
[self showTalkMessage:CH conversation:CC sequence:SS];
```
```csharp
PushConversationsActivities(gameMain, CH, CC, SS, SS);
```

A loop `for (int i = a; i <= b; i++) [self showTalkMessage:CH conversation:CC sequence:i];`
becomes a single `PushConversationsActivities(gameMain, CH, CC, a, b)`.

**The C# parameter is named `sequenceId` but it holds the original's
`conversation:` value**, and `start`/`end` hold the `sequence:` range. The names
do not line up; the values do.

The speaker is not passed — `PushConversationsActivities` looks it up in
`Chapter_NN_ConversationId.txt`, which is what `[FDLocalString chapterCreature:]`
did. And `showTalkMessage` slid the cursor to the speaker before talking; the C#
`TalkActivity` does that itself, so do not add a cursor slide for it.

### Sequence and conversation ids must stay under 100

The C# key is built with `StringUtils.Digit2`, which renders anything over 99 as
`"??"`. Chapter 2's original `sequence:101` variant ships as `99`, renumbered in
the `.strings`, the `ConversationId.txt` and the `.cs` together. See
`chapter-conversations`.

## Cursor and pacing

| Objective-C | C# |
|---|---|
| `[field setCursorTo:CGPointMake(x, y)]` | `gameMain.PushActivity(new SlideCursorActivity(x, y))` |
| `appendToCurrentActivityMethod:@selector(setCursorObjTo:) Param1:[FDPosition positionX:x Y:y]` | same |
| `[[FDDurationActivity alloc] initWithDuration:d]` | drop it — the C# activities pace themselves |
| `appendToCurrentActivityMethod:@selector(round1_2)` | drop it — inline `round1_2`'s body where the call was |

A bare C# lambda queued as an activity is the general form of "run this between
two animations":

```csharp
gameMain.PushActivity((gameMain) => { /* spawn, retarget AI, ... */ });
```

Anything that must happen *after* an animation and *before* the next one has to
go inside such a lambda — code written straight into the `Action<GameMain>` body
runs immediately, while the queue is still being built. Spawns that should
appear after a walk are the usual case.

## AI

| Objective-C | C# |
|---|---|
| `[self setAiOfId:i EscapeTo:p]` | `SetCreatureAiEscape(gameMain, i, FDPosition.At(x, y))` |
| `[self setAiOfId:i getTreasure:t EscapeTo:e]` | `SetCreatureAiTreasure(gameMain, i, t, e)` |
| `[self setAiOfId:i withType:AIType_X]` | `SetCreatureAiType(gameMain, i, AITypes.AIType_X)` |

A creature the original never gives an AI type to takes the default:
`ChapterEvents.GetDefaultAiType` makes healers (occupation 154/155) defensive
and everybody else aggressive.

## Ending the chapter

| Objective-C | C# |
|---|---|
| `[layers gameOver]` / `[self gameOver]` | `gameMain.OnGameOver()` |
| `[layers gameWin]` | `gameMain.OnGameWin()` |
| `[layers gameCleared]` | drop it — it only stopped the music, which `OnGameWin` handles |
| `[self addItemToTeam:id]` | no shared helper yet; `Chapter2.cs` has a private `AddItemToTeam` worth copying |
| `[field getDeadCreatureById:i] != nil` | `gameMain.gameMap.Map.DeadCreatures.Exists(c => c.Id == i)` |
| `[[field getNpcList] count]` | `gameMain.gameMap.Map.Npcs.Count` |

`OnGameWin` queues itself behind whatever is already in the activity queue, so
call it last in the handler and the closing conversation still plays first.

### `adjustFriends` → `AdjustFriendsAfterWon`

The original chapters end with an `adjustFriends` step chained after the closing
dialog: it fixes up who is in the party before `gameWin`. The C# equivalent is
the `AdjustFriendsAfterWon` override, which `GameMain.EnterVillage` calls just
before it builds the record — same position in the sequence, so port the body
there rather than into the `enemyClear` handler.

What it is for: **only Friend-faction creatures standing on the map are carried
to the next chapter.** `GameRecordManager.CreateFromMapRecord` keeps
`map.Creatures` filtered to Friend plus the Friend entries in `DeadCreatures`,
and drops NPCs and enemies.

So a guest who joins at the end of a battle has to *end it as a Friend*:

- the original writes `[[field getFriendList] addObject:tienuo]`
- the C# is `gameMain.gameMap.RemoveCreature(id)` for the NPC, then
  `AddCreatureToMap(gameMain, CreatureFaction.Friend, id, definitionId, position)`

The swap is necessary, not cosmetic: an NPC is an `FDAICreature` and `Faction`
has a private setter, so there is no way to flip one in place. Grab the NPC's
`Position` before removing it.

And the mirror case: `Chapter1.AdjustFriendsAfterWon` calls
`RemoveCreature(6)` to drop a guest who was only on loan for that battle.

## The one thing to add that is not in the original

End the opening cutscene with

```csharp
gameMain.PushActivity((gameMain) => { gameMain.PlayBackgroundMusic(); });
```

The original started the battle music elsewhere in its scene setup, so no chapter
script calls for it. In the C# port the chapter script is what kicks it off, and
both `Chapter1.cs` and `Chapter2.cs` close their `turn1` this way. Leave it out
and the battle is silent.

## What has no equivalent, and is fine to drop

`CCLOG` / `NSLog`, `retain` / `release` / `autorelease`, `FDEmptyActivity` on its
own (it only opened a parallel branch), and the commented-out `composeChapterRecord`
blocks — the C# save path handles that in `GameMain.EnterVillage`.
