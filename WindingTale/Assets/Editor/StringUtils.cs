using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets.Editor
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

            return number.ToString();
        }

        public static string Digit2(int number)
        {
            if (number < 10)
            {
                return string.Format(@"0{0}", number);
            }

            return number.ToString();
        }
    }
}
