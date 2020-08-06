using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WindingTale.Core.Definitions;

namespace WindingTale.Core.Components.Events.Conditions
{
    public class TeamEliminatedCondition : TriggeredEventCondition
    {
        public CreatureFaction Faction
        {
            get; private set;
        }

        public TeamEliminatedCondition(CreatureFaction faction)
        {
            this.Faction = faction;
        }
    }
}