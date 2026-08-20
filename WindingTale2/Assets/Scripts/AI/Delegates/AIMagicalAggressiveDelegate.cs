using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WindingTale.Core.Common;
using WindingTale.Core.Objects;
using WindingTale.Scenes.GameFieldScene;

namespace WindingTale.AI.Delegates
{
    /// <summary>
    /// A caster that fights: with no spell worth casting it closes in and swings its weapon
    /// instead -- but only while it is healthy enough, since a battered mage is worth more
    /// hanging back and keeping its distance.
    /// </summary>
    public class AIMagicalAggressiveDelegate : AIMagicalDelegate
    {
        public AIMagicalAggressiveDelegate(GameMain gameMain, FDAICreature c) : base(gameMain, c)
        {

        }

        protected override void TakePendAction()
        {
            FDCreature target = this.LookForAggressiveTarget();
            FDPosition attackFrom = this.creature.Position;

            // Under half health it stays put rather than walking into the fight.
            if (target != null && this.creature.Hp > this.creature.HpMax / 2)
            {
                attackFrom = this.MoveToward(target.Position);
            }

            this.AttackTargetOrEndTurn(target, attackFrom);
        }

    }
}
