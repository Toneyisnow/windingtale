using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WindingTale.Core.Map;
using WindingTale.Core.Objects;

namespace WindingTale.Core.Events
{
    public class CreatureDeadCondition : FDConditionEvent
    {
        private int creatureId = 0;

        public CreatureDeadCondition(int creatureId)
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
