using Assets.Scripts.Common;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WindingTale.Core.Components;

namespace WindingTale.UI.Components.Activities
{
    public class TalkActivity : ActivityBase
    {
        public int CreatureId
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

        public TalkActivity(int creatureId, MessageId mId)
        {
            this.CreatureId = creatureId;
            this.MessageId = mId;
        }

        public override void Start(IGameInterface gameInterface)
        {
            // Cursor move to the current talking creature, and show talk dialog

            this.HasFinished = true;
        }

        public override void Update(IGameInterface gameInterface)
        {
            
        }

    }
}