using WindingTale.Common;
using WindingTale.Core.Map;
using WindingTale.Core.Objects;

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
        public ReachPositionEvent(int creatureId, FDPosition position)
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