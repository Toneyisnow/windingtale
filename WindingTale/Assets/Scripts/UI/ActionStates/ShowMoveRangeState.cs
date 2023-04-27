using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WindingTale.Common;
using WindingTale.Core.Components.Algorithms;
using WindingTale.Core.Objects;
using WindingTale.UI.Scenes.Game;

namespace WindingTale.UI.ActionStates
{
    public class ShowMoveRangeState : ActionState
    {
        private FDCreature creature = null;

        private FDPosition position = null;

        /// <summary>
        /// This is cached value
        /// </summary>
        private FDMoveRange moveRange = null;



        public ShowMoveRangeState(GameMain gameMain, FDCreature creature) : base(gameMain)
        {
            this.creature = creature;
            this.position = creature.Position;
        }

        public override void OnEnter()
        {
            base.OnEnter();

            // Calculcate the moving scopes and save it in cache
            if (moveRange == null)
            {
                MoveRangeFinder finder = new MoveRangeFinder(gameMain, this.creature);
                moveRange = finder.CalculateMoveRange();
            }

            // Send move range to UI
            ShowRangeActivity showRange = new ShowRangeActivity(gameMain, moveRange);
        }

        public override void OnExit()
        {
            base.OnExit();

            // Clear move range on UI
        }

        public override StateResult OnSelectPosition(FDPosition position)
        {
            // If position is in range
            if (moveRange.Contains(position))
            {
                FDMovePath movePath = moveRange.GetPath(position);
                gameAction.CreatureWalk(new SingleWalkAction(creature.Id, movePath));

                var nextState = new MenuActionState(gameAction, creature.Id, position);
                return StateResult.Push(nextState);
            }
            else
            {
                // Cancel
                return StateResult.Pop();
            }
        }
    }
}
