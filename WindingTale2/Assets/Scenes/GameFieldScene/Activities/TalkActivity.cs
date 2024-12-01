using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WindingTale.Core.Common;
using WindingTale.Core.Objects;

namespace WindingTale.Scenes.GameFieldScene.Activities
{
    public class TalkActivity : ActivityBase
    {

        /// <summary>
        /// The creature on the map that is talking. If null, it's the narrator.
        /// </summary>        
        private FDCreature creature = null;

        /// <summary>
        /// The raw text to be displayed.
        /// </summary>
        private string rawText = null;

        /// <summary>
        /// Need to confirm Yes or No
        /// </summary>
        private bool needConfirm = false;

        private Action<int> onSelected = null;

        private Nullable<int> selectedResult = null;


        /// <summary>
        /// Show the TalkDialog and wait for the user to click close or confirm.
        /// </summary>
        /// <param name="gameMain"></param>
        /// <param name="creature"></param>
        /// <param name="message"></param>
        public TalkActivity(FDMessage message, FDCreature creature = null, Action<int> onSelected = null)
        {
            this.creature = creature;
            this.onSelected = onSelected;

            //// this.rawText = message;
            ///
            this.needConfirm = message.MessageType == FDMessage.MessageTypes.Confirm;
        }

        public TalkActivity(Conversation conversation, FDCreature creature = null, Action<int> onSelected = null)
        {
            this.creature = creature;
            this.onSelected = onSelected;
            
            //// this.rawText = conversation;

            this.needConfirm = false;
        }

        public override void Start(GameMain gameMain)
        {
            /// TODO: move camera to the talking creature
            /// 

            int creatureAnimationId = this.creature?.Definition?.AnimationId ?? 0;
            gameMain.gameCanvas.ShowTalkDialog(creatureAnimationId, this.rawText, needConfirm, GameCanvas.DialogPosition.Bottom, 
                (result) =>
                {
                    selectedResult = result;
                });

            this.HasFinished = false;
        }

        // Update is called once per frame
        public override void Update(GameMain gameMain)
        {
            if (this.HasFinished)
            {
                return;
            }

            if (selectedResult.HasValue)
            {
                this.HasFinished = true;
                if (onSelected != null)
                {
                    onSelected(selectedResult.Value);
                }
            }
        }
    }
}