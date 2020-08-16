using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WindingTale.Common;
using WindingTale.Core.Components.Algorithms;
using WindingTale.Core.Components.Packs;
using WindingTale.Core.ObjectModels;

namespace WindingTale.Core.Components.ActionStates
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

        public SelectAttackTargetState(IGameAction action, FDCreature creature) : base(action)
        {
            this.Creature = creature;
            this.AttackRange = null;
        }

        public override void OnEnter()
        {
            if (this.AttackRange == null)
            {
                AttackRangeFinder rangeFinder = new AttackRangeFinder(this.gameAction);
                this.AttackRange = rangeFinder.FindRange(this.Creature);
            }

            // Display the attack range on the UI.
            ShowRangePack pack = new ShowRangePack(this.AttackRange);
            SendPack(pack);
        }

        public override void OnExit()
        {
            ClearRangePack pack = new ClearRangePack();
            SendPack(pack);
        }

        public override StateOperationResult OnSelectPosition(FDPosition position)
        {
            if (this.AttackRange != null && this.AttackRange.Contains(position))
            {
                FDCreature target = this.gameAction.GetCreatureAt(position);
                if (target != null && target.Faction == Definitions.CreatureFaction.Enemy)
                {
                    // Do the attack
                    this.gameAction.DoCreatureAttack(this.Creature.CreatureId, position);
                    return StateOperationResult.Clear();
                }
                else
                {
                    // Clicked in the range, but no target, let the player to click again
                    return StateOperationResult.None();
                }
            }
            else
            {
                // Clicked out of range, cancel
                return StateOperationResult.Pop();
            }

        }
    }
}