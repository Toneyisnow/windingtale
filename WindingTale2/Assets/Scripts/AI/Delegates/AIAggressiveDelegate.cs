using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WindingTale.Core.Algorithms;
using WindingTale.Core.Common;
using WindingTale.Core.Definitions;
using WindingTale.Core.Definitions.Items;
using WindingTale.Core.Objects;
using WindingTale.Scenes.GameFieldScene;

namespace WindingTale.AI.Delegates
{
    /// <summary>
    /// The plain fighter: heal up when badly hurt, otherwise walk at the nearest opponent
    /// it can hurt and attack it if the walk brought it within reach.
    /// </summary>
    public class AIAggressiveDelegate : AIDelegate
    {
        public AIAggressiveDelegate(GameMain gameMain, FDAICreature c) : base(gameMain, c)
        {

        }

        public override void TakeAction()
        {
            if (this.NeedAndCanRecover())
            {
                this.SelfRecover();
                return;
            }

            FDCreature target = this.LookForAggressiveTarget();
            if (target == null)
            {
                // Nobody left on the other side.
                this.EndTurn();
                return;
            }

            // Walk towards the target, then attack from wherever the walk ended.
            FDPosition destination = this.MoveToward(target.Position);

            this.AttackTargetOrEndTurn(target, destination);
        }

    }
}
