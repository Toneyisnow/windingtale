using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WindingTale.Core.Definitions;

namespace WindingTale.Core.Components.Events
{
    public class TurnEventCondition : EventCondition
    {
        public int TurnId
        {
            get; set;
        }

        public CreatureFaction TurnPhase
        {
            get; set;
        }

    }
}