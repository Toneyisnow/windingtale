using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WindingTale.Core.Map;
using WindingTale.Core.Objects;
using WindingTale.UI.Scenes.Game;

namespace WindingTale.Core.Events
{
    public class CreatureDeadEvent : FDConditionEvent
    {
        private int creatureId = 0;

        public CreatureDeadEvent(int eventId, int creatureId, Action<GameMain> action) : base(eventId, action)
        {
            this.creatureId = creatureId;
        }

        public override bool Match(GameMap gameMap)
        {
            FDCreature creature = gameMap.DeadCreatures.Find(c => c.Id == this.creatureId);
            return (creature != null);
        }
    }
}
