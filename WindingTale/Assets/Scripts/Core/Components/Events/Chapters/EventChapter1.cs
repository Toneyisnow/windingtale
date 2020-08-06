using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WindingTale.Common;
using WindingTale.Core.Definitions;
using WindingTale.Core.ObjectModels;

namespace WindingTale.Core.Components.Events
{
    public class EventChapter1 : EventLoader
    {
        public EventChapter1(GameEventManager eventManager)
            : base(eventManager)
        {

        }

        private Action<IGameAction> turn1 = (game) =>
        {
            // Friends appear
            game.ComposeCreature(CreatureFaction.Friend, 1, 1, FDPosition.At(8, 20));
            game.ComposeCreature(CreatureFaction.Friend, 2, 2, FDPosition.At(11, 21));
            game.ComposeCreature(CreatureFaction.Friend, 3, 3, FDPosition.At(9, 22));
            game.ComposeCreature(CreatureFaction.Friend, 4, 4, FDPosition.At(12, 23));

            List<SingleWalkAction> walks = new List<SingleWalkAction>();
            walks.Add(new SingleWalkAction(1, FDMovePath.Create(FDPosition.At(8, 15))));
            walks.Add(new SingleWalkAction(2, FDMovePath.Create(FDPosition.At(11, 16))));
            walks.Add(new SingleWalkAction(3, FDMovePath.Create(FDPosition.At(9, 17))));
            walks.Add(new SingleWalkAction(4, FDMovePath.Create(FDPosition.At(12, 18))));
            game.CreatureWalks(walks);

            // Talking
            game.ShowTalk(1, 1, 5);

            // Enemy Group1 appear
            game.ComposeCreature(CreatureFaction.Enemy, 11, 50101, FDPosition.At(2, 22));
            game.ComposeCreature(CreatureFaction.Enemy, 12, 50101, FDPosition.At(3, 22), 101);
            game.ComposeCreature(CreatureFaction.Enemy, 13, 50101, FDPosition.At(4, 23));
            game.ComposeCreature(CreatureFaction.Enemy, 14, 50101, FDPosition.At(5, 23));

            List<SingleWalkAction> walks2 = new List<SingleWalkAction>();
            walks2.Add(new SingleWalkAction(11, FDMovePath.Create(FDPosition.At(3, 19))));
            walks2.Add(new SingleWalkAction(12, FDMovePath.Create(FDPosition.At(4, 19))));
            walks2.Add(new SingleWalkAction(13, FDMovePath.Create(FDPosition.At(6, 20))));
            walks2.Add(new SingleWalkAction(14, FDMovePath.Create(FDPosition.At(5, 20))));
            game.CreatureWalks(walks);

            // Enemy Group2 appear
            game.ComposeCreature(CreatureFaction.Enemy, 15, 50101, FDPosition.At(4, 2), 101);
            game.ComposeCreature(CreatureFaction.Enemy, 16, 50101, FDPosition.At(3, 2));
            game.ComposeCreature(CreatureFaction.Enemy, 17, 50101, FDPosition.At(2, 3), 101);
            game.ComposeCreature(CreatureFaction.Enemy, 18, 50101, FDPosition.At(2, 3));

            List<SingleWalkAction> walks3 = new List<SingleWalkAction>();
            walks3.Add(new SingleWalkAction(15, FDMovePath.Create(FDPosition.At(7, 2))));
            walks3.Add(new SingleWalkAction(16, FDMovePath.Create(FDPosition.At(2, 5))));
            walks3.Add(new SingleWalkAction(17, FDMovePath.Create(FDPosition.At(3, 3))));
            walks3.Add(new SingleWalkAction(18, FDMovePath.Create(FDPosition.At(3, 3))));
            game.CreatureWalks(walks);

            // Talking
            game.ShowTalk(1, 6, 7);

            // One Enemy move away
            game.CreatureWalk(new SingleWalkAction(14, FDMovePath.Create(FDPosition.At(8, 20), FDPosition.At(8, 24))));
            game.DisposeCreature(14);

            // Talking
            game.ShowTalk(1, 8, 19);
        };

        private Action<IGameAction> turn3 = (game) =>
        {

        };

        private Action<IGameAction> turn4 = (game) =>
        {

        };

        private Action<IGameAction> turn5_Boss = (game) =>
        {

        };

        private Action<IGameAction> turn6_Npc = (game) =>
        {

        };


        public override void LoadEvents()
        {
            int eventIndex = 0;
            LoadTurnEvent(eventIndex++, 1, CreatureFaction.Friend, turn1);
            LoadTurnEvent(eventIndex++, 3, CreatureFaction.Friend, turn3);
            LoadTurnEvent(eventIndex++, 4, CreatureFaction.Friend, turn4);
            LoadTurnEvent(eventIndex++, 5, CreatureFaction.Friend, turn5_Boss);
            LoadTurnEvent(eventIndex++, 6, CreatureFaction.Friend, turn6_Npc);

            LoadDeadEvent(eventIndex++, 1, gameOver);
            LoadDeadEvent(eventIndex++, 5, hanuoDead);
            LoadDeadEvent(eventIndex++, 6, hawateDead);

            LoadDyingEvent(eventIndex++, 29, showBossDyingMessage);

            LoadTeamEvent(eventIndex++, CreatureFaction.Enemy, enemyClear);

        }

        private Action<IGameAction> hanuoDead = (game) =>
        {
            FDCreature hawate = game.GetCreature(6);
            if (hawate != null)
            {
                game.ShowTalk(6, 1, 4);

                // Hawate converts to Npc
                game.SwitchCreature(6, CreatureFaction.Npc);
            }

        };

        private Action<IGameAction> hawateDead = (game) =>
        {
            // Nothing here
        };

        private Action<IGameAction> showBossDyingMessage = (game) =>
        {
            game.ShowTalk(99, 1, 1);
        };

        private Action<IGameAction> enemyClear = (game) =>
        {
            game.ShowTalk(7, 1, 13);

            // Adjust friends
            game.DisposeCreature(6, false);
        };

    }

}
