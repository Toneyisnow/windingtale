using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace WindingTale.Core.Components.ActionStates
{
    public abstract class ActionState
    {
        protected IGameAction gameAction = null;

        protected IGameCallback gameCallback = null;

        public ActionState(IGameAction action, IGameCallback callback)
        {
            this.gameAction = action;
            this.gameCallback = callback;
        }
    }
}