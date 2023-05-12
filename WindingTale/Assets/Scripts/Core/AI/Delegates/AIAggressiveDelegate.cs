using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WindingTale.Core.Algorithms;
using WindingTale.Core.Common;
using WindingTale.Core.Components;
using WindingTale.Core.Components.Algorithms;
using WindingTale.Core.Definitions;
using WindingTale.Core.Definitions.Items;
using WindingTale.Core.Objects;
using WindingTale.UI.Scenes.Game;

namespace WindingTale.AI.Delegates
{
    public class AIAggressiveDelegate : AIDelegate
    {
        public AIAggressiveDelegate(GameMain gameMain, FDAICreature c) : base(gameMain, c)
        {

        }

        public override void TakeAction()
        {
            if (this.NeedAndCanRecover())
            {
                this.gameMain.CreatureRest(this.creature);
                return;
            }

            // Get target 
            FDCreature target = this.LookForAggressiveTarget();

            // According to the target, find the nearest position within the Move scope, and get the path to that position
            FDMovePath movePath = this.DecidePositionAndPath(target.Position);

            // Do the walk
            this.gameMain.CreatureMove(creature, movePath);

            FDPosition destination = movePath.Desitination ?? this.creature.Position;

            AttackItemDefinition item = this.creature.GetAttackItem();
            if (item != null)
            {
                FDSpan span = item.AttackScope;
                DirectRangeFinder finder = new DirectRangeFinder(this.gameMain.GameMap.Field, destination, span.Max, span.Min);
                FDRange range = finder.CalculateRange();
                if (range.Contains(target.Position))
                {
                    // If in attack range, attack the target
                    this.gameMain.CreatureAttack(this.creature, target);
                    return;
                }
            }

            this.gameMain.CreatureRest(this.creature);
        }
    }
}
