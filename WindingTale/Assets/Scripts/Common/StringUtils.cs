using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WindingTale.Common
{

    public class StringUtils
    {
        public static string Digit3(int number)
        {
            if (number < 10)
            {
                return string.Format(@"00{0}", number);
            }

            if (number < 100)
            {
                return string.Format(@"0{0}", number);
            }

            if (number > 999)
            {
                return "???";
            }

            return number.ToString();
        }

        public static string Digit2(int number)
        {
            if (number < 10)
            {
                return string.Format(@"0{0}", number);
            }

            if (number > 99)
            {
                return "??";
            }

            return number.ToString();
        }
    }
}