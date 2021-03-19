using WindingTale.Common;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WindingTale.Core.ObjectModels;

namespace WindingTale.Core.Components.Packs
{
    public class TalkPack : PackBase
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

        public TalkPack(FDCreature creature, ConversationId convId)
        {
            this.Type = PackType.Talk;
            this.Creature = creature;
            this.ConversationId = convId;
        }

        public TalkPack(FDCreature creature, MessageId mId)
        {
            this.Type = PackType.Talk;
            this.Creature = creature;
            this.MessageId = mId;
        }

    }
}