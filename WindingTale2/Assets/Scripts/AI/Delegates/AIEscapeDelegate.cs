using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WindingTale.Core.Common;
using WindingTale.Core.Objects;
using WindingTale.Scenes.GameFieldScene;

namespace WindingTale.AI.Delegates
{
    /// <summary>
    /// The runaway: it ignores everyone and heads for the escape point it was given, as fast
    /// as the ground lets it. What happens when it gets there is the chapter's business --
    /// an event watching that tile removes it, or the battle is lost for letting it go.
    ///
    /// The escape point is carried on the creature (FDAICreature.EscapePosition), set by the
    /// chapter script via ChapterEvents.SetCreatureAiEscape.
    /// </summary>
    public class AIEscapeDelegate : AIDelegate
    {
        public AIEscapeDelegate(GameMain gameMain, FDAICreature c) : base(gameMain, c)
        {

        }

        public override void TakeAction()
        {
            FDPosition escapePosition = (this.creature as FDAICreature)?.EscapePosition;

            if (escapePosition == null)
            {
                // No escape point was ever set for this creature: it has nowhere to run to.
                Debug.LogWarning("AIEscapeDelegate: creature " + creature.Id + " has no escape position.");
                this.EndTurn();
                return;
            }

            this.MoveToward(escapePosition);
            this.EndTurn();
        }

    }
}
