using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WindingTale.Core.Components;

namespace WindingTale.UI.Components.Activities
{
    public class CallbackActivity : ActivityBase
    {
        private Action<IGameInterface> callbackAction = null;

        public CallbackActivity(Action<IGameInterface> action)
        {
            this.callbackAction = action;
        }

        public override void Start(IGameInterface gameInterface)
        {
            if (callbackAction != null && gameInterface != null)
            {
                callbackAction(gameInterface);
            }

            this.HasFinished = true;
        }

        // Update is called once per frame
        public override void Update(IGameInterface gameInterface)
        {

        }
    }
}