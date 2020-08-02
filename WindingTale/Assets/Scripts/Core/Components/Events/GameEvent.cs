using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WindingTale.Core.Components.Events
{
    public abstract class GameEvent
    {
        public EventCondition Condition
        {
            get; set;
        }

        public int EventId
        {
            get; set;
        }

        public bool IsActive
        {
            get; set;
        }
    }
}