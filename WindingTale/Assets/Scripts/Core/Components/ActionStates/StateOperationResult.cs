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

    }
}