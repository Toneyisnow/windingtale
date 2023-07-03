using System;
using System.Collections.Generic;
using WindingTale.Core.Algorithms;
using WindingTale.Core.Common;
using WindingTale.Core.Definitions;
using WindingTale.Core.Events;
using WindingTale.Core.Map;
using WindingTale.Core.Objects;
using WindingTale.UI.Activities;
using WindingTale.UI.Scenes.Game;

namespace WindingTale.Chapters
{
    public class Chapter1 : ChapterEvents
    {
        
        public Chapter1() : base (1)
        {
            int eventId = 0;
            LoadTurnEvent(++eventId, 1, CreatureFaction.Friend, turn1);
            LoadTurnEvent(++eventId, 3, CreatureFaction.Friend, turn3);
            LoadTurnEvent(++eventId, 4, CreatureFaction.Friend, turn4);
            LoadTurnEvent(++eventId, 5, CreatureFaction.Friend, turn5_Boss);
            LoadTurnEvent(++eventId, 6, CreatureFaction.Friend, turn6_Npc);

            LoadDeadEvent(++eventId, 1, (gameMain) => gameMain.OnGameOver());
            LoadDeadEvent(++eventId, 5, hanuoDead);
            LoadDeadEvent(++eventId, 6, hawateDead);

            LoadDyingEvent(++eventId, 29, showBossDyingMessage);

            LoadTeamEvent(++eventId, CreatureFaction.Enemy, enemyClear);
        }


        private Action<GameMain> turn1 = (gameMain) =>
        {
            // Friends appear
            FDCreature c1 = gameMain.AddCreature(CreatureFaction.Friend, 1, 1, FDPosition.At(8, 20));
            FDCreature c2 = gameMain.AddCreature(CreatureFaction.Friend, 2, 2, FDPosition.At(11, 21));
            FDCreature c3 = gameMain.AddCreature(CreatureFaction.Friend, 3, 3, FDPosition.At(9, 22));
            FDCreature c4 = gameMain.AddCreature(CreatureFaction.Friend, 4, 4, FDPosition.At(12, 23));

            gameMain.ActivityManager.Push(new BatchActivity(
                new ActivityBase[] {
                    new CreatureMoveActivity(c1, FDMovePath.Create(FDPosition.At(8, 15))),
                    new CreatureMoveActivity(c2, FDMovePath.Create(FDPosition.At(11, 16))),
                    new CreatureMoveActivity(c3, FDMovePath.Create(FDPosition.At(9, 17))),
                    new CreatureMoveActivity(c4, FDMovePath.Create(FDPosition.At(12, 18))) })
                );

            /*
            // Talking
            ShowConversations(gameMain, 1, 1, 1, 5);

            // Enemy Group1 appear
            FDCreature e1 = gameMain.AddCreature(CreatureFaction.Enemy, 11, 50101, FDPosition.At(2, 22));
            FDCreature e2 = gameMain.AddCreature(CreatureFaction.Enemy, 12, 50101, FDPosition.At(3, 22), 101);
            FDCreature e3 = gameMain.AddCreature(CreatureFaction.Enemy, 13, 50101, FDPosition.At(4, 23));
            FDCreature e4 = gameMain.AddCreature(CreatureFaction.Enemy, 14, 50101, FDPosition.At(5, 23));

            gameMain.ActivityManager.Push(new BatchActivity(
                new ActivityBase[] {
                    new CreatureMoveActivity(e1, FDMovePath.Create(FDPosition.At(2, 19), FDPosition.At(3, 19))),
                    new CreatureMoveActivity(e2, FDMovePath.Create(FDPosition.At(4, 22), FDPosition.At(4, 19))),
                    new CreatureMoveActivity(e3, FDMovePath.Create(FDPosition.At(6, 23), FDPosition.At(6, 20))),
                    new CreatureMoveActivity(e4, FDMovePath.Create(FDPosition.At(5, 20))) })
                );

            // Enemy Group2 appear
            FDCreature e5 = gameMain.AddCreature(CreatureFaction.Enemy, 15, 50101, FDPosition.At(4, 2), 101);
            FDCreature e6 = gameMain.AddCreature(CreatureFaction.Enemy, 16, 50101, FDPosition.At(3, 2));
            FDCreature e7 = gameMain.AddCreature(CreatureFaction.Enemy, 17, 50101, FDPosition.At(2, 3), 101);
            FDCreature e8 = gameMain.AddCreature(CreatureFaction.Enemy, 18, 50101, FDPosition.At(2, 3));

            gameMain.ActivityManager.Push(new BatchActivity(
                new ActivityBase[] {
                    new CreatureMoveActivity(e5, FDMovePath.Create(FDPosition.At(7, 2))),
                    new CreatureMoveActivity(e6, FDMovePath.Create(FDPosition.At(2, 5))),
                    new CreatureMoveActivity(e7, FDMovePath.Create(FDPosition.At(5, 3))),
                    new CreatureMoveActivity(e8, FDMovePath.Create(FDPosition.At(3, 3))) })
                );

            // Talking
            ShowConversations(gameMain, 1, 1, 6, 7);


            // One Enemy move away
            gameMain.CreatureMove(e4, FDMovePath.Create(FDPosition.At(8, 20), FDPosition.At(8, 24)));
            //// gameMain.DisposeCreature(14);

            // Talking
            ShowConversations(gameMain, 1, 1, 8, 19);
            */

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