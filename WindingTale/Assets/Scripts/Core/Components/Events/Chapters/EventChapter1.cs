using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WindingTale.Common;
using WindingTale.Core.Definitions;

namespace WindingTale.Core.Components.Events
{
    public class EventChapter1 : EventLoader
    {
        public EventChapter1(GameEventManager eventManager)
            : base(eventManager)
        {

        }

        public override void LoadEvents()
        {
            LoadTurnEvent(101, 1, CreatureFaction.Friend, (game) => {

                // game.ComposeCreatureByDef(1, 1, FDPosition.At(10, 10));
                game.ComposeCreatureByDef(CreatureFaction.Friend, 2, 2, FDPosition.At(11, 11));
                game.ComposeCreatureByDef(CreatureFaction.Friend, 3, 3, FDPosition.At(12, 12));
                game.ComposeCreatureByDef(CreatureFaction.Friend, 4, 4, FDPosition.At(13, 13));

                List<SingleWalkAction> walks = new List<SingleWalkAction>();
                walks.Add(new SingleWalkAction(2, FDMovePath.Create(FDPosition.At(11, 13))));
                walks.Add(new SingleWalkAction(3, FDMovePath.Create(FDPosition.At(11, 13))));
                walks.Add(new SingleWalkAction(4, FDMovePath.Create(FDPosition.At(11, 13))));

                game.CreatureWalk(walks);

            });
        }
    }

}
