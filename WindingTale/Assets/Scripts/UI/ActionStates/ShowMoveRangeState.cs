using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using WindingTale.Core.Common;
using WindingTale.Core.Algorithms;
using WindingTale.Core.Objects;
using WindingTale.UI.Activities;
using WindingTale.UI.Scenes.Game;

namespace WindingTale.UI.ActionStates
{
    public class ShowMoveRangeState : ActionState
    {
        private FDCreature creature = null;

        /// <summary>
        /// This is cached value
        /// </summary>
        private FDMoveRange moveRange = null;

        public ShowMoveRangeState(GameMain gameMain, IStateResultHandler stateHandler, FDCreature creature) : base(gameMain, stateHandler)
        {
            this.creature = creature;
        }

        public override void OnEnter()
        {
            base.OnEnter();

            // Calculcate the moving scopes and save it in cache
            if (moveRange == null)
            {
                MoveRangeFinder finder = new MoveRangeFinder(gameMap, creature);
                moveRange = finder.CalculateMoveRange();
            }

            // Send move range to UI
            ShowRangeActivity activity = new ShowRangeActivity(gameMain, moveRange.ToList());
            activityManager.Push(activity);
        }

        public override void OnExit()
        {
            base.OnExit();

            // Clear move range on UI
        }

        public override void OnSelectPosition(FDPosition position)
        {
            // If position is in range
            if (moveRange.Contains(position))
            {
                MovePathFinder movePathFinder = new MovePathFinder(moveRange);
                FDMovePath movePath = movePathFinder.GetPath(position);

                gameMain.CreatureMove(creature, movePath);

                var nextState = new MenuActionState(gameMain, stateHandler, creature);
                stateHandler.HandlePushState(nextState);
            }
            else
            {
                // Cancel
                stateHandler.HandlePopState();
            }
        }
    }
}
