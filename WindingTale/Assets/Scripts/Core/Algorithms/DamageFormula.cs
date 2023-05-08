using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using WindingTale.Core.Common;
using WindingTale.Core.Components.Algorithms;
using WindingTale.Core.Definitions;
using WindingTale.Core.Definitions.Items;
using WindingTale.Core.Map;
using WindingTale.Core.Objects;

namespace WindingTale.Core.Algorithms
{
    public class DamageFormula
    {
        private static int commonDoubleAttackRate = 5;
        private static int commonCriticalAttackRate = 5;

        public static AttackResult DealWithAttack(FDCreature subject, FDCreature target, GameField field, bool canFightBack)
        {
            AttackResult attack1 = AttackFrom(subject, target, field);
            BattleResults attack2 = null;
            BattleResults back1 = null;
            BattleResults back2 = null;

            int exp1 = CalculateAttackExp(subject, target, attack1);
            int backExp1 = 0;

            if (target.Data.Hp > 0)
            {
                bool isDoubleHit = FDRandom.BoolFromRate(commonDoubleAttackRate);

                if (isDoubleHit)
                {
                    attack2 = AttackFrom(subject, target, field);
                    exp1 += CalculateAttackExp(subject, target, attack2);
                }

                if (exp1 > 0)
                {
                    // Gain Experience
                }
            }
            else
            {
                // Gain Experience
            }

            // Fight back
            if (canFightBack && target.Data.Hp > 0)
            {
                back1 = AttackFrom(target, subject, field);
                backExp1 = CalculateAttackExp(target, subject, back1);

                bool isDoubleHit = FDRandom.BoolFromRate(commonDoubleAttackRate);
                if(subject.Data.Hp > 0 && isDoubleHit)
                {
                    back2 = AttackFrom(target, subject, field);
                    backExp1 += CalculateAttackExp(target, subject, back1);

                    if (subject.Data.Hp > 0)
                    {
                        // Gain Experience
                    }
                    else
                    {
                        // Gain Experience
                    }
                }
                else
                {
                    // Gain Experience
                }
            }
            else
            {
                // Gain Experience
            }

            FightInformation fighting = new FightInformation(attack1, attack2, back1, back2);

            return fighting;
        }

        public static MagicalInformation DealWithMagic(int magicId, FDCreature subject, List<FDCreature> targetList, GameField field)
        {
            MagicalInformation result = new MagicalInformation();

            MagicDefinition magic = DefinitionStore.Instance.GetMagicDefinition(magicId);
            if (magic == null)
            {
                return null;
            }

            if (subject == null || targetList == null)
            {
                throw new ArgumentNullException("subject or targetList");
            }

            int totalExp = 0;
            foreach(FDCreature target in targetList)
            {
                BattleResults magicInfo = MagicFrom(magic, subject, target, field);
                result.AddInformation(magicInfo);

                if (magic.Type == MagicType.Attack || magic.Type == MagicType.Recover)
                {
                    totalExp += CalculateAttackExp(subject, target, magicInfo);
                }
                else
                {
                    totalExp += CalculateMagicExp(subject, target, magic);
                }
            }

            if (totalExp > 0)
            {
                // Gain Experience
            }

            return result;
        }

        
        private static BattleResults MagicFrom(MagicDefinition magic, FDCreature subject, FDCreature target, GameField field)
        {
            bool isHit = FDRandom.BoolFromRate(magic.HittingRate);

            int changedHp = 0;
            if(isHit)
            {
                OccupationDefinition occupation = DefinitionStore.Instance.GetOccupationDefinition(target.Definition.Occupation);
                double hitRate = 1.0f;

                if (occupation != null)
                {
                    hitRate = (100 - occupation.MagicDefendRate) / 100.0f;
                }

                switch(magic.Type)
                {
                    case MagicType.Attack:
                        changedHp = -FDRandom.IntFromSpan(magic.Span) + magic.ApInvolvedRate * subject.Data.CalculatedAp / 100;
                        changedHp = (int)(changedHp * hitRate);
                        changedHp = Math.Min(0, changedHp);
                        break;
                    case MagicType.Recover:
                        changedHp = FDRandom.IntFromSpan(magic.Span);
                        changedHp = Math.Max(0, changedHp);
                        break;
                    case MagicType.Offensive:
                        TakeOffensiveEffect(magic, target);
                        break;
                    case MagicType.Defensive:
                        TakeDefensiveEffect(magic, target); 
                        break;
                    default:
                        break;
                }
            }

            BattleResults info = new AttackInformation(target.Data.Hp, target.Data.Hp + changedHp, false);
            target.Data.UpdateHp(changedHp);

            return info;
        }

        private static int CalculateAttackExp(FDCreature subject, FDCreature target, BattleResults info)
        {
            return 0;
        }

        private static int CalculateMagicExp(FDCreature subject, FDCreature target, MagicDefinition magic)
        {
            if (subject == null || target == null || magic == null)
            {
                return 0;
            }

            return magic.GetBaseExperience() * target.Level / subject.Level;
        }

        private static void TakeOffensiveEffect(MagicDefinition magic, FDCreature target)
        {

        }

        private static void TakeDefensiveEffect(MagicDefinition magic, FDCreature target)
        {

        }

    }
}
