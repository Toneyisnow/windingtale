using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using WindingTale.Common;
using WindingTale.Core.Components;
using WindingTale.Core.ObjectModels;

namespace WindingTale.UI.Components.Activities
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

        public ConversationId ConversationId
        {
            get; private set;
        }

        public MessageId MessageId
        {
            get; private set;
        }

        public TalkActivity()
        {

        }

        public TalkActivity(FDCreature creature, ConversationId cId)
        {
            this.Creature = creature;
            this.ConversationId = cId;
        }

        public TalkActivity(FDCreature creature, MessageId mId)
        {
            this.Creature = creature;
            this.MessageId = mId;
        }

        public override void Start(IGameInterface gameInterface)
        {
            // Cursor move to the current talking creature, and show talk dialog

            if (this.ConversationId != null)
            {
                gameInterface.ShowConversationDialog(this.Creature, this.ConversationId);
            }
            else if (this.MessageId != null)
            {
                gameInterface.ShowMessageDialog(this.Creature, this.MessageId);
            }

            this.HasFinished = true;
        }

        public override void Update(IGameInterface gameInterface)
        {
            
        }

    }
}