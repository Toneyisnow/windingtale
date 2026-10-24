using System;
using WindingTale.Core.Common;
using WindingTale.Core.Definitions;
using WindingTale.Core.Map;

namespace WindingTale.Core.Algorithms
{
    /// <summary>
    /// Where a spell can be aimed and what it covers. Most spells are aimed at any tile
    /// within EffectRange (a diamond) and cover the diamond of EffectScope around it.
    ///
    /// Cross magic (MagicDefinition.IsCross: 音速刃, the beam cannon's 震荡波) is the
    /// original's getCreaturesAt:ToDirection: it is aimed along the caster's own row or
    /// column, up to EffectRange tiles out, and covers that whole line from the tile next
    /// to the caster out to EffectRange -- picking any tile on it picks the direction.
    /// </summary>
    public static class MagicAreaFinder
    {
        /// <summary>The tiles the spell can be aimed at.</summary>
        public static FDRange CastRange(FDField field, FDPosition caster, MagicDefinition magic)
        {
            if (!magic.IsCross)
            {
                return new DirectRangeFinder(field, caster, magic.EffectRange).CalculateRange();
            }

            FDRange range = new FDRange();
            for (int k = 1; k <= magic.EffectRange; k++)
            {
                AddIfOnField(field, range, FDPosition.At(caster.X + k, caster.Y));
                AddIfOnField(field, range, FDPosition.At(caster.X - k, caster.Y));
                AddIfOnField(field, range, FDPosition.At(caster.X, caster.Y + k));
                AddIfOnField(field, range, FDPosition.At(caster.X, caster.Y - k));
            }

            return range;
        }

        /// <summary>The tiles the spell lands on when aimed at the target tile.</summary>
        public static FDRange Scope(FDField field, FDPosition caster, FDPosition target, MagicDefinition magic)
        {
            if (!magic.IsCross)
            {
                return new DirectRangeFinder(field, target, magic.EffectScope).CalculateRange();
            }

            FDRange scope = new FDRange();
            int dx = Math.Sign(target.X - caster.X);
            int dy = Math.Sign(target.Y - caster.Y);
            if (dx != 0 && dy != 0 || dx == 0 && dy == 0)
            {
                // Off the caster's row and column (or the caster itself): no line.
                return scope;
            }

            for (int k = 1; k <= magic.EffectRange; k++)
            {
                AddIfOnField(field, scope, FDPosition.At(caster.X + dx * k, caster.Y + dy * k));
            }

            return scope;
        }

        private static void AddIfOnField(FDField field, FDRange range, FDPosition pos)
        {
            if (pos.X <= 0 || pos.X > field.Width || pos.Y <= 0 || pos.Y > field.Height)
            {
                return;
            }

            range.Add(pos);
        }
    }
}
