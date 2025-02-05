using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WindingTale.Core.Definitions;
using WindingTale.Core.Definitions.Items;
using WindingTale.Core.Objects;

namespace WindingTale.Core.Algorithms
{
    public class CreatureFormula
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="hp"></param>
        /// <param name="hpMax"></param>
        /// <returns></returns>
        public static int GetRestRecoveredHp(int hp, int hpMax)
        {
            int result = hp + (int)(hpMax * 0.2);
            if (result > hpMax)
            {
                result = hpMax;
            }

            return result;
        }

        /// <summary>
        /// Only Consumable Items or Special Items might be used here.
        /// </summary>
        /// <param name="creature"></param>
        /// <param name="itemId"></param>
        public static void TakeItemEffect(FDCreature creature, int itemId)
        {
            if (itemId == 0)
            {
                return;
            }

            ItemDefinition item = DefinitionStore.Instance.GetItemDefinition(itemId);
            if (item == null || 
                (item.GetItemType() != ItemDefinition.ItemType.Consumable)
                && (item.GetItemType() != ItemDefinition.ItemType.Special)
            )
            {
                return;
            }

            if (item.GetItemType() == ItemDefinition.ItemType.Consumable)
            {
                ConsumableItemDefinition consumable = item as ConsumableItemDefinition;

                switch(consumable.UseType)
                {
                    case ItemUseType.Hp:
                        creature.UpdateHp(consumable.Quantity);
                        break;
                    case ItemUseType.Mp:
                        creature.UpdateMp(consumable.Quantity);
                        break;
                    default:
                        break;
                }
                return;
            }

            if (item.GetItemType() == ItemDefinition.ItemType.Special)
            {
                SpecialItemDefinition special = item as SpecialItemDefinition;
                
            }
        }
    }
}
