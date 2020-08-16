using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WindingTale.Common;
using WindingTale.Core.Definitions;
using WindingTale.Core.ObjectModels;

namespace WindingTale.Core.Components.ActionStates
{
    /// <summary>
    /// 
    /// </summary>
    public class SelecteMagicTargetState : ActionState
    {
        public FDCreature Creature
        {
            get; private set;
        }

        public MagicDefinition Magic
        {
            get; private set;
        }

        public SelecteMagicTargetState(IGameAction action, FDCreature creature, MagicDefinition magic) : base(action)
        {
            this.Creature = creature;
            this.Magic = magic;
        }

        public override void OnEnter()
        {
            Debug.Log("SelecteMagicTargetState: OnEnter.");
        }

        public override void OnExit()
        {
            Debug.Log("SelecteMagicTargetState: OnExit.");
        }

        public override StateOperationResult OnSelectPosition(FDPosition position)
        {
            throw new System.NotImplementedException();
        }
    }
}