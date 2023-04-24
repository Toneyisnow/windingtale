using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WindingTale.Core.Components.ActionStates
{

    public class StateOperationResult
    {
        public enum ResultType
        {
            None,
            Push,   // Push a new state
            Pop,    // pop current state
            Clear,  // clear all states in the stack, and just keep Idle state
        }

        public ResultType Type
        {
            get; private set;
        }

        public ActionState NextState
        {
            get; private set;
        }

        public StateOperationResult(ResultType type, ActionState next = null)
        {
            this.Type = type;
            this.NextState = next;
        }

        public static StateOperationResult None()
        {
            return new StateOperationResult(ResultType.None);
        }

        public static StateOperationResult Push(ActionState next)
        {
            return new StateOperationResult(ResultType.Push, next);
        }

        public static StateOperationResult Pop()
        {
            return new StateOperationResult(ResultType.Pop);
        }

        public static StateOperationResult Clear()
        {
            return new StateOperationResult(ResultType.Clear);
        }

    }
}