using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WindingTale.Core.Map;
using WindingTale.Core.Objects;

namespace WindingTale.Core.Events
{
    public class CreatureDyingCondition : FDConditionEvent
    {
        private int creatureId = 0;

        public CreatureDyingCondition(int creatureId)
        {
            this.creatureId = creatureId;
        }


        public override bool Match(GameMap gameMap)
        {
            FDCreature creature = gameMap.Creatures.Find(c => c.Id == this.creatureId);
            return creature != null && creature.IsDead();
        }
    }
}
