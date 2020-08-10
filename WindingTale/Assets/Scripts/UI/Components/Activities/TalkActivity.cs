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

        public string ConversationId
        {
            get; private set;
        }

        public TalkActivity()
        {

        }

        public TalkActivity(int creatureId, string conversationid)
        {
            this.CreatureId = creatureId;
            this.ConversationId = conversationid;
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