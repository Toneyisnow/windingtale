using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using WindingTale.Core.Common;
using WindingTale.Core.Definitions;
using WindingTale.Core.Objects;
using WindingTale.UI.Scenes;
using WindingTale.UI.Scenes.Game;

namespace WindingTale.UI.Activities
{
    public class TalkActivity : ActivityBase
    {
        /// <summary>
        /// If the creature is null, then it is a system message;
        /// If the creature is NPC or Enemy, then show the dialog on the top
        /// </summary>
        public FDCreature Creature
        {
            get; private set;
        }

        public Conversation ConversationId
        {
            get; private set;
        }

        public FDMessage MessageId
        {
            get; private set;
        }

        public TalkActivity()
        {

        }

        public TalkActivity(Conversation cId, FDCreature creature = null)
        {
            this.Creature = creature;
            this.ConversationId = cId;
        }

        public TalkActivity(FDMessage messageId, FDCreature creature = null)
        {
            if (messageId.MessageType == FDMessage.MessageTypes.Confirm)
            {
                throw new System.Exception("Talk activity could only handle informational messages, not confirm messages.");
            }

            this.Creature = creature;
            this.MessageId = messageId;
        }

        public override void Start(GameObject gameInterface)
        {
            Debug.Log("Start Talk activity");

            GameFieldScene gameFieldScene = gameInterface.GetComponent<GameFieldScene>();
            GameCanvas gameCanvas = gameFieldScene.canvas.GetComponent<GameCanvas>();
            gameCanvas.ShowMessageDialog();

            // Cursor move to the current talking creature, and show talk dialog
            if (this.ConversationId != null)
            {


                //// gameInterface.ShowConversationDialog(this.Creature, this.ConversationId);
            }
            else if (this.MessageId != null)
            {
                //// gameInterface.ShowMessageDialog(this.Creature, this.MessageId);
            }

            this.HasFinished = true;
        }

        public override void Update(GameObject gameInterface)
        {
            
        }

    }
}