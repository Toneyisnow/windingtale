using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WindingTale.Common;
using WindingTale.Core.ObjectModels;
using WindingTale.UI.Components;

namespace WindingTale.Core.Components.ActionStates
{
    public class GameStateDispatcher
    {
        private Stack<ActionState> actionStateStack = null;

        private IGameAction gameAction = null;

        private ActionState currentState = null;

        public GameStateDispatcher(IGameAction gameAction)
        {
            this.gameAction = gameAction;

            this.actionStateStack = new Stack<ActionState>();

            // Push Idle State for init
            var idle = new IdleState(gameAction);
            actionStateStack.Push(idle);

            currentState = RetrieveCurrentState();

        }

        public ActionState RetrieveCurrentState()
        {
            if (actionStateStack == null || actionStateStack.Count == 0)
            {
                return null;
            }

            return actionStateStack.Peek();
        }

        public void OnSelectPosition(FDPosition position)
        {
            if (currentState == null)
            {
                throw new InvalidOperationException("current state is null.");
            }

            var result = currentState.OnSelectPosition(position);
            this.HandleOperationResult(result);
        }

        public void OnSelectIndex(int index)
        {
            if (currentState == null)
            {
                throw new InvalidOperationException("current state is null.");
            }

            var result = currentState.OnSelectIndex(index);
            this.HandleOperationResult(result);
        }



        /*
         * 
        // Might not be used
        public void SelectCreature(int creatureId)
        {

        }
        
        public void SelectCreature(FDCreature creature)
        {

        }

        public void SelectMenu(int menuId)
        {

        }

        public void SelectMenu(FDMenu menu)
        {

        }
        */

        private void HandleOperationResult(StateOperationResult result)
        {
            switch(result.Type)
            {
                case StateOperationResult.ResultType.None:
                    // Nothing to do here
                    break;
                case StateOperationResult.ResultType.Push:
                    if (result.NextState != null)
                    {
                        currentState.OnExit();
                        actionStateStack.Push(result.NextState);
                        result.NextState.OnEnter();
                        currentState = actionStateStack.Peek();

                    }
                    break;
                case StateOperationResult.ResultType.Pop:   // Pop the last state
                    currentState.OnExit();
                    actionStateStack.Pop();
                    currentState = actionStateStack.Peek();
                    currentState.OnEnter();
                    break;
                case StateOperationResult.ResultType.Clear: // Clear all states and only keep the first one IdleState
                    currentState.OnExit();
                    while(actionStateStack.Count > 1)
                    {
                        actionStateStack.Pop();
                    }
                    currentState = actionStateStack.Peek();
                    currentState.OnEnter();
                    break;
                default:
                    break;
            }
        }
    }

}