using System;
using WindingTale.Core.Common;
using WindingTale.Core.Map;
using WindingTale.Core.Objects;
using WindingTale.UI.Scenes.Game;

namespace WindingTale.Core.Events
{
    public class ReachPositionEvent : FDConditionEvent
    {
        private int creatureId = -1;

        private FDPosition position = null;


        /// <summary>
        /// If creatureId is -1, it means the anyone can trigger this event.
        /// </summary>
        /// <param name="creatureId"></param>
        /// <param name="position"></param>
        public ReachPositionEvent(int eventId, int creatureId, FDPosition position, Action<GameMain> action) : base(eventId, action) 
        {
            this.creatureId = creatureId;
            this.position = position;
        }

        public override bool Match(GameMap gameMap)
        {
            FDCreature creature = gameMap.GetCreatureById(creatureId);
            if (creature == null)
            {
                return false;
            }

            if (creature.Position.AreSame(position))
            {
                return true;
            }
            return false;
        }

    }
}