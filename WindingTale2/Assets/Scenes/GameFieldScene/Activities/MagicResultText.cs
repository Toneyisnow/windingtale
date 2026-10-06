using UnityEngine;
using WindingTale.Core.Algorithms;
using WindingTale.Core.Common;
using WindingTale.Core.Definitions.Items;

namespace WindingTale.Scenes.GameFieldScene.Activities
{
    /// <summary>
    /// The floating line a spell leaves over each creature it touched: what it did, and
    /// the colour to show it in. Healing and buffs read green / blue, debuffs red, and a
    /// spell that did nothing (a miss, a full-health target, a buff with no effect) grey.
    /// </summary>
    public static class MagicResultText
    {
        private static readonly Color HealColor = new Color(0.45f, 1f, 0.45f);
        private static readonly Color BuffColor = new Color(0.55f, 0.85f, 1f);
        private static readonly Color DebuffColor = new Color(1f, 0.45f, 0.4f);
        private static readonly Color NoneColor = new Color(0.8f, 0.8f, 0.8f);

        private const string NothingText = "无效";

        // The stat potions, each in a colour of its own: strength deep purple, speed deep
        // teal, endurance deep blue-violet, the wind spirit's feather bright silver.
        private static readonly Color PotionApColor = new Color(0.42f, 0.10f, 0.62f);
        private static readonly Color PotionDxColor = new Color(0.00f, 0.42f, 0.45f);
        private static readonly Color PotionDpColor = new Color(0.27f, 0.16f, 0.72f);
        private static readonly Color PotionMvColor = new Color(0.85f, 0.89f, 0.95f);

        /// <summary>
        /// The line for a permanent stat potion: "AP+3", "DP+3", "DX+3", "MV+1" (or "HP上限+20"
        /// / "MP上限+20" for the max-HP / max-MP ones), with the colour it is shown in. Null for
        /// an item that is not one of these.
        /// </summary>
        public static string ForPotion(ItemUseType type, int amount, out Color color)
        {
            switch (type)
            {
                case ItemUseType.Ap:
                    color = PotionApColor;
                    return "AP+" + amount;
                case ItemUseType.Dp:
                    color = PotionDpColor;
                    return "DP+" + amount;
                case ItemUseType.Dx:
                    color = PotionDxColor;
                    return "DX+" + amount;
                case ItemUseType.Mv:
                    color = PotionMvColor;
                    return "MV+" + amount;
                case ItemUseType.HpMax:
                    color = HealColor;
                    return "HP上限+" + amount;
                case ItemUseType.MpMax:
                    color = BuffColor;
                    return "MP上限+" + amount;
                default:
                    color = NoneColor;
                    return null;
            }
        }

        /// <summary>
        /// The line for a healing spell that restored the given amount, and its colour. The
        /// amount is prefixed with what it restored: "HP+30" / "MP+12". A heal that restored
        /// nothing (a full-health target) reads "HP+0" in grey rather than a vague "invalid".
        /// </summary>
        public static string ForRecover(RecoverType type, int recovered, out Color color)
        {
            bool isMp = type == RecoverType.Mp;
            string text = (isMp ? "MP+" : "HP+") + Mathf.Max(0, recovered);
            color = recovered > 0 ? (isMp ? BuffColor : HealColor) : NoneColor;
            return text;
        }

        /// <summary>
        /// The line for an attack spell shown on the field rather than in the battle scene:
        /// the HP it took, "HP-72", in red, or "无效" in grey when it missed.
        /// </summary>
        public static string ForDamage(DamageResult damage, out Color color)
        {
            if (damage == null || damage.HasMissed)
            {
                color = NoneColor;
                return NothingText;
            }

            color = DebuffColor;
            return "HP-" + (damage.HpBefore - damage.HpAfter);
        }

        /// <summary>The line for a status spell (buff, cure, debuff, extra action).</summary>
        public static string ForEffect(EffectResult effect, out Color color)
        {
            string text = NothingText;
            color = NoneColor;

            if (effect == null)
            {
                return text;
            }

            if (effect is MultiEffectResult multi)
            {
                if (multi.Effects == null || multi.Effects.Count == 0)
                {
                    return text;
                }

                // The one Multi spell is the all-round buff (407: attack + defence + dexterity).
                text = "全能力提升";
                color = BuffColor;
                return text;
            }

            switch (effect.Type)
            {
                case EffectType.EnhancedAp:
                    text = "攻击提升";
                    color = BuffColor;
                    break;
                case EffectType.EnhancedDp:
                    text = "防御提升";
                    color = BuffColor;
                    break;
                case EffectType.EnhancedDx:
                    text = "敏捷提升";
                    color = BuffColor;
                    break;
                case EffectType.AntiPoison:
                    text = "解毒";
                    color = HealColor;
                    break;
                case EffectType.AntiFreeze:
                    text = "解除麻痹";
                    color = HealColor;
                    break;
                case EffectType.StartAction:
                    text = "再次行动";
                    color = BuffColor;
                    break;
                case EffectType.Poison:
                    text = "中毒";
                    color = DebuffColor;
                    break;
                case EffectType.Freezing:
                    text = "麻痹";
                    color = DebuffColor;
                    break;
                case EffectType.Forbidden:
                    text = "封魔";
                    color = DebuffColor;
                    break;
            }

            return text;
        }

        /// <summary>Whether the result is, or (for a Multi) contains, an effect of this type.</summary>
        public static bool Contains(EffectResult effect, EffectType type)
        {
            if (effect == null)
            {
                return false;
            }

            if (effect is MultiEffectResult multi)
            {
                if (multi.Effects != null)
                {
                    foreach (EffectResult inner in multi.Effects)
                    {
                        if (Contains(inner, type))
                        {
                            return true;
                        }
                    }
                }
                return false;
            }

            return effect.Type == type;
        }
    }
}
