using System;
using System.Collections.Generic;
using Unity.VisualScripting;
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
    public class Chapter1 : ChapterEvents
    {
        
        public Chapter1(GameMain gameMain) : base (gameMain, 1)
        {
            int eventId = 0;
            LoadTurnEvent(++eventId, 1, CreatureFaction.Friend, turn1);
            LoadTurnEvent(++eventId, 3, CreatureFaction.Enemy, turn3);
            LoadTurnEvent(++eventId, 4, CreatureFaction.Enemy, turn4);
            LoadTurnEvent(++eventId, 5, CreatureFaction.Enemy, turn5_Boss);
            LoadTurnEvent(++eventId, 6, CreatureFaction.Npc, turn6_Npc);

            LoadDeadEvent(++eventId, 1, (gameMain) => gameMain.OnGameOver());
            LoadDeadEvent(++eventId, 5, hanuoDead);
            LoadDeadEvent(++eventId, 6, hawateDead);

            LoadDyingEvent(++eventId, 29, showBossDyingMessage);

            LoadTeamEvent(++eventId, CreatureFaction.Enemy, enemyClear);
        }

        private Action<GameMain> turn1_test = (gameMain) =>
        {
            // Friends appear
            FDCreature c1 = AddCreatureToMap(gameMain, CreatureFaction.Friend, 1, 1, FDPosition.At(8, 20));
            FDCreature c2 = AddCreatureToMap(gameMain, CreatureFaction.Friend, 2, 2, FDPosition.At(11, 21));
            FDCreature c3 = AddCreatureToMap(gameMain, CreatureFaction.Friend, 3, 3, FDPosition.At(9, 22));
            FDCreature c4 = AddCreatureToMap(gameMain, CreatureFaction.Friend, 4, 4, FDPosition.At(12, 23));


            FDCreature e1 = null;
            FDCreature e2 = null;
            FDCreature e3 = null;
            FDCreature e4 = null;
            FDCreature e5 = null;
            FDCreature e6 = null;
            FDCreature e7 = null;
            FDCreature e8 = null;

            gameMain.PushActivity((gameMain) =>
            {
                // Enemy Group1 appear
                e1 = AddCreatureToMap(gameMain, CreatureFaction.Enemy, 11, 50101, FDPosition.At(2, 22));
                e2 = AddCreatureToMap(gameMain, CreatureFaction.Enemy, 12, 50101, FDPosition.At(3, 22), 101);
                e3 = AddCreatureToMap(gameMain, CreatureFaction.Enemy, 13, 50101, FDPosition.At(4, 23));
                e4 = AddCreatureToMap(gameMain, CreatureFaction.Enemy, 14, 50101, FDPosition.At(5, 23));
            });

        };

        private Action<GameMain> turn1 = (gameMain) =>
        {
            // Friends appear
            FDCreature c1 = AddCreatureToMap(gameMain, CreatureFaction.Friend, 1, 1, FDPosition.At(8, 20));
            FDCreature c2 = AddCreatureToMap(gameMain, CreatureFaction.Friend, 2, 2, FDPosition.At(11, 21));
            FDCreature c3 = AddCreatureToMap(gameMain, CreatureFaction.Friend, 3, 3, FDPosition.At(9, 22));
            FDCreature c4 = AddCreatureToMap(gameMain, CreatureFaction.Friend, 4, 4, FDPosition.At(12, 23));

            //FDCreature c1 = AddCreatureToMap(gameMain, CreatureFaction.Friend, 1, 1, FDPosition.At(4, 11));
            //FDCreature c2 = AddCreatureToMap(gameMain, CreatureFaction.Friend, 2, 2, FDPosition.At(4, 8));
            //FDCreature c3 = AddCreatureToMap(gameMain, CreatureFaction.Friend, 3, 3, FDPosition.At(5, 9));
            //FDCreature c4 = AddCreatureToMap(gameMain, CreatureFaction.Friend, 4, 4, FDPosition.At(6, 7));

            //FDCreature e1 = AddCreatureToMap(gameMain, CreatureFaction.Enemy, 11, 50101, FDPosition.At(5, 6));
            //FDCreature e2 = AddCreatureToMap(gameMain, CreatureFaction.Enemy, 12, 50101, FDPosition.At(3, 7), 101);

            gameMain.PushActivity(new ParallelActivity(
            new ActivityBase[] {
                ActivityFactory.CreatureWalkActivity(1, FDMovePath.Create(FDPosition.At(8, 20), FDPosition.At(8, 15))),
                ActivityFactory.CreatureWalkActivity(2, FDMovePath.Create(FDPosition.At(11, 21), FDPosition.At(11, 16))),
                ActivityFactory.CreatureWalkActivity(3, FDMovePath.Create(FDPosition.At(9, 22), FDPosition.At(9, 17))),
                ActivityFactory.CreatureWalkActivity(4, FDMovePath.Create(FDPosition.At(12, 23), FDPosition.At(12, 18)))
                }
            ));

            // Talking
            PushConversationsActivities(gameMain, 1, 1, 1, 5);

            gameMain.PushActivity((gameMain) =>
            {
                // Enemy Group1 appear
                AddCreatureToMap(gameMain, CreatureFaction.Enemy, 11, 50101, FDPosition.At(2, 22));
                AddCreatureToMap(gameMain, CreatureFaction.Enemy, 12, 50101, FDPosition.At(3, 22), 101);
                AddCreatureToMap(gameMain, CreatureFaction.Enemy, 13, 50101, FDPosition.At(4, 23));
                AddCreatureToMap(gameMain, CreatureFaction.Enemy, 14, 50101, FDPosition.At(5, 23));
            });

            gameMain.PushActivity(new ParallelActivity(
                new ActivityBase[] {
                    ActivityFactory.CreatureWalkActivity(11, FDMovePath.Create(FDPosition.At(2, 22), FDPosition.At(2, 19), FDPosition.At(3, 19))),
                    ActivityFactory.CreatureWalkActivity(12, FDMovePath.Create(FDPosition.At(3, 22),FDPosition.At(4, 22), FDPosition.At(4, 19))),
                    ActivityFactory.CreatureWalkActivity(13, FDMovePath.Create(FDPosition.At(4, 23), FDPosition.At(6, 23), FDPosition.At(6, 20))),
                    ActivityFactory.CreatureWalkActivity(14, FDMovePath.Create(FDPosition.At(5, 23), FDPosition.At(5, 20))) })
                );

            gameMain.PushActivity((gameMain) =>
            {
                // Enemy Group2 appear
                AddCreatureToMap(gameMain, CreatureFaction.Enemy, 15, 50101, FDPosition.At(4, 2), 101);
                AddCreatureToMap(gameMain, CreatureFaction.Enemy, 16, 50101, FDPosition.At(3, 2));
                AddCreatureToMap(gameMain, CreatureFaction.Enemy, 17, 50101, FDPosition.At(2, 3), 101);
                AddCreatureToMap(gameMain, CreatureFaction.Enemy, 18, 50101, FDPosition.At(2, 3));
            });

            gameMain.PushActivity(new ParallelActivity(
                new ActivityBase[] {
                    ActivityFactory.CreatureWalkActivity(15, FDMovePath.Create(FDPosition.At(4, 2), FDPosition.At(7, 2))),
                    ActivityFactory.CreatureWalkActivity(16, FDMovePath.Create(FDPosition.At(3, 2), FDPosition.At(2, 2), FDPosition.At(2, 5))),
                    ActivityFactory.CreatureWalkActivity(17, FDMovePath.Create(FDPosition.At(2, 3), FDPosition.At(5, 3))),
                    ActivityFactory.CreatureWalkActivity(18, FDMovePath.Create(FDPosition.At(2, 3), FDPosition.At(3, 3))) })
                );

            gameMain.PushActivity((gameMain) =>
            {
                // Talking
                // Note: Because the enemies are created after the above activity, we need to put the talking activity here
                PushConversationsActivities(gameMain, 1, 1, 6, 7);

                // One Enemy move away
                gameMain.PushActivity(
                    ActivityFactory.CreatureWalkActivity(14, FDMovePath.Create(FDPosition.At(5, 20), FDPosition.At(8, 20), FDPosition.At(8, 24)))
                );

                gameMain.PushActivity((gameMain) =>
                {
                    gameMain.gameMap.RemoveCreature(14);
                });

                // Talking
                PushConversationsActivities(gameMain, 1, 1, 8, 19);

                gameMain.PushActivity((gameMain) =>
                {
                    // Play background music
                    gameMain.PlayBackgroundMusic();
                });
            });
        };

        private Action<GameMain> turn3 = (gameMain) =>
        {
            // Friends appear
            FDCreature c5 = AddCreatureToMap(gameMain, CreatureFaction.Friend, 5, 5, FDPosition.At(12, 12));
            FDCreature c6 = AddCreatureToMap(gameMain, CreatureFaction.Friend, 6, 1016, FDPosition.At(12, 12));

            gameMain.PushActivity(new ParallelActivity(
                new ActivityBase[] {
                    ActivityFactory.CreatureWalkActivity(5, FDMovePath.Create(FDPosition.At(12, 12), FDPosition.At(12, 14))),
                    ActivityFactory.CreatureWalkActivity(6, FDMovePath.Create(FDPosition.At(12, 12), FDPosition.At(12, 13)))
                }
            ));

            // Talking
            PushConversationsActivities(gameMain, 1, 2, 1, 13);
        };

        private Action<GameMain> turn4 = (gameMain) =>
        {
            // Enemy Group1 appear
            AddCreatureToMap(gameMain, CreatureFaction.Enemy, 21, 50101, FDPosition.At(19, 23));
            AddCreatureToMap(gameMain, CreatureFaction.Enemy, 22, 50101, FDPosition.At(20, 22), 101);
            AddCreatureToMap(gameMain, CreatureFaction.Enemy, 23, 50101, FDPosition.At(21, 21));
            AddCreatureToMap(gameMain, CreatureFaction.Enemy, 24, 50101, FDPosition.At(22, 21));
            
            gameMain.PushActivity(new ParallelActivity(
                new ActivityBase[] {
                    ActivityFactory.CreatureWalkActivity(21, FDMovePath.Create(FDPosition.At(19, 23), FDPosition.At(17, 23), FDPosition.At(17, 21))),
                    ActivityFactory.CreatureWalkActivity(22, FDMovePath.Create(FDPosition.At(20, 22), FDPosition.At(20, 20), FDPosition.At(18, 20))),
                    ActivityFactory.CreatureWalkActivity(23, FDMovePath.Create(FDPosition.At(21, 21), FDPosition.At(21, 20), FDPosition.At(20, 20))),
                    ActivityFactory.CreatureWalkActivity(24, FDMovePath.Create(FDPosition.At(22, 21), FDPosition.At(21, 21), FDPosition.At(21, 19))),
                }
            ));

            PushConversationsActivities(gameMain, 1, 3, 1, 2);
        };

        private Action<GameMain> turn5_Boss = (gameMain) =>
        {
            // Enemy Group1 appear
            AddCreatureToMap(gameMain, CreatureFaction.Enemy, 25, 50101, FDPosition.At(1, 22));
            AddCreatureToMap(gameMain, CreatureFaction.Enemy, 26, 50101, FDPosition.At(2, 22));
            AddCreatureToMap(gameMain, CreatureFaction.Enemy, 27, 50101, FDPosition.At(5, 24));
            AddCreatureToMap(gameMain, CreatureFaction.Enemy, 28, 50101, FDPosition.At(5, 24));
            AddCreatureToMap(gameMain, CreatureFaction.Enemy, 29, 50102, FDPosition.At(4, 23), 201);

            gameMain.PushActivity(new ParallelActivity(
                new ActivityBase[] {
                    ActivityFactory.CreatureWalkActivity(29, FDMovePath.Create(FDPosition.At(4, 23), FDPosition.At(4, 22))),
                    ActivityFactory.CreatureWalkActivity(25, FDMovePath.Create(FDPosition.At(1, 22), FDPosition.At(1, 21))),
                    ActivityFactory.CreatureWalkActivity(26, FDMovePath.Create(FDPosition.At(2, 22), FDPosition.At(2, 21))),
                    ActivityFactory.CreatureWalkActivity(27, FDMovePath.Create(FDPosition.At(5, 24), FDPosition.At(5, 23))),
                    ActivityFactory.CreatureWalkActivity(28, FDMovePath.Create(FDPosition.At(5, 24), FDPosition.At(6, 24))),
                }
            ));

            PushConversationsActivities(gameMain, 1, 4, 1, 11);
        };

        private Action<GameMain> turn6_Npc = (gameMain) =>
        {
            // Enemy Group1 appear
            AddCreatureToMap(gameMain, CreatureFaction.Npc, 31, 50103, FDPosition.At(24, 15));
            AddCreatureToMap(gameMain, CreatureFaction.Npc, 32, 50103, FDPosition.At(24, 15));
            AddCreatureToMap(gameMain, CreatureFaction.Npc, 33, 50103, FDPosition.At(24, 15));
            AddCreatureToMap(gameMain, CreatureFaction.Npc, 34, 50103, FDPosition.At(24, 15));

            gameMain.PushActivity(new ParallelActivity(
                new ActivityBase[] {
                    ActivityFactory.CreatureWalkActivity(31, FDMovePath.Create(FDPosition.At(24, 15), FDPosition.At(18, 15))),
                    ActivityFactory.CreatureWalkActivity(32, FDMovePath.Create(FDPosition.At(24, 15), FDPosition.At(19, 15), FDPosition.At(19, 14))),
                    ActivityFactory.CreatureWalkActivity(33, FDMovePath.Create(FDPosition.At(24, 15), FDPosition.At(19, 15), FDPosition.At(19, 16))),
                    ActivityFactory.CreatureWalkActivity(34, FDMovePath.Create(FDPosition.At(24, 15), FDPosition.At(19, 15), FDPosition.At(20, 15)))
                }
            ));

            PushConversationsActivities(gameMain, 1, 5, 1, 6);
        };

        private Action<GameMain> hanuoDead = (gameMain) =>
        {
            PushConversationsActivities(gameMain, 1, 91, 1, 4);
        };

        private Action<GameMain> hawateDead = (gameMain) =>
        {

        };

        private Action<GameMain> showBossDyingMessage = (gameMain) =>
        {
            PushConversationsActivities(gameMain, 1, 99, 1, 1);
        };

        private Action<GameMain> enemyClear = (gameMain) =>
        {
            PushConversationsActivities(gameMain, 1, 7, 1, 13);

            // Adjust friends

        };
    }
}