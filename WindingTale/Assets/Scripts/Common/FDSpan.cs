using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WindingTale.Common
{

    public class FDSpan
    {
        public int Min
        {
            get; private set;
        }

        public int Max
        {
            get; private set;
        }

        public FDSpan(int min, int max)
        {
            this.Min = min;
            this.Max = max;
        }

        public bool ContainsValue(int val)
        {
            return val >= Min && val <= Max;
        }
    }
}