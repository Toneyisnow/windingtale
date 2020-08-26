using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WindingTale.Common;

namespace WindingTale.Core.Components.Packs
{
    public class ShowRangePack : PackBase
    {
        public FDRange Range
        {
            get; private set;
        }

        public ShowRangePack(FDRange range)
        {
            this.Type = PackType.ShowRange;
            this.Range = range;
        }
    }
}