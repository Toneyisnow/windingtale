using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WindingTale.Core.Objects;
using WindingTale.Scenes.GameFieldScene;

namespace WindingTale.AI.Delegates
{
    /// <summary>
    /// A caster that never leaves its spot: with no spell worth casting it simply holds
    /// position and waits for the battle to come to it.
    /// </summary>
    public class AIMagicalDefensiveDelegate : AIMagicalDelegate
    {
        public AIMagicalDefensiveDelegate(GameMain gameMain, FDAICreature c) : base(gameMain, c)
        {

        }

        protected override void TakePendAction()
        {
            this.EndTurn();
        }

    }
}
