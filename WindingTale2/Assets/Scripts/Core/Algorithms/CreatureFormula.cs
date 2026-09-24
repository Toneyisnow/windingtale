using System;
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
            // map is null when the creature is shown outside a battle (e.g. the shop's
            // creature picker); the shape it would give is unused here anyway.
            ShapeDefinition shape = map != null ? map.Field.GetShapeAt(position) : null;

            AttackItemDefinition attackItem = creature.GetAttackItem();
            if (attackItem == null)
            {
                return 0;
            }
            return creature.Ap + attackItem.Ap;
        }


        /// <summary>
        /// A stat raised or lowered by a terrain percentage: 200 on a tile that gives
        /// "AP+05" is 200 + 200 * 5% = 210. Works for attack and defense alike, and for
        /// negative percentages.
        /// </summary>
        public static int AdjustByTerrain(int value, int percent)
        {
            return value + value * percent / 100;
        }

        /// <summary>
        /// The creature's attack power (items and effects included) once the terrain it
        /// stands on is counted: CalculatedAp adjusted by the tile's AP percentage
        /// (ShapeDefinition.AdjustedAp). A null shape (off the map) adds nothing.
        /// </summary>
        public static int GetTerrainAdjustedAp(FDCreature creature, ShapeDefinition shape)
        {
            return AdjustByTerrain(creature.CalculatedAp, shape != null ? shape.AdjustedAp : 0);
        }

        /// <summary>
        /// The creature's defense power (items and effects included) once the terrain it
        /// stands on is counted: CalculatedDp adjusted by the tile's DP percentage
        /// (ShapeDefinition.AdjustedDp). A null shape (off the map) adds nothing.
        /// </summary>
        public static int GetTerrainAdjustedDp(FDCreature creature, ShapeDefinition shape)
        {
            return AdjustByTerrain(creature.CalculatedDp, shape != null ? shape.AdjustedDp : 0);
        }

        /// <summary>The same, for the tile the creature is standing on right now.</summary>
        public static int GetTerrainAdjustedAp(FDCreature creature, FDMap map)
        {
            return GetTerrainAdjustedAp(creature, GetShapeUnder(creature, map));
        }

        /// <summary>The same, for the tile the creature is standing on right now.</summary>
        public static int GetTerrainAdjustedDp(FDCreature creature, FDMap map)
        {
            return GetTerrainAdjustedDp(creature, GetShapeUnder(creature, map));
        }

        private static ShapeDefinition GetShapeUnder(FDCreature creature, FDMap map)
        {
            if (creature == null || creature.Position == null || map == null || map.Field == null)
            {
                return null;
            }

            return map.Field.GetShapeAt(creature.Position);
        }

        /// <summary>
        /// Get Dp according to the creature's status, and map's block
        /// </summary>
        /// <param name="creature"></param>
        /// <returns></returns>
        public static int GetCalculatedDp(FDCreature creature, FDMap map)
        {
            FDPosition position = creature.Position;
            // See GetCalculatedAp: map may be null when shown outside a battle.
            ShapeDefinition shape = map != null ? map.Field.GetShapeAt(position) : null;

            AttackItemDefinition attackItem = creature.GetAttackItem();
            DefendItemDefinition defendItem = creature.GetDefendItem();

            int result = creature.Dp;
            if (defendItem != null)
            {
                result += defendItem.Dp;
            }
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
        /// Rolls what a creature gains from its next level: each stat from the span in
        /// its LevelUpDefinition, plus the magic (if any) its LevelUpMagicDefinition
        /// grants at the level it is about to reach. Nothing is written to the creature
        /// here -- see BattleHandler.ApplyLevelUp for that.
        /// </summary>
        public static LevelUpInfo ComposeLevelUp(FDCreature creature)
        {
            LevelUpInfo result = new LevelUpInfo();

            if (creature == null || creature.Definition == null)
            {
                return result;
            }

            int definitionId = creature.Definition.DefinitionId;

            LevelUpDefinition levelUp = DefinitionStore.Instance.GetLevelUpDefinition(definitionId);
            if (levelUp != null)
            {
                result.ImprovedHp = Math.Max(0, FDRandom.IntFromSpan(levelUp.HpRange));
                result.ImprovedMp = Math.Max(0, FDRandom.IntFromSpan(levelUp.MpRange));
                result.ImprovedAp = Math.Max(0, FDRandom.IntFromSpan(levelUp.ApRange));
                result.ImprovedDp = Math.Max(0, FDRandom.IntFromSpan(levelUp.DpRange));
                result.ImprovedDx = Math.Max(0, FDRandom.IntFromSpan(levelUp.DxRange));
            }

            // The magic table is keyed by the level being reached, not the current one.
            LevelUpMagicDefinition levelUpMagic =
                DefinitionStore.Instance.GetLevelUpMagicDefinition(definitionId, creature.Level + 1);
            if (levelUpMagic != null)
            {
                result.LearntMagicId = levelUpMagic.MagicId;
            }

            return result;
        }


    }
}
