using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WindingTale.Common;

namespace WindingTale.Core.Components.ActionStates
{
    public class ActionMenuState : ActionState
    {
        public ActionMenuState(IGameAction action)
            : base(action)
        {

        }

        public override void OnEnter()
        {
            // Nothing
        }

        public override void OnExit()
        {
            // Nothing
        }

        public override void OnSelectPosition(FDPosition position)
        {
            
        }
    }
}
