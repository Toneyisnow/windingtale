using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WindingTale.Core.Components.Events.Conditions
{
    public abstract class EventCondition
    {
        public enum ConditionType
        {
            Turn = 1,
            Triggered = 2
        }

        public ConditionType Type
        {
            get; protected set;
        }

        public abstract bool IsMatched(IGameAction gameAction);


    }
}