using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WindingTale.Core.Common
{

    public class StringUtils
    {
        /// <summary>
        /// Zero-pads to at least 3 digits: 7 -> "007", 16 -> "016", 1016 -> "1016".
        /// Numbers above 999 keep all their digits -- they are legitimate ids (creature
        /// definitions in the 1xxx range, for one) and the keys built from them
        /// ("Creature-1016", object names, resource paths) must stay distinct.
        /// </summary>
        public static string Digit3(int number)
        {
            return number.ToString("D3");
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