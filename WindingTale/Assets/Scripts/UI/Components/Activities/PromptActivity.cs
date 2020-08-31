using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WindingTale.Core.Components;
using WindingTale.Core.Components.Packs;

namespace WindingTale.UI.Components.Activities
{
    public class PromptActivity : ActivityBase
    {
        private PromptPack pack = null;

        public PromptActivity(PromptPack pack)
        {
            this.pack = pack;
        }

        public override void Start(IGameInterface gameInterface)
        {
            gameInterface.ShowPromptyDialog(pack.AnimationId, pack.Content);
            this.HasFinished = true;
        }

        public override void Update(IGameInterface gameInterface)
        {

        }

    }
}