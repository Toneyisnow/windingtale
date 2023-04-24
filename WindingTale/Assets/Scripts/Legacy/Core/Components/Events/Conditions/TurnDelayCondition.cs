using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WindingTale.Core.Components.Events.Conditions
{
    public class TurnDelayCondition : EventCondition
    {


        public TurnDelayCondition()
        {
            this.Type = ConditionType.Turn;

        }

        public override bool IsMatched(IGameAction gameAction)
        {
            return false;
        }
    }
}