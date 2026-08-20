using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WindingTale.Core.Objects;
using WindingTale.Scenes.GameFieldScene;

namespace WindingTale.AI.Delegates
{
    /// <summary>
    /// A fighter that holds its ground: it stays where it is posted until an opponent comes
    /// close enough that it could be reached this turn, and only then fights like an
    /// ordinary aggressive creature.
    /// </summary>
    public class AIGuardDelegate : AIAggressiveDelegate
    {
        public AIGuardDelegate(GameMain gameMain, FDAICreature c) : base(gameMain, c)
        {

        }

        public override void TakeAction()
        {
            // One tile of slack on top of the move points: an opponent that close is within
            // striking distance of the post next turn, so the guard stops waiting.
            int alertDistance = this.creature.CalculatedMv + 1;

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

    }
}
