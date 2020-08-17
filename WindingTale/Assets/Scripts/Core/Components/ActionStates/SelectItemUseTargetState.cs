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
            throw new System.NotImplementedException();
        }

        public override void OnExit()
        {
            throw new System.NotImplementedException();
        }

        public override StateOperationResult OnSelectPosition(FDPosition position)
        {
            throw new System.NotImplementedException();
        }
    }
}