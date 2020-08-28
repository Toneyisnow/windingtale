using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WindingTale.Common;

namespace WindingTale.Core.Components.ActionStates
{
    public class SelectItemUseTargetState : ActionState
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="gameAction"></param>
        public SelectItemUseTargetState(IGameAction gameAction) : base(gameAction)
        {

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