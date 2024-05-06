using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WindingTale.Core.Map;
using WindingTale.Core.Objects;

namespace WindingTale.Core.Events
{
    public class CreatureDyingEvent : FDConditionEvent
    {
        private int creatureId = 0;

        public CreatureDyingEvent(int eventId, int creatureId, Action action) : base(eventId, action)
        {
            this.creatureId = creatureId;
        }


        public override bool Match(FDMap gameMap)
        {
            FDCreature creature = gameMap.Creatures.Find(c => c.Id == this.creatureId);
            return creature != null && creature.IsDead();
        }
    }
}
