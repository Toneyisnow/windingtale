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

            Debug.Log("ShowMoveRangeState: OnEnter PrePosition: " + creature.PrePosition?.ToString() + " Position: " + creature.Position.ToString());

            base.OnEnter();

            // Reset creature to pre position
            if (creature.PrePosition != null && !creature.PrePosition.AreSame(creature.Position))
            {
                creature.Position = creature.PrePosition;
                CreatureRefreshActivity refreshActivity = new CreatureRefreshActivity(new List<int>() { creature.Id });
                activityManager.Push(refreshActivity);
            }

            // Calculcate the moving scopes and save it in cache
            if (moveRange == null)
            {
                MoveRangeFinder finder = new MoveRangeFinder(gameMap, creature);
                moveRange = finder.CalculateMoveRange();
            }

            // Send move range to UI
            ShowRangeActivity activity = new ShowRangeActivity(moveRange.ToList());
            activityManager.Push(activity);
        }

        public override void OnExit()
        {
            Debug.Log("ShowMoveRangeState: OnExit");

            
            // Clear move range on UI
            ClearRangeActivity activity = new ClearRangeActivity();
            activityManager.Push(activity);
           


            base.OnExit();
        }

        public override void OnSelectPosition(FDPosition position)
        {
            // If position is in range
            if (moveRange.Contains(position))
            {
                MovePathFinder movePathFinder = new MovePathFinder(moveRange);
                FDMovePath movePath = movePathFinder.GetPath(position);

                gameMain.CreatureMove(creature, movePath);

                // Save the current position
                creature.PrePosition = creature.Position;
                var nextState = new MenuActionState(gameMain, stateHandler, creature, position);
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
