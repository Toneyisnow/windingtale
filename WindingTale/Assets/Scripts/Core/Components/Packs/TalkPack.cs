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

        public string Conversationid
        {
            get; private set;
        }

        public TalkPack(string convId)
        {
            this.Conversationid = convId;
        }

    }
}