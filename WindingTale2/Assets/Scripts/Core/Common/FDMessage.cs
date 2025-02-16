using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WindingTale.Core.Common
{
    public class FDMessage
    {
        public enum MessageTypes
        {
            Confirm,
            Information,
        }

        public int Key
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

        public string StrParam1
        {
            get; private set;
        }

        public string StrParam2
        {
            get; private set;
        }

        public static FDMessage Create(MessageTypes type, int key, int iParam1 = 0, int iParam2 = 0, string strParam1 = "", string strParam2 = "")
        {
            FDMessage result = new FDMessage();

            result.MessageType = type;
            result.Key = key;
            result.IntParam1 = iParam1;
            result.IntParam2 = iParam2;
            result.StrParam1 = strParam1;
            result.StrParam2 = strParam2;

            return result;
        }

    }
}
