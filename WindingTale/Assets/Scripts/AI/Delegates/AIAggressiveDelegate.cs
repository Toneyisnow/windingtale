using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WindingTale.Common;
using WindingTale.Core.Components;
using WindingTale.Core.ObjectModels;

namespace WindingTale.AI.Delegates
{
    public class AIAggressiveDelegate : AIDelegate
    {
        public AIAggressiveDelegate(IGameAction gameAction, FDCreature c) : base(gameAction, c)
        {

        }

        public override void TakeAction()
        {
            FDPosition pos = this.Creature.Position;
            FDPosition path1 = FDPosition.At(pos.X + 3, pos.Y);
            FDPosition target = FDPosition.At(pos.X + 3, pos.Y + 2);

            FDMovePath movePath = FDMovePath.Create(path1, target);

            this.GameAction.CreatureWalk(new SingleWalkAction(this.Creature.CreatureId, movePath));

            this.GameAction.DoCreatureRest(this.Creature.CreatureId);
        }

    }
}
