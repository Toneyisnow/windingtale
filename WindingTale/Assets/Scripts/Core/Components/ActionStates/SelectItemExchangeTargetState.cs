using System.Collections;
using System.Collections.Generic;
using System.Security.Principal;
using UnityEngine;
using WindingTale.Common;
using WindingTale.Core.ObjectModels;

namespace WindingTale.Core.Components.ActionStates
{
    public class SelectItemExchangeTargetState : ActionState
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
        public SelectItemExchangeTargetState(IGameAction gameAction, int creatureId, int itemIndex) : base(gameAction)
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
            return StateOperationResult.None();
        }
    }
}