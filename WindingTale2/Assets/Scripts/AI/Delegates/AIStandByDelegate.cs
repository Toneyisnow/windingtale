using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WindingTale.Core.Objects;
using WindingTale.Scenes.GameFieldScene;

namespace WindingTale.AI.Delegates
{
    /// <summary>
    /// Lies in wait: does nothing at all, turn after turn, until a chapter event switches it
    /// to another AI type -- or until somebody attacks it, which wakes it up on its own
    /// (FDAICreature.WakeUpByAttack).
    /// </summary>
    public class AIStandByDelegate : AIDelegate
    {
        public AIStandByDelegate(GameMain gameMain, FDAICreature c) : base(gameMain, c)
        {

        }

        public override void TakeAction()
        {
            this.EndTurn();
        }

    }
}
