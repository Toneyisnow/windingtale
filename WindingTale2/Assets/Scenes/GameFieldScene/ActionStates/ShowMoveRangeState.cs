using System.Collections;
using System.Collections.Generic;
using UnityEditor.SceneManagement;
using UnityEngine;
using WindingTale.Core.Algorithms;
using WindingTale.Core.Common;
using WindingTale.Core.Definitions;
using WindingTale.Core.Objects;
using WindingTale.MapObjects.GameMap;

namespace WindingTale.Scenes.GameFieldScene.ActionStates
{
    public class ShowMoveRangeState : IActionState
    {
        private FDCreature creature;

        private FDMoveRange moveRange = null;

        public ShowMoveRangeState(GameMain gameMain, FDCreature creature) : base(gameMain)
        {
            this.creature = creature;
        }

        public override void onEnter()
        {
            // Reset creature to pre position
            //if (creature.PrePosition != null && !creature.PrePosition.AreSame(creature.Position))
            //{
            //    creature.Position = creature.PrePosition;
            //    CreatureRefreshActivity refreshActivity = new CreatureRefreshActivity(new List<int>() { creature.Id });
            //    activityManager.Push(refreshActivity);
            //}

            // Calculcate the moving scopes and save it in cache
            if (moveRange == null)
            {
                MoveRangeFinder finder = new MoveRangeFinder(map, creature);
                moveRange = finder.CalculateMoveRange();
            }

            // Send move range to UI
            //ShowRangeActivity activity = new ShowRangeActivity(moveRange.ToList());
            //activityManager.Push(activity);
        }

        public override void onExit()
        {
            // Clear move range on UI
            gameMain.gameMap.clearAllIndicators();
        }

        public override IActionState onSelectedPosition(FDPosition position)
        {
            // If position is in range
            if (moveRange.Contains(position))
            {
                MovePathFinder movePathFinder = new MovePathFinder(moveRange);
                FDMovePath movePath = movePathFinder.GetPath(position);

                //// gameMain.CreatureMove(creature, movePath);

                // Save the current position
                creature.PrePosition = creature.Position;
                var nextState = new MenuActionState(gameMain, creature, position);
                return nextState;
                ////stateHandler.HandlePushState(nextState);
            }
            else
            {
                // Cancel
                //// stateHandler.HandlePopState();
            }
            return this;
        }

        public override IActionState onUserCancelled()
        {
            // Nothing to do here
            return this;
        }
    }
}