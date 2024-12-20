﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WindingTale.Core.Common
{
    public class Conversation
    {
        public int ChapterId
        {
            get; private set;
        }

        public int SequenceId
        {
            get; private set;
        }

        public int Index
        {
            get; private set;
        }

        public static Conversation Create(int cId, int sId, int index)
        {
            Conversation conversation = new Conversation();

            conversation.ChapterId = cId;
            conversation.SequenceId = sId;
            conversation.Index = index;

            return conversation;
        }

        public string GetKeyForId()
        {
            string key = string.Format(@"Chapter_{0}-{0}-{1}-{2}-Id",
                StringUtils.Digit2(this.ChapterId), StringUtils.Digit2(this.SequenceId), StringUtils.Digit3(this.Index));
            return key;
        }

    }
}
