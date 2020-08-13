using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WindingTale.Common;
using WindingTale.Core.Components.Algorithms;
using WindingTale.Core.Components.Packs;
using WindingTale.Core.ObjectModels;

namespace WindingTale.Core.Components.ActionStates
{
    public class ShowMoveRangeState : ActionState
    {
        private FDCreature creature = null;

        private FDPosition position = null;

        /// <summary>
        /// This is cached value
        /// </summary>
        private FDMoveRange moveRange = null;



        public ShowMoveRangeState(IGameAction action, FDCreature creature) : base(action)
        {
            this.creature = creature;
            this.position = creature.Position;
        }

        public override void OnEnter()
        {
            // Calculcate the moving scopes
            if (moveRange == null)
            {
                MoveRangeFinder finder = new MoveRangeFinder(gameAction);
                moveRange = finder.CalculateMoveRange();
            }

            ShowRangePack pack = new ShowRangePack(moveRange);
            var gameCallback = gameAction.GetCallback();
            gameCallback.OnReceivePack(pack);
        }

        public override void OnExit()
        {
            // Clear move range on UI
        }

        public override void OnSelectPosition(FDPosition position)
        {


        }
    }
}
