using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WindingTale.Core.ObjectModels;

namespace WindingTale.Core.Components.Events.Conditions
{
    public class CreatureDeadCondition : TriggeredEventCondition
    {
        private int creatureId = 0;

        public CreatureDeadCondition(int creatureId)
        {
            this.creatureId = creatureId;
        }

        public override bool IsMatched(IGameAction gameAction)
        {
            FDCreature creature = gameAction.GetDeadCreature(creatureId);
            return (creature != null);
        }
    }
}
