using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WindingTale.Core.ObjectModels;

namespace WindingTale.Core.Components.Events.Conditions
{
    public class CreatureDyingCondition : TriggeredEventCondition
    {
        private int creatureId = 0;

        public CreatureDyingCondition(int creatureId)
        {
            this.creatureId = creatureId;
        }

        public override bool IsMatched(IGameAction gameAction)
        {
            FDCreature creature = gameAction.GetCreature(creatureId);
            return (creature != null && creature.Data.Hp <= 0);
        }
    }
}
