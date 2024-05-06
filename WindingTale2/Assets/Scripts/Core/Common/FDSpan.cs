using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WindingTale.Core.Common
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

        public override string ToString()
        {
            return string.Format("[{0}, {1}]", this.Min, this.Max);
        }
    }
}