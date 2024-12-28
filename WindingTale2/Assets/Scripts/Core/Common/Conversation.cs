using System;
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

        public int ConversationId
        {
            get; private set;
        }

        public int SequenceId
        {
            get; private set;
        }


        public static Conversation Create(int cId, int convId, int sId)
        {
            Conversation conversation = new Conversation();

            conversation.ChapterId = cId;
            conversation.ConversationId = convId;
            conversation.SequenceId = sId;

            return conversation;
        }

        public string GetKeyForId()
        {
            string key = string.Format(@"Chapter_{0}-{0}-{1}-{2}-Id",
                StringUtils.Digit2(this.ChapterId), StringUtils.Digit2(this.ConversationId), StringUtils.Digit3(this.SequenceId));
            return key;
        }

    }
}
