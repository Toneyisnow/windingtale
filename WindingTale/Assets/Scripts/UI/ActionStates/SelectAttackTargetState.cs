using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WindingTale.Core.Algorithms;
using WindingTale.Core.Common;
using WindingTale.Core.Definitions;
using WindingTale.Core.Definitions.Items;
using WindingTale.Core.Objects;
using WindingTale.UI.Activities;
using WindingTale.UI.Scenes.Game;

namespace WindingTale.UI.ActionStates
{
    public class SelectAttackTargetState : ActionState
    {
        public FDCreature Creature
        {
            get; private set;
        }

        public FDRange AttackRange
        {
            get; private set;
        }

        public SelectAttackTargetState(GameMain gameMain, IStateResultHandler stateHandler, FDCreature creature) : base(gameMain, stateHandler)
        {
            this.Creature = creature;
            this.AttackRange = null;
        }

        public override void OnEnter()
        {
            base.OnEnter();

            if (this.AttackRange == null)
            {
                AttackItemDefinition attackItem = this.Creature.GetAttackItem();
                if (attackItem == null)
                {
                    return;
                }
                
                FDSpan span = attackItem.AttackScope;
                Debug.Log("AttackScope: " + span.Min + " " + span.Max);


                DirectRangeFinder finder = new DirectRangeFinder(gameMap.Field, this.Creature.Position, span.Max, span.Min);
                this.AttackRange = finder.CalculateRange();
            }

            // Display the attack range on the UI.
            ShowRangeActivity activity = new ShowRangeActivity(this.AttackRange.ToList());
            activityManager.Push(activity);
        }

        public override void OnExit()
        {
            
            ClearRangeActivity activity = new ClearRangeActivity();
            activityManager.Push(activity);

            base.OnExit();
        }

        public override void OnSelectPosition(FDPosition position)
        {
            if (this.AttackRange != null && this.AttackRange.Contains(position))
            {
                FDCreature target = gameMap.GetCreatureAt(position);
                if (target != null && target.Faction == CreatureFaction.Enemy)
                {
                    // Do the attack
                    this.gameMain.CreatureAttack(this.Creature, target);
                    stateHandler.HandleClearStates();
                }
                else
                {
                    // Clicked in the range, but no target, let the player to click again
                }
            }
            else
            {
                // Clicked out of range, cancel
                stateHandler.HandlePopState();
            }

        }
    }
}