using WindingTale.Core.Common;
using WindingTale.Core.Components.Algorithms;
using WindingTale.Core.Definitions;
using WindingTale.Core.Map;
using WindingTale.Core.Objects;
using System.Collections.Generic;
using WindingTale.Core.Definitions.Items;
using System;
using WindingTale.Core.Algorithms;

namespace WindingTale.Core
{
    public class GameHandler
    {
        private static int DEFAULT_DOUBLE_ATTACK_RATE = 5;
        private static int DEFAULT_CRITICAL_ATTACK_RATE = 5;

        private GameMap gameMap = null;

        public GameHandler(GameMap map)
        {
            this.gameMap = map;
        }

        /// <summary>
        /// This function will actually update the creature and target, the returned
        /// result is only for UI display.
        /// </summary>
        /// <param name="creature"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        public AttackResult HandleCreatureAttack(FDCreature subject, FDCreature target, GameMap gameMap)
        {
            AttackResult result = new AttackResult();

            DamageResult damage1 = DamageFrom(subject, target, gameMap.Field);
            result.Experience = CalculateDamageExp(subject, target, damage1);
            result.Damages.Add(damage1);

            if (target.Hp > 0 && FDRandom.BoolFromRate(DEFAULT_DOUBLE_ATTACK_RATE))
            {
                DamageResult damage2 = DamageFrom(subject, target, gameMap.Field);
                result.Experience += CalculateDamageExp(subject, target, damage2);
                result.Damages.Add(damage2);
            }

            // Fight back
            int backExp1 = 0;
            bool canFightBack = CanFightBack(subject, target, gameMap.Field );
            if (canFightBack && target.Hp > 0)
            {
                DamageResult back1 = DamageFrom(target, subject, gameMap.Field);
                result.Experience += CalculateDamageExp(target, subject, back1);
                result.BackDamages.Add(back1);

                if (subject.Hp > 0 && FDRandom.BoolFromRate(DEFAULT_DOUBLE_ATTACK_RATE))
                {
                    DamageResult back2 = DamageFrom(target, subject, gameMap.Field);
                    result.Experience += CalculateDamageExp(target, subject, back2);
                    result.BackDamages.Add(back2);
                }
            }

            // Calculate experience and level up

            return result;
        }

        /// <summary>
        /// This function will actually update the creature and target, the returned
        /// result is only for UI display.
        /// </summary>
        /// <param name="creature"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        public MagicResult HandleCreatureMagic(FDCreature subject, FDPosition position, MagicDefinition magic, GameMap gameMap)
        {
            if (subject == null || magic == null)
            {
                return null;
            }

            MagicResult result = new MagicResult();

            DirectRangeFinder directRangeFinder = new DirectRangeFinder(gameMap.Field, position, magic.EffectRange);
            FDRange range = directRangeFinder.CalculateRange();
            List<FDCreature> targetList = gameMap.GetCreaturesInRange(range.ToList(), CreatureFaction.Enemy);

            if (subject == null || targetList == null || targetList.Count == 0)
            {
                // From previous steps, there should be at least one target
                throw new ArgumentNullException("subject or targetList");
            }

            result.Experience = 0;
            foreach (FDCreature target in targetList)
            {
                SoloResult soloResult = MagicFrom(magic, subject, target, gameMap.Field);
                result.Results[target.Id] = soloResult;

                if (soloResult.ResultType == SoloResultType.Damage)
                {
                    result.Experience += CalculateDamageExp(subject, target, (DamageResult)soloResult);
                }
                else if (soloResult.ResultType == SoloResultType.Recover)
                {
                    result.Experience += CalculateRecoverExp(subject, target, magic, (RecoverResult)soloResult);
                }
                else if (soloResult.ResultType == SoloResultType.Effect)
                {
                    result.Experience += CalculateMagicEffectExp(subject, target, magic, (EffectResult)soloResult);
                }
            }

            return result;
        }


        /// <summary>
        /// This function will actually update the creature and target, the returned
        /// result is only for UI display.
        /// </summary>
        /// <param name="creature"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        public ItemResult HandleCreatureItem(FDCreature creature, FDPosition position, ItemDefinition item)
        {
            return null;
        }


        #region Private Methods

        private static DamageResult DamageFrom(FDCreature subject, FDCreature target, GameField field)
        {
            bool isHit = FDRandom.BoolFromRate(subject.CalculatedHit - target.CalculatedEv);
            bool isCritical = FDRandom.BoolFromRate(DEFAULT_CRITICAL_ATTACK_RATE);

            int reduceHp = 0;
            if (isHit)
            {
                FDPosition pos = subject.Position;
                ShapeDefinition shape = field.GetShapeAt(pos.X, pos.Y);
                int adjustedAp = subject.CalculatedAp * (100 + shape.AdjustedAp) / 100;

                FDPosition targetPos = target.Position;
                ShapeDefinition targetShape = field.GetShapeAt(targetPos.X, targetPos.Y);
                int adjustedDp = target.CalculatedDp * (100 + shape.AdjustedDp) / 100;

                int attackMax = adjustedAp - adjustedDp;
                int attackMin = (int)(attackMax * 0.9f);
                reduceHp = FDRandom.IntFromSpan(attackMin, attackMax);
                reduceHp = (reduceHp < 0) ? 0 : reduceHp;

                if (isCritical)
                {
                    reduceHp *= 2;
                }

                // Poisoned
                AttackItemDefinition attackItem = subject.GetAttackItem();
                if (attackItem != null)
                {
                    bool isPoisoned = FDRandom.BoolFromRate(attackItem.GetPoisonRate());
                    if (isPoisoned)
                    {
                        target.SetEffect(CreatureEffects.Poisoned);
                    }
                }
            }

            DamageResult damage = new DamageResult(target.Hp, target.Hp - reduceHp, isCritical);
            ////// target.Data.UpdateHp(-reduceHp);

            return damage;
        }

        private static SoloResult MagicFrom(MagicDefinition magic, FDCreature subject, FDCreature target, GameField field)
        {
            bool isHit = FDRandom.BoolFromRate(magic.HittingRate);
            int changedHp = 0;
            if (isHit)
            {
                OccupationDefinition occupation = DefinitionStore.Instance.GetOccupationDefinition(target.Definition.Occupation);
                double hitRate = 1.0f;
                if (occupation != null)
                {
                    hitRate = (100 - occupation.MagicDefendRate) / 100.0f;
                }

                switch (magic.Type)
                {
                    case MagicType.Attack:
                        changedHp = -FDRandom.IntFromSpan(magic.Span) + magic.ApInvolvedRate * subject.CalculatedAp / 100;
                        changedHp = (int)(changedHp * hitRate);
                        changedHp = Math.Min(0, changedHp);
                        return new DamageResult(target.Hp, target.Hp + changedHp, false);
                    case MagicType.Recover:
                        changedHp = FDRandom.IntFromSpan(magic.Span);
                        changedHp = Math.Max(0, changedHp);
                        return new RecoverResult(RecoverType.Hp, changedHp);
                    case MagicType.Offensive:
                    case MagicType.Defensive:
                        return magic.GenerateEffect();
                    default:
                        break;
                }
            }

            return null;
        }


        private static int CalculateDamageExp(FDCreature subject, FDCreature target, DamageResult damage)
        {
            if (subject == null || target == null || damage == null || subject.Faction != CreatureFaction.Friend)
            {
                return 0;
            }

            int calculatedHp = 0;
            if (damage.HpAfter <= 0)
            {
                calculatedHp = target.HpMax;
            }
            else
            {
                calculatedHp = damage.HpBefore - damage.HpAfter;
            }

            return calculatedHp * target.Level * target.Exp / subject.Level / target.HpMax;
        }

        private static int CalculateRecoverExp(FDCreature subject, FDCreature target, MagicDefinition magic, RecoverResult recoverResult)
        {
            if (subject == null || target == null || magic == null || recoverResult == null || subject.Faction != CreatureFaction.Friend)
            {
                return 0;
            }

            if (recoverResult.Type == RecoverType.Hp)
            {
                // Recover
                int calculatedHp = recoverResult.Amount;
                return (int)(calculatedHp * 100 * target.Level * 0.7 / subject.Level / target.HpMax);
            }
            else
            {
                // No others now
                return 0;
            }
        }

        private static int CalculateMagicEffectExp(FDCreature subject, FDCreature target, MagicDefinition magic, EffectResult effectResult)
        {
            if (subject == null || target == null || magic == null || effectResult == null || subject.Faction != CreatureFaction.Friend)
            {
                return 0;
            }

            // Other effects magic
            return magic.GetBaseExperience() * target.Level / subject.Level;
        }

        private static bool CanFightBack(FDCreature subject, FDCreature target, GameField field)
        {
            // Near

            // target has weapon and not freezing



            return false;
        }

        #endregion

    }
}