using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using WindingTale.Common;
using WindingTale.Core.Objects;
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

        public Message MessageId
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

        public TalkActivity(Message messageId, FDCreature creature = null)
        {
            if (messageId.MessageType == Message.MessageTypes.Confirm)
            {
                throw new System.Exception("Talk activity could only handle informational messages, not confirm messages.");
            }

            this.Creature = creature;
            this.MessageId = messageId;
        }

        public override void Start(IGameInterface gameInterface)
        {
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

        public override void Update(IGameInterface gameInterface)
        {
            
        }

    }
}