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
            LoadTurnEvent(++eventId, 3, CreatureFaction.Friend, turn3);
            LoadTurnEvent(++eventId, 4, CreatureFaction.Friend, turn4);
            LoadTurnEvent(++eventId, 5, CreatureFaction.Friend, turn5_Boss);
            LoadTurnEvent(++eventId, 6, CreatureFaction.Friend, turn6_Npc);

            LoadDeadEvent(++eventId, 1, (gameMain) => gameMain.onGameOver());
            LoadDeadEvent(++eventId, 5, hanuoDead);
            LoadDeadEvent(++eventId, 6, hawateDead);

            LoadDyingEvent(++eventId, 29, showBossDyingMessage);

            LoadTeamEvent(++eventId, CreatureFaction.Enemy, enemyClear);
        }


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
                //// ActivityFactory.CreatureWalkActivity(3, FDMovePath.Create(FDPosition.At(9, 22), FDPosition.At(9, 8), FDPosition.At(6, 8))),
                ActivityFactory.CreatureWalkActivity(4, FDMovePath.Create(FDPosition.At(12, 23), FDPosition.At(12, 18)))
                }
            ));

            // Talking
            ShowConversations(gameMain, 1, 1, 1, 5);

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
                e5 = AddCreatureToMap(gameMain, CreatureFaction.Enemy, 15, 50101, FDPosition.At(4, 2), 101);
                e6 = AddCreatureToMap(gameMain, CreatureFaction.Enemy, 16, 50101, FDPosition.At(3, 2));
                e7 = AddCreatureToMap(gameMain, CreatureFaction.Enemy, 17, 50101, FDPosition.At(2, 3), 101);
                e8 = AddCreatureToMap(gameMain, CreatureFaction.Enemy, 18, 50101, FDPosition.At(2, 3));
            });

            gameMain.PushActivity(new ParallelActivity(
                new ActivityBase[] {
                    ActivityFactory.CreatureWalkActivity(15, FDMovePath.Create(FDPosition.At(4, 2), FDPosition.At(7, 2))),
                    ActivityFactory.CreatureWalkActivity(16, FDMovePath.Create(FDPosition.At(3, 2), FDPosition.At(2, 2), FDPosition.At(2, 5))),
                    ActivityFactory.CreatureWalkActivity(17, FDMovePath.Create(FDPosition.At(2, 3), FDPosition.At(5, 3))),
                    ActivityFactory.CreatureWalkActivity(18, FDMovePath.Create(FDPosition.At(2, 3), FDPosition.At(3, 3))) })
                );

            // Talking
            ShowConversations(gameMain, 1, 1, 6, 7);


            // One Enemy move away
            gameMain.PushActivity(
                    ActivityFactory.CreatureWalkActivity(14, FDMovePath.Create(FDPosition.At(5, 20), FDPosition.At(8, 20), FDPosition.At(8, 24))));

            gameMain.PushActivity((gameMain) =>
            {
                gameMain.gameMap.RemoveCreature(14);
            });

            // Talking
            ShowConversations(gameMain, 1, 1, 8, 9);
            ////ShowConversations(gameMain, 1, 1, 8, 19);

        };

        private Action<GameMain> turn3 = (gameMain) =>
        {
        };

        private Action<GameMain> turn4 = (gameMain) =>
        {
        };

        private Action<GameMain> turn5_Boss = (gameMain) =>
        {
        };

        private Action<GameMain> turn6_Npc = (gameMain) =>
        {
        };

        private Action<GameMain> hanuoDead = (gameMain) =>
        {

        };

        private Action<GameMain> hawateDead = (gameMain) =>
        {

        };

        private Action<GameMain> showBossDyingMessage = (gameMain) =>
        {
            ShowConversations(gameMain, 1, 99, 1, 1);
        };

        private Action<GameMain> enemyClear = (gameMain) =>
        {
            ShowConversations(gameMain, 1, 7, 1, 13);

            // Adjust friends

        };
    }
}