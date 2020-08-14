using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WindingTale.Common;

namespace WindingTale.Core.Components.ActionStates
{
    public abstract class ActionState
    {
        protected IGameAction gameAction = null;

        /// <summary>
        /// This is the delegate function that will be called after interface passed in the index value
        /// </summary>
        protected Func<int, StateOperationResult> SelectIndexDelegate = null;

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
            if (this.SelectIndexDelegate != null)
            {
                return this.SelectIndexDelegate(index);
            }

            return StateOperationResult.None();
        }

        #endregion
    }
}