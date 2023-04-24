using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WindingTale.Common
{
    public class MessageId
    {
        public enum MessageTypes
        {
            Confirm,
            Message,
        }

        public int MessageKey
        {
            get; private set;
        }

        public MessageTypes MessageType
        {
            get; private set;
        }

        public int IntParam1
        {
            get; private set;
        }

        public int IntParam2
        {
            get; private set;
        }

        public int IntParam3
        {
            get; private set;
        }

        public static MessageId Create(MessageTypes type, int key, int iParam1 = 0, int iParam2 = 0, int iParam3 = 0)
        {
            MessageId result = new MessageId();

            result.MessageType = type;
            result.MessageKey = key;
            result.IntParam1 = iParam1;
            result.IntParam2 = iParam2;
            result.IntParam3 = iParam3;

            return result;
        }

    }
}
