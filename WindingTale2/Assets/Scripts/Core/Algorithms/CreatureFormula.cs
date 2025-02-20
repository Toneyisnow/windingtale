using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WindingTale.Core.Common;
using WindingTale.Core.Definitions;
using WindingTale.Core.Definitions.Items;
using WindingTale.Core.Map;
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

        /// <summary>
        /// Get Ap according to the creature's status, and map's block
        /// </summary>
        /// <param name="creature"></param>
        /// <returns></returns>
        public static int GetCalculatedAp(FDCreature creature, FDMap map)
        {
            FDPosition position = creature.Position;
            ShapeDefinition shape = map.Field.GetShapeAt(position);

            AttackItemDefinition attackItem = creature.GetAttackItem();
            if (attackItem == null)
            {
                return 0;
            }
            return creature.Ap + attackItem.Ap;
        }


        /// <summary>
        /// Get Dp according to the creature's status, and map's block
        /// </summary>
        /// <param name="creature"></param>
        /// <returns></returns>
        public static int GetCalculatedDp(FDCreature creature, FDMap map)
        {
            FDPosition position = creature.Position;
            ShapeDefinition shape = map.Field.GetShapeAt(position);
            
            AttackItemDefinition attackItem = creature.GetAttackItem();
            DefendItemDefinition defendItem = creature.GetDefendItem();

            int result = creature.Dp + defendItem.Dp;
            if (attackItem != null)
            {
                result += attackItem.Dp;
            }
            return result;
        }


        /// <summary>
        /// Get Dx according to the creature's status, and map's block
        /// </summary>
        /// <param name="creature"></param>
        /// <returns></returns>
        public static int GetCalculatedDx(FDCreature creature, FDMap map)
        {
            
            return creature.Dx;
        }


        /// <summary>
        /// Get Ap according to the creature's status, and map's block
        /// </summary>
        /// <param name="creature"></param>
        /// <returns></returns>
        public static int GetCalculatedHit(FDCreature creature, FDMap map)
        {
            int effectDx = GetCalculatedDx(creature, map);

            AttackItemDefinition item = creature.GetAttackItem();
            if (item != null)
            {
                return item.Hit + effectDx;
            }
            return effectDx;
        }


        /// <summary>
        /// Get Ev according to the creature's status, and map's block
        /// </summary>
        /// <param name="creature"></param>
        /// <returns></returns>
        public static int GetCalculatedEv(FDCreature creature, FDMap map)
        {
            int effectDx = GetCalculatedDx(creature, map);

            DefendItemDefinition defendItem = creature.GetDefendItem();
            if (defendItem != null)
            {
                return effectDx + defendItem.Ev;
            }

            return effectDx;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="creature"></param>
        /// <returns></returns>
        public static LevelUpInfo ComposeLevelUp(FDCreature creature)
        {
            LevelUpInfo result = new LevelUpInfo();
            result.ImprovedAp = 0;
            result.ImprovedDp = 0;
            result.ImprovedDx = 0;
            result.ImprovedHp = 0;
            result.ImprovedMp = 0;

            return result;
        }


    }
}
