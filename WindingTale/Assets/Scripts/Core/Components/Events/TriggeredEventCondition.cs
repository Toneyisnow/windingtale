using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WindingTale.Core.Components.Events
{
    public class TriggeredEventCondition : EventCondition
    {


        public override bool Match(IGameAction gameAction)
        {
            return false;
        }
    }
}
