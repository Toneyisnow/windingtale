using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace WindingTale.Core.Common
{
    public class FDRandom
    {
        /// <summary>
        /// Rate is from 0 - 100
        /// </summary>
        /// <param name="rate"></param>
        /// <returns></returns>
        public static bool BoolFromRate(int rate)
        {
            return UnityEngine.Random.Range(0, 100) < rate;
        }

        public static int IntFromSpan(FDSpan span)
        {
            return span.Min + UnityEngine.Random.Range(0, 1) * (span.Max - span.Min);
        }

        public static int IntFromSpan(int min, int max)
        {
            return min + UnityEngine.Random.Range(0, 1) * (max - min);
        }

    }
}
