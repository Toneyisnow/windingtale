using System;
using System.Collections.Generic;
using WindingTale.Core.Algorithms;
using WindingTale.Core.Common;
using WindingTale.Core.Definitions;
using WindingTale.Core.Events;
using WindingTale.Core.Objects;
using WindingTale.Legacy.Core.Components;
using WindingTale.UI.Scenes.Game;

namespace WindingTale.Chapters
{
    public class Chapter1 : ChapterEvents
    {
        public Chapter1()
        {
            LoadTurnEvent(1, CreatureFaction.Friend, turn1);
        
        }


        private Action<GameMain> turn1 = (game) =>
        {
            // Friends appear
            FDCreature c1 = game.AddCreature(CreatureFaction.Friend, 1, 1, FDPosition.At(8, 20));
            FDCreature c2 = game.AddCreature(CreatureFaction.Friend, 2, 2, FDPosition.At(11, 21));
            FDCreature c3 = game.AddCreature(CreatureFaction.Friend, 3, 3, FDPosition.At(9, 22));
            FDCreature c4 = game.AddCreature(CreatureFaction.Friend, 4, 4, FDPosition.At(12, 23));

            List<SingleWalkAction> walks1 = new List<SingleWalkAction>();
            game.CreatureMove(c1, FDMovePath.Create(FDPosition.At(8, 15)));
            game.CreatureMove(c2, FDMovePath.Create(FDPosition.At(11, 16)));
            game.CreatureMove(c3, FDMovePath.Create(FDPosition.At(9, 17)));
            game.CreatureMove(c4, FDMovePath.Create(FDPosition.At(12, 18)));


            // Talking
            //game.ShowTalk(1, 1, 5);


            /*
            // Enemy Group1 appear
            game.ComposeCreature(CreatureFaction.Enemy, 11, 50101, FDPosition.At(2, 22));
            game.ComposeCreature(CreatureFaction.Enemy, 12, 50101, FDPosition.At(3, 22), 101);
            game.ComposeCreature(CreatureFaction.Enemy, 13, 50101, FDPosition.At(4, 23));
            game.ComposeCreature(CreatureFaction.Enemy, 14, 50101, FDPosition.At(5, 23));

            List<SingleWalkAction> walks2 = new List<SingleWalkAction>();
            walks2.Add(new SingleWalkAction(11, FDMovePath.Create(FDPosition.At(2, 19), FDPosition.At(3, 19))));
            walks2.Add(new SingleWalkAction(12, FDMovePath.Create(FDPosition.At(4, 22), FDPosition.At(4, 19))));
            walks2.Add(new SingleWalkAction(13, FDMovePath.Create(FDPosition.At(6, 23), FDPosition.At(6, 20))));
            walks2.Add(new SingleWalkAction(14, FDMovePath.Create(FDPosition.At(5, 20))));
            game.CreatureWalks(walks2);

            // Enemy Group2 appear
            game.ComposeCreature(CreatureFaction.Enemy, 15, 50101, FDPosition.At(4, 2), 101);
            game.ComposeCreature(CreatureFaction.Enemy, 16, 50101, FDPosition.At(3, 2));
            game.ComposeCreature(CreatureFaction.Enemy, 17, 50101, FDPosition.At(2, 3), 101);
            game.ComposeCreature(CreatureFaction.Enemy, 18, 50101, FDPosition.At(2, 3));

            List<SingleWalkAction> walks3 = new List<SingleWalkAction>();
            walks3.Add(new SingleWalkAction(15, FDMovePath.Create(FDPosition.At(7, 2))));
            walks3.Add(new SingleWalkAction(16, FDMovePath.Create(FDPosition.At(2, 5))));
            walks3.Add(new SingleWalkAction(17, FDMovePath.Create(FDPosition.At(5, 3))));
            walks3.Add(new SingleWalkAction(18, FDMovePath.Create(FDPosition.At(3, 3))));
            game.CreatureWalks(walks3);

            // Talking
            game.ShowTalk(1, 6, 7);

            // One Enemy move away
            game.CreatureWalk(new SingleWalkAction(14, FDMovePath.Create(FDPosition.At(8, 20), FDPosition.At(8, 24))));
            game.DisposeCreature(14);

            // Talking
            game.ShowTalk(1, 8, 19);

            */
        };
    }
}