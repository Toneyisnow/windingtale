using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WindingTale.Common;
using WindingTale.Core.Common;
using WindingTale.Core.Objects;
using WindingTale.UI.Scenes.Game;

namespace WindingTale.UI.Activities
{
    /// <summary>
    /// Show message with a YES or NO callback
    /// </summary>
    public class PromptActivity : ActivityBase
    {
        private Action<int> callback = null;

        public FDCreature Creature { get; private set; }

        private FDMessage message = null;

        public PromptActivity(FDMessage message, Action<int> callback, FDCreature creature = null)
        {
            this.message = message;
            this.Creature = creature;
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