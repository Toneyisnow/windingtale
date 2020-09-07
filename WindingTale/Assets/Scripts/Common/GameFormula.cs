using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WindingTale.Core.Components;
using WindingTale.Core.Components.Data;

namespace WindingTale.Common
{
    public class GameFormula
    {
        public static int CalculateAp(CreatureData creatureData)
        {
            return creatureData.Ap;
        }

        public static int CalculateDp(CreatureData creatureData)
        {
            return creatureData.Dp;
        }

        public static int CalculateDx(CreatureData creatureData)
        {
            return creatureData.Ex;
        }

        public static int CalculateEx(CreatureData creatureData)
        {
            return creatureData.Ex;
        }

    }
}