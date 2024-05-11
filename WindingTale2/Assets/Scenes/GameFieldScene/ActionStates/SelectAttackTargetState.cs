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


                DirectRangeFinder finder = new DirectRangeFinder(map.Field, this.Creature.Position, span.Max, span.Min);
                this.AttackRange = finder.CalculateRange();
            }

            // Display the attack range on the UI.
            //ShowRangeActivity activity = new ShowRangeActivity(this.AttackRange.ToList());
            //activityManager.Push(activity);
        }

        public override void onExit()
        {
            //ClearRangeActivity activity = new ClearRangeActivity();
            //activityManager.Push(activity);
        }

        public override IActionState onSelectedPosition(FDPosition position)
        {
            if (this.AttackRange != null && this.AttackRange.Contains(position))
            {
                FDCreature target = map.GetCreatureAt(position);
                if (target != null && target.Faction == CreatureFaction.Enemy)
                {
                    // Do the attack
                    //this.gameMain.CreatureAttack(this.Creature, target);
                    //stateHandler.HandleClearStates();
                }
                else
                {
                    // Clicked in the range, but no target, let the player to click again
                }
            }
            else
            {
                // Clicked out of range, cancel
                //// stateHandler.HandlePopState();
            }

            return this;

        }

        

        public override IActionState onUserCancelled()
        {
            return this;
        }
    }
}
