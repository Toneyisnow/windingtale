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
    public class Chapter2 : ChapterEvents
    {
        /// <summary>
        /// The six villagers the party escorts. Saving every one of them is what unlocks
        /// the alternative closing conversation and the bonus item.
        /// </summary>
        private static readonly int[] NpcIds = new int[] { 91, 92, 93, 94, 95, 96 };

        /// <summary>The bonus handed to the team when no villager was lost.</summary>
        private const int AllNpcSavedItemId = 113;

        /// <summary>
        /// Where the villagers run to. They head for the north pass to begin with; once the
        /// enemy reinforcements come down through it (turn 3) they turn around and make for
        /// the east road the party came in by.
        /// </summary>
        private static readonly FDPosition NorthPassEscape = FDPosition.At(20, 2);
        private static readonly FDPosition EastRoadEscape = FDPosition.At(26, 20);

        public Chapter2(GameMain gameMain) : base(gameMain, 2)
        {
            int eventId = 0;
            // The original fired both of these on "TurnType_Friend Turn:N", which meant once
            // the last friend had acted -- the Npc phase boundary here. See LoadTurnEvent.
            LoadTurnEvent(++eventId, 1, CreatureFaction.Friend, turn1);
            LoadTurnEvent(++eventId, 3, CreatureFaction.Npc, turn3);

            LoadDeadEvent(++eventId, 1, (gameMain) => gameMain.OnGameOver());

            // Each villager has its own farewell line, keyed by the npc id.
            foreach (int npcId in NpcIds)
            {
                LoadDyingEvent(++eventId, npcId, NpcDyingMessage(npcId));
            }

            LoadTeamEvent(++eventId, CreatureFaction.Enemy, enemyClear);

            // Losing every villager loses the chapter.
            LoadTeamEvent(++eventId, CreatureFaction.Npc, (gameMain) => gameMain.OnGameOver());
        }

        private Action<GameMain> turn1 = (gameMain) =>
        {
            // The party walks in from the east road. In the original the first five were
            // the creatures carried over from the previous chapter; until that hand-over
            // exists they are spawned fresh, the way chapter 1 does it.
            AddCreatureToMap(gameMain, CreatureFaction.Friend, 1, 1, FDPosition.At(27, 16));
            AddCreatureToMap(gameMain, CreatureFaction.Friend, 2, 2, FDPosition.At(27, 16));
            AddCreatureToMap(gameMain, CreatureFaction.Friend, 3, 3, FDPosition.At(27, 16));
            AddCreatureToMap(gameMain, CreatureFaction.Friend, 4, 4, FDPosition.At(27, 16));
            AddCreatureToMap(gameMain, CreatureFaction.Friend, 5, 5, FDPosition.At(27, 16));

            gameMain.PushActivity(new ParallelActivity(
                new ActivityBase[] {
                    ActivityFactory.CreatureWalkActivity(1, FDMovePath.Create(FDPosition.At(27, 16), FDPosition.At(23, 16), FDPosition.At(23, 15))),
                    ActivityFactory.CreatureWalkActivity(2, FDMovePath.Create(FDPosition.At(27, 16), FDPosition.At(25, 16), FDPosition.At(25, 17))),
                    ActivityFactory.CreatureWalkActivity(3, FDMovePath.Create(FDPosition.At(27, 16), FDPosition.At(24, 16), FDPosition.At(24, 14))),
                    ActivityFactory.CreatureWalkActivity(4, FDMovePath.Create(FDPosition.At(27, 16), FDPosition.At(25, 16), FDPosition.At(25, 15))),
                    ActivityFactory.CreatureWalkActivity(5, FDMovePath.Create(FDPosition.At(27, 16), FDPosition.At(26, 16)))
                }
            ));

            gameMain.PushActivity(new SlideCursorActivity(23, 15));

            // Talking
            PushConversationsActivities(gameMain, 2, 1, 1, 1);

            // The leader steps forward, and the party talks it over
            gameMain.PushActivity(
                ActivityFactory.CreatureWalkActivity(1, FDMovePath.Create(FDPosition.At(23, 15), FDPosition.At(22, 15)))
            );

            // Note: sequence 5 does not exist in this chapter's conversation table.
            PushConversationsActivities(gameMain, 2, 1, 2, 4);

            // Focus the cursor on where the villagers will appear.
            gameMain.PushActivity(new SlideCursorActivity(17, 15));

            gameMain.PushActivity((gameMain) =>
            {
                // The villagers appear.
                AddCreatureToMap(gameMain, CreatureFaction.Npc, 91, 50201, FDPosition.At(17, 13));
                AddCreatureToMap(gameMain, CreatureFaction.Npc, 92, 50202, FDPosition.At(16, 13));
                AddCreatureToMap(gameMain, CreatureFaction.Npc, 93, 50202, FDPosition.At(8, 8));
                AddCreatureToMap(gameMain, CreatureFaction.Npc, 94, 50202, FDPosition.At(24, 7));
                AddCreatureToMap(gameMain, CreatureFaction.Npc, 95, 50201, FDPosition.At(7, 11));
                AddCreatureToMap(gameMain, CreatureFaction.Npc, 96, 50201, FDPosition.At(10, 13));

                // They are running for the north pass, and fight nobody on the way.
                SetVillagersEscapeTo(gameMain, NorthPassEscape);
            });

            gameMain.PushActivity(new ParallelActivity(
                new ActivityBase[] {
                    ActivityFactory.CreatureWalkActivity(91, FDMovePath.Create(FDPosition.At(17, 13), FDPosition.At(17, 15))),
                    ActivityFactory.CreatureWalkActivity(92, FDMovePath.Create(FDPosition.At(16, 13), FDPosition.At(16, 14)))
                }
            ));

            // Talking
            PushConversationsActivities(gameMain, 2, 1, 6, 11);

            // Focus the cursor on where the enemies will appear.
            gameMain.PushActivity(new SlideCursorActivity(10, 21));

            gameMain.PushActivity((gameMain) =>
            {
                // The main enemy group, chasing the villagers from the south
                for (int creatureId = 21; creatureId <= 26; creatureId++)
                {
                    AddCreatureToMap(gameMain, CreatureFaction.Enemy, creatureId, 50204, FDPosition.At(10, 21));
                }

                // The group holding the west side of the map
                AddCreatureToMap(gameMain, CreatureFaction.Enemy, 27, 50203, FDPosition.At(3, 9), 101);
                AddCreatureToMap(gameMain, CreatureFaction.Enemy, 28, 50203, FDPosition.At(4, 10), 801);
                AddCreatureToMap(gameMain, CreatureFaction.Enemy, 29, 50203, FDPosition.At(3, 11), 101);
                AddCreatureToMap(gameMain, CreatureFaction.Enemy, 30, 50203, FDPosition.At(2, 12), 101);
            });

            gameMain.PushActivity(new ParallelActivity(
                new ActivityBase[] {
                    ActivityFactory.CreatureWalkActivity(21, FDMovePath.Create(FDPosition.At(10, 21), FDPosition.At(10, 18))),
                    ActivityFactory.CreatureWalkActivity(22, FDMovePath.Create(FDPosition.At(10, 21), FDPosition.At(9, 21), FDPosition.At(9, 19))),
                    ActivityFactory.CreatureWalkActivity(23, FDMovePath.Create(FDPosition.At(10, 21), FDPosition.At(11, 21), FDPosition.At(11, 19))),
                    ActivityFactory.CreatureWalkActivity(24, FDMovePath.Create(FDPosition.At(10, 21), FDPosition.At(8, 21), FDPosition.At(8, 20))),
                    ActivityFactory.CreatureWalkActivity(25, FDMovePath.Create(FDPosition.At(10, 21), FDPosition.At(12, 21), FDPosition.At(12, 20))),
                    ActivityFactory.CreatureWalkActivity(26, FDMovePath.Create(FDPosition.At(10, 21), FDPosition.At(10, 20)))
                }
            ));

            // Talking
            PushConversationsActivities(gameMain, 2, 1, 12, 21);

            gameMain.PushActivity((gameMain) =>
            {
                // Play background music
                gameMain.PlayBackgroundMusic();
            });
        };

        private Action<GameMain> turn3 = (gameMain) =>
        {
            // Focus the cursor on where the reinforcements will appear.
            gameMain.PushActivity(new SlideCursorActivity(16, 1));

            gameMain.PushActivity((gameMain) =>
            {
                // Enemy reinforcements come down from the north pass
                for (int creatureId = 31; creatureId <= 36; creatureId++)
                {
                    AddCreatureToMap(gameMain, CreatureFaction.Enemy, creatureId, 50203, FDPosition.At(16, 1));
                }
            });

            gameMain.PushActivity(new ParallelActivity(
                new ActivityBase[] {
                    ActivityFactory.CreatureWalkActivity(31, FDMovePath.Create(FDPosition.At(16, 1), FDPosition.At(16, 4))),
                    ActivityFactory.CreatureWalkActivity(32, FDMovePath.Create(FDPosition.At(16, 1), FDPosition.At(15, 1), FDPosition.At(15, 3))),
                    ActivityFactory.CreatureWalkActivity(33, FDMovePath.Create(FDPosition.At(16, 1), FDPosition.At(17, 1), FDPosition.At(17, 3))),
                    ActivityFactory.CreatureWalkActivity(34, FDMovePath.Create(FDPosition.At(16, 1), FDPosition.At(14, 1), FDPosition.At(14, 2))),
                    ActivityFactory.CreatureWalkActivity(35, FDMovePath.Create(FDPosition.At(16, 1), FDPosition.At(18, 1), FDPosition.At(18, 2))),
                    ActivityFactory.CreatureWalkActivity(36, FDMovePath.Create(FDPosition.At(16, 1), FDPosition.At(16, 2)))
                }
            ));

            // Talking
            PushConversationsActivities(gameMain, 2, 2, 1, 9);

            gameMain.PushActivity((gameMain) =>
            {
                // The north pass is blocked now: the villagers turn around and run for the
                // east road behind the party instead.
                SetVillagersEscapeTo(gameMain, EastRoadEscape);
            });
        };

        private Action<GameMain> enemyClear = (gameMain) =>
        {
            // Every villager still standing means the party escorted all six out.
            bool isAllNpcSaved = gameMain.gameMap.Map.Npcs.Count == NpcIds.Length;

            if (isAllNpcSaved)
            {
                // The villagers hand over a power drink
                AddItemToTeam(gameMain, AllNpcSavedItemId);
            }

            // The two openings are the same line of dialog, thanked for or apologized for.
            int openingSequence = isAllNpcSaved ? 101 : 1;
            PushConversationsActivities(gameMain, 2, 3, openingSequence, openingSequence);

            gameMain.PushActivity((gameMain) =>
            {
                // Xiliya appears, and joins the party from here on
                AddCreatureToMap(gameMain, CreatureFaction.Friend, 6, 6, FDPosition.At(23, 5));
            });

            gameMain.PushActivity(
                ActivityFactory.CreatureWalkActivity(6, FDMovePath.Create(FDPosition.At(23, 5), FDPosition.At(23, 7)))
            );

            PushConversationsActivities(gameMain, 2, 3, 2, 24);

            // The chapter is over once the last enemy falls. OnGameWin queues itself
            // behind the conversation above, so the closing lines play out first.
            gameMain.OnGameWin();
        };

        /// <summary>
        /// Points every villager still on the map at an escape tile. Villagers that have
        /// already fallen are simply not there to redirect.
        /// </summary>
        private static void SetVillagersEscapeTo(GameMain gameMain, FDPosition escapePosition)
        {
            foreach (int npcId in NpcIds)
            {
                SetCreatureAiEscape(gameMain, npcId, escapePosition);
            }
        }

        /// <summary>
        /// The farewell line a villager says as it falls, one conversation per npc id.
        /// </summary>
        private static Action<GameMain> NpcDyingMessage(int npcId)
        {
            return (gameMain) => PushConversationsActivities(gameMain, 2, npcId, 1, 1);
        }

        /// <summary>
        /// Puts an item in the pack of the first party member with room for it, the way
        /// the original handed chapter rewards to the team.
        /// </summary>
        private static void AddItemToTeam(GameMain gameMain, int itemId)
        {
            foreach (FDCreature creature in gameMain.gameMap.Map.Friends)
            {
                if (!creature.IsItemsFull())
                {
                    creature.AddItem(itemId);
                    return;
                }
            }
        }
    }
}
