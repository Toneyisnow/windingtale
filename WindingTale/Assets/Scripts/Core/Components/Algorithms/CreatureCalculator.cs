using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WindingTale.Core.Components.Algorithms
{
    public class CreatureCalculator
    {
        public static int GetRestRecoveredHp(int hp, int hpMax)
        {
            int result = hp + (int)(hpMax * 0.2);
            if (result > hpMax)
            {
                result = hpMax;
            }

            return result;
        }



    }
}
