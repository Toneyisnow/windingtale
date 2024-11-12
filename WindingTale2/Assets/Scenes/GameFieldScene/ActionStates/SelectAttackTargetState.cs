using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WindingTale.Core.Algorithms;
using WindingTale.Core.Common;
using WindingTale.Core.Definitions;
using WindingTale.Core.Definitions.Items;
using WindingTale.Core.Objects;
using UnityEditor.SceneManagement;
using WindingTale.MapObjects.GameMap;
using UnityEngine.TestTools;
using WindingTale.MapObjects.CreatureIcon;

namespace WindingTale.Scenes.GameFieldScene.ActionStates
{
    public class SelectAttackTargetState : IActionState
    {
        public FDCreature Creature
        {
            get; private set;
        }

        public FDRange AttackRange
        {
            get; private set;
        }

        public SelectAttackTargetState(GameMain gameMain, FDCreature creature) : base(gameMain)
        {
            this.Creature = creature;
            this.AttackRange = null;
        }

        public override void onEnter()
        {
            if (this.AttackRange == null)
            {
                AttackItemDefinition attackItem = this.Creature.GetAttackItem();
                if (attackItem == null)
                {
                    return;
                }

                FDSpan span = attackItem.AttackScope;
                Debug.Log("AttackScope: " + span.Min + " " + span.Max);


                DirectRangeFinder finder = new DirectRangeFinder(fdMap.Field, this.Creature.Position, span.Max, span.Min);
                this.AttackRange = finder.CalculateRange();
            }

            // Display the attack range on the UI.
            gameMain.PushActivity((gameMain) =>
            {
                gameMain.gameMap.showActionTargetRange(this.Creature, AttackRange);
            });
        }

        public override void onExit()
        {
            // Clear move range on UI
            gameMain.gameMap.clearAllIndicators();
        }

        public override IActionState onSelectedPosition(FDPosition position)
        {
            if (this.AttackRange != null && this.AttackRange.Contains(position))
            {
                FDCreature target = fdMap.GetCreatureAt(position);
                if (target != null && target.Faction == CreatureFaction.Enemy)
                {
                    // Do the attack
                    this.gameMain.creatureAttackAsync(this.Creature, target);
                    return new IdleState(gameMain);
                }
                else
                {
                    // Clicked in the range, but no target, let the player to click again
                    return this;
                }
            }
            else
            {
                // Cancel
                return new IdleState(gameMain);
            }

        }

        

        public override IActionState onUserCancelled()
        {
            return new MenuActionState(gameMain, this.Creature, this.Creature.Position);
        }
    }
}
