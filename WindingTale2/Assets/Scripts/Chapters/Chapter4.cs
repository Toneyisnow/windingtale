using System;
using System.Collections.Generic;
using WindingTale.Core.Algorithms;
using WindingTale.Core.Common;
using WindingTale.Core.Definitions;
using WindingTale.Core.Events;
using WindingTale.Core.Map;
using WindingTale.Core.Objects;
using WindingTale.Scenes.GameFieldScene;
using WindingTale.Scenes.GameFieldScene.Activities;

namespace WindingTale.Chapters
{
    /// <summary>
    /// Chapter 4 -- the woods outside Sera village.
    ///
    /// Tienuo leads the party down a short cut he swears nobody else knows about, and it
    /// turns out to be a bandit camp. Their captain is convinced the party has come to
    /// raid him and attacks. On turn 4 a pack of beasts wanders in out of the forest,
    /// drawn by the noise, and joins in on whichever side is losing -- but the first one
    /// to fall takes the fight out of the rest, and they run for the edges of the map.
    ///
    /// Unlike chapters 1 to 3 this one is unusual in the events it needs: the beasts'
    /// escape events watch the very tiles they arrive on, so each is held back behind the
    /// matching death event (FDEvent.AddDependentEvent) exactly as the original held them
    /// back with "setEvent:dependentOn:". Handlers are instance methods rather than the
    /// Action fields chapters 2 and 3 use, because they have to reach those event handles.
    /// </summary>
    public class Chapter4 : ChapterEvents
    {
        /// <summary>The tile the party marches in on, and where Sol's line-up forms up.</summary>
        private static readonly FDPosition PartyEntry = FDPosition.At(11, 20);

        /// <summary>The bandits blocking the path, as (id, definition, x, y, drop item).</summary>
        private static readonly int[,] Bandits = new int[,]
        {
            { 21, 50404,  8, 6,   0 },
            { 22, 50404, 10, 6, 801 },
            { 23, 50405, 11, 5, 101 },
            { 24, 50405, 14, 6,   0 },
            { 25, 50401,  7, 5,   0 },
            { 26, 50401,  9, 5, 105 },
            { 27, 50401, 13, 5,   0 },
            { 28, 50401,  8, 4,   0 },
            { 29, 50401, 12, 4, 102 },
            { 30, 50401, 14, 4,   0 },
            { 31, 50401,  6, 3, 201 },
            { 32, 50401,  9, 3,   0 },
            { 33, 50401, 11, 3,   0 },
            { 34, 50401, 13, 3, 101 },
            { 35, 50401, 15, 3,   0 },
            { 36, 50401, 10, 2,   0 },
            { 40, 50402, 10, 4,   0 },
        };

        /// <summary>The definition every one of the four beasts is built from.</summary>
        private const int BeastDefinitionId = 50403;

        /// <summary>
        /// The beasts, as (id, x, y). Each comes out of the trees at its own corner of the
        /// map and runs back to the same tile once the pack loses its nerve, which is why
        /// one list serves as both the spawn points and the escape targets.
        /// </summary>
        private static readonly int[,] Beasts = new int[,]
        {
            { 81,  1,  9 },
            { 82, 20,  8 },
            { 83,  1, 19 },
            { 84, 11, 20 },
        };

        /// <summary>
        /// The death events for the four beasts, kept so the pack's nerve breaks exactly
        /// once: the first death deactivates all four, which both silences the other three
        /// messages and releases the escape events waiting on them.
        /// </summary>
        private readonly List<FDEvent> beastDeathEvents = new List<FDEvent>();

        public Chapter4(GameMain gameMain) : base(gameMain, 4)
        {
            int eventId = 0;
            // The original was "TurnType_Friend Turn:0" -- the opening cutscene -- and
            // "TurnType_Friend Turn:4", which fired once the last friend had acted in turn
            // 4, i.e. that turn's Npc phase here. See LoadTurnEvent.
            LoadTurnEvent(++eventId, 1, CreatureFaction.Friend, turn1);
            LoadTurnEvent(++eventId, 4, CreatureFaction.Npc, turn4);

            LoadDeadEvent(++eventId, 1, (gameMain) => gameMain.OnGameOver());

            for (int i = 0; i < Beasts.GetLength(0); i++)
            {
                beastDeathEvents.Add(LoadDeadEvent(++eventId, Beasts[i, 0], BeastDead));
            }

            // Each beast leaves the battle when it gets back to the tile it came in on.
            // It is standing on that tile the moment it arrives, so the event only becomes
            // live once its own death event is done with -- i.e. once the pack has broken.
            for (int i = 0; i < Beasts.GetLength(0); i++)
            {
                int beastId = Beasts[i, 0];
                FDEvent escaped = LoadReachPositionEvent(++eventId, beastId,
                    FDPosition.At(Beasts[i, 1], Beasts[i, 2]),
                    (gameMain) => gameMain.gameMap.RemoveCreature(beastId));
                escaped.AddDependentEvent(beastDeathEvents[i]);
            }

            LoadTeamEvent(++eventId, CreatureFaction.Enemy, enemyClear);
        }

        private Action<GameMain> turn1 = (gameMain) =>
        {
            // The party comes up the path in single file and spreads out at the top of it.
            for (int creatureId = 1; creatureId <= 7; creatureId++)
            {
                AddCreatureToMap(gameMain, CreatureFaction.Friend, creatureId, creatureId, PartyEntry);
            }

            // The bandits are already waiting, drawn up across the north of the clearing.
            for (int i = 0; i < Bandits.GetLength(0); i++)
            {
                AddCreatureToMap(gameMain, CreatureFaction.Enemy, Bandits[i, 0], Bandits[i, 1],
                    FDPosition.At(Bandits[i, 2], Bandits[i, 3]), Bandits[i, 4]);
            }

            gameMain.PushActivity(new ParallelActivity(PartyWalkIn()));

            // Sol has walked out in front; frame him for the argument that follows.
            gameMain.PushActivity(new SlideCursorActivity(11, 18));

            // Talking
            PushConversationsActivities(gameMain, 4, 1, 1, 9);

            gameMain.PushActivity((gameMain) =>
            {
                // Play background music
                gameMain.PlayBackgroundMusic();
            });
        };

        private Action<GameMain> turn4 = (gameMain) =>
        {
            // The noise has brought a pack out of the forest. Three come in from the edges
            // of the map; the fourth arrives behind the party, so it takes the first free
            // tile it can find rather than landing on top of somebody.
            AddCreatureToMap(gameMain, CreatureFaction.Enemy, 81, BeastDefinitionId, FDPosition.At(1, 9));
            AddCreatureToMap(gameMain, CreatureFaction.Enemy, 82, BeastDefinitionId, FDPosition.At(20, 8));
            AddCreatureToMap(gameMain, CreatureFaction.Enemy, 83, BeastDefinitionId, FDPosition.At(1, 19));
            AddCreatureAroundToMap(gameMain, CreatureFaction.Enemy, 84, BeastDefinitionId, PartyEntry);

            // Talking
            PushConversationsActivities(gameMain, 4, 2, 1, 4);
        };

        /// <summary>
        /// The first beast to fall. The other three decide this is no longer a game and
        /// bolt for the trees they came out of.
        /// </summary>
        private void BeastDead(GameMain gameMain)
        {
            // Talking
            PushConversationsActivities(gameMain, 4, 3, 1, 1);

            // Only the first death gets a line, and only the first one breaks the pack.
            // Standing the other three down is also what arms their escape events.
            foreach (FDEvent deathEvent in beastDeathEvents)
            {
                deathEvent.SetActive(false);
            }

            for (int i = 0; i < Beasts.GetLength(0); i++)
            {
                SetCreatureAiEscape(gameMain, Beasts[i, 0], FDPosition.At(Beasts[i, 1], Beasts[i, 2]));
            }
        }

        private Action<GameMain> enemyClear = (gameMain) =>
        {
            // Talking
            PushConversationsActivities(gameMain, 4, 4, 1, 4);

            // The chapter is over once the last enemy falls -- or runs. OnGameWin queues
            // itself behind the conversation above, so the closing lines play out first.
            gameMain.OnGameWin();
        };

        /// <summary>
        /// The party fanning out from the mouth of the path. They all start stacked on the
        /// entry tile, which is where the original settled them, and step onto the three
        /// clear tiles ahead of it -- everything to either side of that little pocket is
        /// blocked, so the moves are one or two tiles each and need no waypoints beyond
        /// the corner.
        ///
        /// Sol (5) is left where he is: the original "moved" him to the tile he was
        /// already standing on.
        /// </summary>
        private static ActivityBase[] PartyWalkIn()
        {
            return new ActivityBase[]
            {
                ActivityFactory.CreatureWalkActivity(1, FDMovePath.Create(PartyEntry, FDPosition.At(11, 18))),
                ActivityFactory.CreatureWalkActivity(2, FDMovePath.Create(PartyEntry, FDPosition.At(11, 19))),
                ActivityFactory.CreatureWalkActivity(3, FDMovePath.Create(PartyEntry, FDPosition.At(10, 20), FDPosition.At(10, 19))),
                ActivityFactory.CreatureWalkActivity(4, FDMovePath.Create(PartyEntry, FDPosition.At(12, 20), FDPosition.At(12, 19))),
                ActivityFactory.CreatureWalkActivity(6, FDMovePath.Create(PartyEntry, FDPosition.At(10, 20))),
                ActivityFactory.CreatureWalkActivity(7, FDMovePath.Create(PartyEntry, FDPosition.At(12, 20))),
            };
        }
    }
}
