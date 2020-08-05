using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WindingTale.Core.Components;

namespace WindingTale.UI.Components.Activities
{
    public class CallbackActivity : ActivityBase
    {
        private Action<IGameCallback> callbackAction = null;

        public CallbackActivity(Action<IGameCallback> action)
        {
            this.callbackAction = action;
        }

        public override void Start(IGameCallback gameCallback)
        {
            if (callbackAction != null && gameCallback != null)
            {
                callbackAction(gameCallback);
            }

            this.HasFinished = true;
        }

        // Update is called once per frame
        public override void Update(IGameCallback gameCallback)
        {

        }
    }
}