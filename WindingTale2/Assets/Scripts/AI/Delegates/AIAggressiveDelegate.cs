using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WindingTale.Core.Algorithms;
using WindingTale.Core.Common;
using WindingTale.Core.Definitions;
using WindingTale.Core.Definitions.Items;
using WindingTale.Core.Objects;
using WindingTale.Scenes.GameFieldScene;

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
                gameMain.creatureRest(this.creature);
                return;
            }

            foreach(FDCreature en in this.gameMain.gameMap.Map.Enemies)
            {
                Debug.Log("TakeAction: enemy position: " + en.Position.ToString());
            }


            // Get target 
            FDCreature target = this.LookForAggressiveTarget();

            // According to the target, find the nearest position within the Move scope, and get the path to that position
            FDMovePath movePath = this.DecidePositionAndPath(target.Position);

            // Do the walk
            gameMain.creatureMove(creature, movePath);

            Debug.Log("AI Aggressive: creature=" + creature.Id + " position=" + creature.Position + " target pos=" + movePath?.Desitination?.ToString());

            FDPosition destination = movePath.Desitination ?? this.creature.Position;

            AttackItemDefinition item = this.creature.GetAttackItem();
            if (item != null)
            {
                FDSpan span = item.AttackScope;
                DirectRangeFinder finder = new DirectRangeFinder(this.gameMain.gameMap.Map.Field, destination, span.Max, span.Min);
                FDRange range = finder.CalculateRange();
                if (range.Contains(target.Position))
                {
                    // If in attack range, attack the target
                    this.gameMain.creatureAttack(this.creature, target);
                    return;
                }
            }
            else
            {
                this.gameMain.creatureRest(this.creature);
            }
        }
    }
}
