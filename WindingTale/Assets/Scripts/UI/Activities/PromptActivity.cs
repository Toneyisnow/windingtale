using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WindingTale.Core.Common;
using WindingTale.Core.Components;
using WindingTale.Core.Components.Packs;

namespace WindingTale.UI.Components.Activities
{
    /// <summary>
    /// Show message with a YES or NO callback
    /// </summary>
    public class PromptActivity : ActivityBase
    {



        public PromptActivity(SelectionCallback callback)
        {
            this.callback = callback;
        }

        public override void Start(IGameInterface gameInterface)
        {
            // gameInterface.ShowPromptDialog(pack.AnimationId, pack.Content);
            this.HasFinished = true;
        }

        public override void Update(IGameInterface gameInterface)
        {

        }

    }
}