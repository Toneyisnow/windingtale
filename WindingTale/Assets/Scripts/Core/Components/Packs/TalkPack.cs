using Assets.Scripts.Common;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WindingTale.Core.Components.Packs
{
    public class TalkPack : PackBase
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

        public TalkPack(ConversationId convId)
        {
            this.Type = PackType.Talk;
            this.ConversationId = convId;
        }

        public TalkPack(int creatureId, MessageId mId)
        {
            this.Type = PackType.Talk;
            this.CreatureId = creatureId;
            this.MessageId = mId;
        }

    }
}