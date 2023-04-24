using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WindingTale.Core.Components.Packs
{

    public class IdlePack : PackBase
    {
        public int TimeUnit
        {
            get; private set;
        }

        public static IdlePack FromTimeUnit(int unit)
        {
            IdlePack pack = new IdlePack();
            pack.TimeUnit = unit;
            return pack;
        }
    }
}