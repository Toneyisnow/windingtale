using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WindingTale.Common;
using WindingTale.Core.ObjectModels;

namespace WindingTale.Core.Components.ActionStates
{
    public class SelectItemUseTargetState : ActionState
    {

        public FDCreature Creature
        {
            get; private set;
        }

        public int CreatureId
        {
            get; private set;
        }

        public int SelectedItemIndex
        {
            get; private set;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="gameAction"></param>
        public SelectItemUseTargetState(IGameAction gameAction, int creatureId, int itemIndex) : base(gameAction)
        {
            this.CreatureId = creatureId;
            this.Creature = gameAction.GetCreature(creatureId);
            this.SelectedItemIndex = itemIndex;

        }

        public override void OnEnter()
        {
            base.OnEnter();
        }

        public override void OnExit()
        {
            base.OnExit();
        }

        public override StateOperationResult OnSelectPosition(FDPosition position)
        {
            // Selecte position must be next to current creature
            if (!this.Creature.Position.IsNextTo(position))
            {
                return StateOperationResult.None();
            }

            // No creature or not a friend/NPC
            FDCreature targetCreature = this.gameAction.GetCreatureAt(position);
            if(targetCreature == null || targetCreature.Faction == Definitions.CreatureFaction.Enemy)
            {
                return StateOperationResult.None();
            }

            gameAction.DoCreatureUseItem(this.CreatureId, this.SelectedItemIndex, targetCreature.CreatureId);
            return StateOperationResult.Clear();
        }
    }
}