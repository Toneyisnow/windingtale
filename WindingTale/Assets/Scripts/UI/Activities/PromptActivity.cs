using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WindingTale.Core.Common;
using WindingTale.UI.Scenes.Game;

namespace WindingTale.UI.Activities
{
    /// <summary>
    /// Show message with a YES or NO callback
    /// </summary>
    public class PromptActivity : ActivityBase
    {
        private Action<int> callback = null;


        public PromptActivity(Action<int> callback)
        {
            this.callback = callback;
        }

        public override void Start(IGameInterface gameInterface)
        {
            // gameInterface.ShowPromptDialog(pack.AnimationId, pack.Content);

            // TODO: Show prompt dialog

            int result = 1;

            this.callback(result);

            this.HasFinished = true;
        }

        public override void Update(IGameInterface gameInterface)
        {

        }
    }
}