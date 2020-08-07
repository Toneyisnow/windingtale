using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WindingTale.UI.Common
{

    public class StringUtils
    {
        public static string FormatDigit3(int number)
        {
            if (number < 10)
            {
                return string.Format(@"00{0}", number);
            }

            if (number < 100)
            {
                return string.Format(@"0{0}", number);
            }

            return number.ToString();
        }
    }
}