using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WindingTale.Core.Components;
using WindingTale.UI.Scenes.Game;

namespace WindingTale.UI.Activities
{
    public class CallbackActivity : ActivityBase
    {
        //// private Action<IGameInterface> callbackAction = null;
        private Action callback = null;

        public CallbackActivity(Action action)
        {
            this.callback = action;
        }

        public override void Start(GameObject gameInterface)
        {
            if (callback != null && gameInterface != null)
            {
                callback();
            }

            this.HasFinished = true;
        }

        // Update is called once per frame
        public override void Update(GameObject gameInterface)
        {

        }

    }
}