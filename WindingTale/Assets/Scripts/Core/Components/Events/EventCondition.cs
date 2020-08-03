using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WindingTale.Core.Components.Events
{
    public abstract class EventCondition
    {
        public enum ConditionType
        {
            Turn = 1,
            Triggered = 2
        }

        public abstract bool Match(IGameAction gameAction);


    }
}