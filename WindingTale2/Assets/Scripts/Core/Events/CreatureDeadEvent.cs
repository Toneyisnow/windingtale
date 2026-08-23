using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WindingTale.Core.Map;
using WindingTale.Core.Objects;
using WindingTale.Scenes.GameFieldScene;

namespace WindingTale.Core.Events
{
    public class CreatureDeadEvent : FDConditionEvent
    {
        private int creatureId = 0;

        public CreatureDeadEvent(int eventId, int creatureId, Action execution) : base(eventId, execution)
        {
            this.creatureId = creatureId;
        }

        /// <summary>
        /// DeadCreatures also holds the party members who walked in already fallen and not
        /// yet revived (ChapterEvents.AddCreatureToMap). They did not die here, so they must
        /// not fire a death the chapter is watching for -- which would otherwise go off the
        /// moment the battle started, before they had a chance to be anywhere.
        /// </summary>
        public override bool Match(FDMap gameMap)
        {
            FDCreature creature = gameMap.DeadCreatures.Find(c => c.Id == this.creatureId && !c.IsUnrevived);
            return (creature != null);
        }
    }
}
