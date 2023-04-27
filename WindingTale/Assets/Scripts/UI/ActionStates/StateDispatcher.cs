using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WindingTale.Common;
using WindingTale.UI.Scenes.Game;

namespace WindingTale.UI.ActionStates
{
    public class StateDispatcher
    {
        private Stack<ActionState> actionStateStack = null;

        public StateDispatcher(GameMain gameMain)
        {
            this.actionStateStack = new Stack<ActionState>();

            // Push Idle State for init
            var idle = new IdleState(gameMain);
            actionStateStack.Push(idle);
        }

        public ActionState GetCurrentState()
        {
            if (actionStateStack == null || actionStateStack.Count == 0)
            {
                return null;
            }

            return actionStateStack.Peek();
        }

        public void OnSelectPosition(FDPosition position)
        {
            ActionState currentState = GetCurrentState();

            if (currentState == null)
            {
                throw new InvalidOperationException("current state is null.");
            }

            var result = currentState.OnSelectPosition(position);
            this.HandleOperationResult(result);
        }

        public void OnSelectCallback(int index)
        {
            ActionState currentState = GetCurrentState();

            if (currentState == null)
            {
                throw new InvalidOperationException("current state is null.");
            }

            var result = currentState.OnSelectCallback(index);
            this.HandleOperationResult(result);
        }

        private void HandleOperationResult(StateResult result)
        {
            ActionState currentState = GetCurrentState();

            switch (result.Type)
            {
                case StateResult.ResultType.Push:
                    if (result.NextState != null)
                    {
                        currentState.OnExit();
                        actionStateStack.Push(result.NextState);
                        result.NextState.OnEnter();
                    }
                    break;
                case StateResult.ResultType.Pop:   // Pop the last state
                    currentState.OnExit();
                    actionStateStack.Pop();
                    currentState = actionStateStack.Peek();
                    currentState.OnEnter();
                    break;
                case StateResult.ResultType.Clear: // Clear all states and only keep the first one IdleState
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