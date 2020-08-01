using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WindingTale.Core.Definitions;

namespace WindingTale.Core.Components.Events
{
    public class TurnEvent : GameEvent
    {
        private int turnId;
        private CreatureFaction faction;



        public TurnEvent(int turnId, CreatureFaction faction)
        {
            this.turnId = turnId;
            this.faction = faction;
        }
    }
}