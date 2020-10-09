using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets.Scripts.Common
{
    public class ConversationId
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

        public static ConversationId Create(int cId, int sId, int index)
        {
            ConversationId conversation = new ConversationId();

            conversation.ChapterId = cId;
            conversation.SequenceId = sId;
            conversation.Index = index;

            return conversation;
        }
    }
}
