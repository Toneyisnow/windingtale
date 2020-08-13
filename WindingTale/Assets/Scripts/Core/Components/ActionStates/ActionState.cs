using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WindingTale.Common;

namespace WindingTale.Core.Components.ActionStates
{
    public abstract class ActionState
    {
        protected IGameAction gameAction = null;


        public ActionState(IGameAction action)
        {
            this.gameAction = action;
        }

        public abstract void OnEnter();

        public abstract void OnExit();

        #region State Operations

        public abstract void OnSelectPosition(FDPosition position);



        #endregion
    }
}