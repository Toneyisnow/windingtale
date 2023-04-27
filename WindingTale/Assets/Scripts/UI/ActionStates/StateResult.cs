using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WindingTale.UI.ActionStates
{

    public class StateResult
    {
        public enum ResultType
        {
            /// None,
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

        private StateResult(ResultType type, ActionState next = null)
        {
            this.Type = type;
            this.NextState = next;
        }


        public static StateResult Push(ActionState next)
        {
            return new StateResult(ResultType.Push, next);
        }

        public static StateResult Pop()
        {
            return new StateResult(ResultType.Pop);
        }

        public static StateResult Clear()
        {
            return new StateResult(ResultType.Clear);
        }

    }
}