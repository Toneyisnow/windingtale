using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WindingTale.Core.Components.Events.Conditions
{
    public class TriggeredEventCondition : EventCondition
    {

        public TriggeredEventCondition()
        {
            this.Type = ConditionType.Triggered;
        }

        public override bool IsMatched(IGameAction gameAction)
        {
            return false;
        }
    }
}
