using Assets.Scripts.Common;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WindingTale.Core.Components;
using WindingTale.Core.ObjectModels;

namespace WindingTale.UI.Components.Activities
{
    public class TalkActivity : ActivityBase
    {
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

        public TalkActivity(ConversationId cId)
        {
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
                gameInterface.ShowConversationDialog(this.ConversationId);
            }
            else if (this.MessageId != null)
            {
                gameInterface.ShowMessageDialog(this.Creature.Definition.AnimationId, this.MessageId);
            }


            this.HasFinished = true;
        }

        public override void Update(IGameInterface gameInterface)
        {
            
        }

    }
}