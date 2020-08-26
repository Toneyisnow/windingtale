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
            // Calculcate the moving scopes and save it in cache
            if (moveRange == null)
            {
                MoveRangeFinder finder = new MoveRangeFinder(gameAction, this.creature);
                moveRange = finder.CalculateMoveRange();
            }

            // If the creature has moved, reset the creature position
            this.creature.ResetPosition();
            RefreshCreaturePack reset = new RefreshCreaturePack(this.creature.Clone());
            SendPack(reset);

            ShowRangePack showRange = new ShowRangePack(moveRange);
            SendPack(showRange);
        }

        public override void OnExit()
        {
            // Clear move range on UI
            var gameCallback = gameAction.GetCallback();
            gameCallback.OnCallback(new ClearRangePack());
        }

        public override StateOperationResult OnSelectPosition(FDPosition position)
        {
            // If position is in range
            if (moveRange.Contains(position))
            {
                FDMovePath movePath = moveRange.GetPath(position);
                gameAction.CreatureWalk(new SingleWalkAction(creature.CreatureId, movePath));

                var nextState = new MenuActionState(gameAction, creature.CreatureId, position);
                return new StateOperationResult(StateOperationResult.ResultType.Push, nextState);
            }
            else
            {
                // Cancel
                return new StateOperationResult(StateOperationResult.ResultType.Pop);
            }
        }
    }
}
