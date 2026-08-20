using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WindingTale.Core.Definitions;
using WindingTale.Core.Objects;
using WindingTale.Scenes.GameFieldScene;

namespace WindingTale.AI.Delegates
{
    /// <summary>
    /// A caster posted somewhere: it keeps quiet until an opponent walks into the reach of
    /// one of its spells, and only then starts casting (and fighting, like its aggressive
    /// counterpart, on the turns it has no spell worth casting).
    /// </summary>
    public class AIMagicalGuardDelegate : AIMagicalAggressiveDelegate
    {
        public AIMagicalGuardDelegate(GameMain gameMain, FDAICreature c) : base(gameMain, c)
        {

        }

        public override void TakeAction()
        {
            int alertDistance = this.GetMaxMagicReach();

            foreach (FDCreature c in this.GetOppositeCreatures())
            {
                if (GetDirectDistance(this.creature.Position, c.Position) <= alertDistance)
                {
                    base.TakeAction();
                    return;
                }
            }

            this.EndTurn();
        }

        /// <summary>
        /// How far the creature's longest ranged spell can touch: the casting range plus the
        /// blast radius, the same reach the target search uses.
        /// </summary>
        private int GetMaxMagicReach()
        {
            int maxReach = 0;

            foreach (MagicDefinition magic in this.GetAvailableMagics())
            {
                int reach = magic.EffectRange + magic.EffectScope;
                if (reach > maxReach)
                {
                    maxReach = reach;
                }
            }

            return maxReach;
        }

    }
}
