using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets.Scripts.Common
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

        public string StringParam1
        {
            get; private set;
        }

        public string StringParam2
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

        public static MessageId Create(MessageTypes type, int key, string sParam1 = "", string sParam2 = "", int iParam1 = 0, int iParam2 = 0)
        {
            MessageId result = new MessageId();

            result.MessageType = type;
            result.MessageKey = key;
            result.StringParam1 = sParam1;
            result.StringParam2 = sParam2;
            result.IntParam1 = iParam1;
            result.IntParam2 = iParam2;

            return result;
        }

    }
}
