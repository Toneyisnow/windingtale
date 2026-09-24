using UnityEngine;
using WindingTale.Core.Algorithms;
using WindingTale.Core.Common;

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

        /// <summary>
        /// The line for a healing spell that restored the given amount (0 = nothing), and its
        /// colour. The amount is prefixed with what it restored: "HP+30" / "MP+12".
        /// </summary>
        public static string ForRecover(RecoverType type, int recovered, out Color color)
        {
            string text;
            if (recovered > 0)
            {
                bool isMp = type == RecoverType.Mp;
                text = (isMp ? "MP+" : "HP+") + recovered;
                color = isMp ? BuffColor : HealColor;
            }
            else
            {
                text = NothingText;
                color = NoneColor;
            }
            return text;
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
                    text = "解冻";
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
                    text = "冰冻";
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
