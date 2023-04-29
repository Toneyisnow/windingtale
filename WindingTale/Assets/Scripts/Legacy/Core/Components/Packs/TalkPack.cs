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

        public Conversation ConversationId
        {
            get; private set;
        }

        public Message MessageId
        {
            get; private set;
        }

        public TalkPack(FDCreature creature, Conversation convId)
        {
            this.Type = PackType.Talk;
            this.Creature = creature;
            this.ConversationId = convId;
        }

        public TalkPack(FDCreature creature, Message mId)
        {
            this.Type = PackType.Talk;
            this.Creature = creature;
            this.MessageId = mId;
        }

    }
}