using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WindingTale.Core.Components;
using WindingTale.Core.Components.Data;
using WindingTale.Core.Definitions;
using WindingTale.Core.Definitions.Items;

namespace WindingTale.Common
{
    public class GameFormula
    {
        public static int CalculateAp(CreatureData creatureData)
        {
            int delta = 0;
            AttackItemDefinition attackItem = creatureData.GetAttackItem();
            if (attackItem != null)
            {
                delta = attackItem.Ap;
            }

            int total = creatureData.Ap + delta;
            if (creatureData.Effects.Contains(CreatureData.CreatureEffects.EnhancedAp))
            {
                total = (int)(total * 1.15f);
            }

            return total;
        }

        public static int CalculateDp(CreatureData creatureData)
        {
            int delta = 0;
            AttackItemDefinition attackItem = creatureData.GetAttackItem();
            if (attackItem != null)
            {
                delta = attackItem.Dp;
            }

            DefendItemDefinition defendItem = creatureData.GetDefendItem();
            if (defendItem != null)
            {
                delta = defendItem.Dp;
            }

            int total = creatureData.Dp + delta;
            if (creatureData.Effects.Contains(CreatureData.CreatureEffects.EnhancedDp))
            {
                total = (int)(total * 1.15f);
            }

            return total;
        }

        public static int CalculateEv(CreatureData creatureData)
        {
            int delta = 0;
            AttackItemDefinition attackItem = creatureData.GetAttackItem();
            if (attackItem != null)
            {
                delta = attackItem.Ev;
            }

            DefendItemDefinition defendItem = creatureData.GetDefendItem();
            if (defendItem != null)
            {
                delta = defendItem.Ev;
            }

            return creatureData.Dx + delta;
        }
        public static int CalculateHit(CreatureData creatureData)
        {
            AttackItemDefinition attackItem = creatureData.GetAttackItem();
            return creatureData.Dx + attackItem.Hit;
        }

    }
}