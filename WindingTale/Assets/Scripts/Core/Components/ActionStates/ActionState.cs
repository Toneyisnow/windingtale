using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WindingTale.Common;
using WindingTale.Core.Components.Packs;

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

        public abstract StateOperationResult OnSelectPosition(FDPosition position);

        public virtual StateOperationResult OnSelectIndex(int index)
        {
            return StateOperationResult.None();
        }

        protected void SendPack(PackBase pack)
        {
            if (this.gameAction != null && this.gameAction.GetCallback() != null)
            {
                this.gameAction.GetCallback().OnCallback(pack);
            }

        }

        #endregion
    }
}