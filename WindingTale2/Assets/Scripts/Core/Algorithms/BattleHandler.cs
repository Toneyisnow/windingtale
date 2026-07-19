using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;
using WindingTale.Core.Common;
using WindingTale.Core.Definitions;
using WindingTale.Core.Definitions.Items;
using WindingTale.Core.Map;
using WindingTale.Core.Objects;
using WindingTale.MapObjects.GameMap;

namespace WindingTale.Core.Algorithms
{
    public class BattleHandler
    {
        private static int DEFAULT_DOUBLE_ATTACK_RATE = 5;

        private static int DEFAULT_CRITICAL_ATTACK_RATE = 5;

        #region Public Methods 

        /// <summary>
        /// Calculate the attack result from subject to target
        /// </summary>
        /// <param name="subject"></param>
        /// <param name="target"></param>
        /// <param name="field"></param>
        /// <returns></returns>
        public static AttackResult HandleCreatureAttack(FDCreature subject, FDCreature target, FDField field)
        {
            AttackResult result = new AttackResult(subject, target);

            DamageResult damage1 = DamageFrom(subject, target, field);
            result.Experience = CalculateDamageExp(subject, target, damage1);
            result.Damages.Add(damage1);
            int targetLastHp = damage1.HpAfter;

            if (targetLastHp > 0 && FDRandom.BoolFromRate(DEFAULT_DOUBLE_ATTACK_RATE))
            {
                DamageResult damage2 = DamageFrom(subject, target, field);
                result.Experience += CalculateDamageExp(subject, target, damage2);
                result.Damages.Add(damage2);
                targetLastHp = damage2.HpAfter;
            }

            // Fight back
            bool canFightBack = CanFightBack(subject, target, field);
            if (canFightBack && targetLastHp > 0)
            {
                DamageResult back1 = DamageFrom(target, subject, field);
                result.BackExperience += CalculateDamageExp(target, subject, back1);
                result.BackDamages.Add(back1);

                if (back1.HpAfter > 0 && FDRandom.BoolFromRate(DEFAULT_DOUBLE_ATTACK_RATE))
                {
                    DamageResult back2 = DamageFrom(target, subject, field);
                    result.BackExperience += CalculateDamageExp(target, subject, back2);
                    result.BackDamages.Add(back2);
                }
            }

            // Calculate experience and level up

            return result;
        }

        /// <summary>
        /// Calculate the magic result from subject to targets
        /// </summary>
        /// <param name="creature"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        public static MagicResult HandleCreatureMagic(FDCreature subject, List<FDCreature> targetList, FDPosition magicPosition, MagicDefinition magic, FDField field)
        {
            if (subject == null || magic == null)
            {
                return null;
            }
            MagicResult result = new MagicResult(subject, targetList, magic.MpCost);
            if (subject == null || targetList == null || targetList.Count == 0)
            {
                // From previous steps, there should be at least one target
                throw new ArgumentNullException("subject or targetList");
            }

            result.Experience = 0;
            foreach (FDCreature target in targetList)
            {
                SoloResult soloResult = MagicFrom(magic, subject, target, field);
                if (soloResult == null)
                {
                    continue;
                }

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
        public static ItemResult HandleCreatureItem(FDCreature creature, FDPosition position, ItemDefinition item)
        {
            return null;
        }

        public static RecoverResult HandleCreatureRecover(FDCreature creature)
        {
            if (creature == null)
            {
                return null;
            }

            int recoverAmount = (int)(creature.HpMax * 0.15);
            int actualAmount = Math.Min(recoverAmount, creature.HpMax - creature.Hp);
            return new RecoverResult(RecoverType.Hp, actualAmount);
        }



        public static void ApplyDamage(FDCreature creature, DamageResult damage)
        {
            creature.Hp = damage.HpAfter;
            if (creature.Hp <= 0)
            {
                creature.Hp = 0;
            }
        }

        /// <summary>
        /// Adds experience and, when it crosses 100, rolls the level-up. A single gain
        /// never exceeds 100 exp, so at most one level is reached at a time. Returns the
        /// composed LevelUpInfo when a level was reached (the caller is expected to pass
        /// it to ApplyLevelUp and announce it), or null otherwise.
        /// </summary>
        public static LevelUpInfo ApplyExperience(FDCreature creature, int experience)
        {
            if (creature == null || creature.Faction != CreatureFaction.Friend)
            {
                // Only friend can gain experience
                return null;
            }

            creature.Exp += experience;
            if (creature.Exp >= 100)
            {
                creature.Exp -= 100;
                return CreatureFormula.ComposeLevelUp(creature);
            }

            return null;
        }

        /// <summary>
        /// Writes a composed level-up onto the creature: one level, the rolled stat
        /// gains, and the magic learnt at the new level. HP/MP gains raise both the
        /// maximum and the current value, so levelling up also heals by that much.
        /// </summary>
        public static void ApplyLevelUp(FDCreature creature, LevelUpInfo levelUpInfo)
        {
            if (creature == null || levelUpInfo == null)
            {
                return;
            }

            creature.Level = creature.Level + 1;

            creature.HpMax += levelUpInfo.ImprovedHp;
            creature.Hp += levelUpInfo.ImprovedHp;
            creature.MpMax += levelUpInfo.ImprovedMp;
            creature.Mp += levelUpInfo.ImprovedMp;

            creature.Ap += levelUpInfo.ImprovedAp;
            creature.Dp += levelUpInfo.ImprovedDp;
            creature.Dx += levelUpInfo.ImprovedDx;

            if (levelUpInfo.LearntMagicId > 0)
            {
                creature.AddMagic(levelUpInfo.LearntMagicId);
            }
        }


        #endregion


        #region Private Methods

        /// <summary>
        /// Calculate a single damage result
        /// </summary>
        /// <param name="subject"></param>
        /// <param name="target"></param>
        /// <param name="field"></param>
        /// <returns></returns>
        private static DamageResult DamageFrom(FDCreature subject, FDCreature target, FDField field)
        {
            bool isHit = FDRandom.BoolFromRate(subject.CalculatedHit - target.CalculatedEv);
            bool isCritical = FDRandom.BoolFromRate(DEFAULT_CRITICAL_ATTACK_RATE);

            int reduceHp = 0;
            if (isHit)
            {
                FDPosition pos = subject.Position;
                ShapeDefinition shape = field.GetShapeAt(pos);
                int adjustedAp = subject.CalculatedAp * (100 + shape.AdjustedAp) / 100;

                FDPosition targetPos = target.Position;
                ShapeDefinition targetShape = field.GetShapeAt(targetPos);
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
                        target.ApplyEffect(new EffectResult(EffectType.Poison, 4));
                    }
                }
            }

            int hpAfter = Math.Max(target.Hp - reduceHp, 0);
            DamageResult damage = new DamageResult(target.Hp, hpAfter, isCritical);
            return damage;
        }

        /// <summary>
        /// Result of one spell against one target. Never returns null: a miss is a real
        /// outcome and gets a zero-valued result of the magic's own kind (a missed attack
        /// is a DamageResult with HasMissed set, the same shape a missed physical attack
        /// produces). Callers index Results by target id for every target, so leaving a
        /// target without a result would break them.
        /// </summary>
        private static SoloResult MagicFrom(MagicDefinition magic, FDCreature subject, FDCreature target, FDField field)
        {
            bool isHit = FDRandom.BoolFromRate(magic.HittingRate);
            int changedHp = 0;

            switch (magic.Type)
            {
                case MagicType.Attack:
                    if (!isHit)
                    {
                        return new DamageResult(target.Hp, target.Hp, false);
                    }

                    double hitRate = 1.0f;
                    OccupationDefinition occupation = (target.Definition != null)
                        ? DefinitionStore.Instance.GetOccupationDefinition(target.Definition.Occupation)
                        : null;
                    if (occupation != null)
                    {
                        hitRate = (100 - occupation.MagicDefendRate) / 100.0f;
                    }

                    changedHp = FDRandom.IntFromSpan(magic.Span) + magic.ApInvolvedRate * subject.CalculatedAp / 100;
                    changedHp = (int)(changedHp * hitRate);
                    changedHp = Math.Max(0, changedHp);
                    return new DamageResult(target.Hp, Math.Max(target.Hp - changedHp, 0), false);

                case MagicType.Recover:
                    changedHp = isHit ? Math.Max(0, FDRandom.IntFromSpan(magic.Span)) : 0;
                    return new RecoverResult(RecoverType.Hp, changedHp);

                case MagicType.Offensive:
                case MagicType.Defensive:
                    // GenerateEffect only covers the magic ids it knows; an unknown id
                    // falls through to "no effect" rather than to a null result.
                    EffectResult effect = isHit ? magic.GenerateEffect() : null;
                    return effect ?? NoEffect();

                default:
                    return NoEffect();
            }
        }

        /// <summary>
        /// "Nothing happened" for effect magic: an empty MultiEffectResult. Applying it
        /// is a no-op and it earns no experience (see CalculateMagicEffectExp).
        /// </summary>
        private static EffectResult NoEffect()
        {
            return new MultiEffectResult(new List<EffectResult>());
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

            float result = calculatedHp * target.Level * target.Exp / (float)subject.Level / (float)target.HpMax;
            return (int)result;
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="subject"></param>
        /// <param name="target"></param>
        /// <param name="magic"></param>
        /// <param name="effectResult"></param>
        /// <returns></returns>
        private static int CalculateMagicEffectExp(FDCreature subject, FDCreature target, MagicDefinition magic, EffectResult effectResult)
        {
            if (subject == null || target == null || magic == null || effectResult == null || subject.Faction != CreatureFaction.Friend)
            {
                return 0;
            }

            // An empty MultiEffectResult means the spell missed / did nothing: no exp.
            if (effectResult is MultiEffectResult multi && (multi.Effects == null || multi.Effects.Count == 0))
            {
                return 0;
            }

            // Other effects magic
            return magic.GetBaseExperience() * target.Level / subject.Level;
        }

        /// <summary>
        /// TODO
        /// </summary>
        /// <param name="subject"></param>
        /// <param name="target"></param>
        /// <param name="field"></param>
        /// <returns></returns>
        private static bool CanFightBack(FDCreature subject, FDCreature target, FDField field)
        {
            // Near
            if (!subject.Position.IsNextTo(target.Position))
            {
                return false;
            }

            // target has weapon and not freezing
            if (!target.CanAttack())
            {
                return false;
            }

            return true;
        }

        #endregion

    }
}