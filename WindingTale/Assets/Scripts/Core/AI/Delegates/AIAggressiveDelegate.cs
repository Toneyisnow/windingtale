using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WindingTale.Common;
using WindingTale.Core.Components;
using WindingTale.Core.Components.Algorithms;
using WindingTale.Core.Definitions;
using WindingTale.Core.Definitions.Items;
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
            if (this.NeedAndCanRecover())
            {
                this.GameAction.DoCreatureRest(this.Creature.CreatureId);
                return;
            }

            // Get target 
            FDCreature target = this.LookForAggressiveTarget();

            // According to the target, find the nearest position within the Move scope, and get the path to that position
            FDMovePath movePath = this.DecidePositionAndPath(target.Position);

            // Do the walk
            this.GameAction.CreatureWalk(new SingleWalkAction(this.Creature.CreatureId, movePath));

            FDPosition destination = movePath.Desitination ?? this.Creature.Position;

            AttackItemDefinition item = this.Creature.Data.GetAttackItem();
            if (item != null)
            {
                FDSpan span = item.AttackScope;
                DirectRangeFinder finder = new DirectRangeFinder(this.GameAction.GetField(), destination, span.Max, span.Min);
                FDRange range = finder.CalculateRange();
                if (range.Contains(target.Position))
                {
                    // If in attack range, attack the target
                    this.GameAction.DoCreatureAttack(this.Creature.CreatureId, target.Position);
                }
            }

            this.GameAction.DoCreatureRest(this.Creature.CreatureId);
        }
    }
}
