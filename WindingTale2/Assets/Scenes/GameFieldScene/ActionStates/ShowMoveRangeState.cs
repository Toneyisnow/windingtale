using System.Collections;
using System.Collections.Generic;
using UnityEditor.SceneManagement;
using UnityEngine;
using WindingTale.Core.Algorithms;
using WindingTale.Core.Common;
using WindingTale.Core.Definitions;
using WindingTale.Core.Objects;
using WindingTale.MapObjects.CreatureIcon;
using WindingTale.MapObjects.GameMap;
using WindingTale.Scenes.GameFieldScene.Activities;

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
            if (creature.PrePosition != null && !creature.PrePosition.AreSame(creature.Position))
            {
                gameMain.PushActivity((gameMain) =>
                {
                    gameMain.gameMap.ResetCreaturePosition(creature, creature.PrePosition);
                });

            //    creature.Position = creature.PrePosition;
            //    CreatureRefreshActivity refreshActivity = new CreatureRefreshActivity(new List<int>() { creature.Id });
            //    activityManager.Push(refreshActivity);
            }

            // Calculcate the moving scopes and save it in cache
            if (moveRange == null)
            {
                MoveRangeFinder finder = new MoveRangeFinder(fdMap, creature);
                moveRange = finder.CalculateMoveRange();
            }

            // Send move range to UI
            gameMain.PushActivity((gameMain) =>
            {
                gameMain.gameMap.showMoveRange(creature, moveRange);
            });

        }

        public override void onExit()
        {
            // Clear move range on UI
            gameMain.gameMap.clearAllIndicators();
        }

        public override IActionState onSelectedPosition(FDPosition position)
        {
            //// Debug.Log("ShowMoveRangeState: onSelectedPosition");

            // If position is in range
            if (moveRange != null && moveRange.Contains(position))
            {
                MovePathFinder movePathFinder = new MovePathFinder(moveRange);
                FDMovePath movePath = movePathFinder.GetPath(position);

                gameMain.creatureMoveAndWait(creature, movePath);

                return new MenuActionState(gameMain, creature, position);
            }
            else
            {
                // Cancel
                return new IdleState(gameMain);
            }
        }

        public override IActionState onUserCancelled()
        {
            // Nothing to do here
            return this;
        }
    }
}