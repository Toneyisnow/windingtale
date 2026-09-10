using System;
using WindingTale.Core.Common;
using WindingTale.Core.Map;
using WindingTale.Core.Objects;

namespace WindingTale.Core.Events
{
    /// <summary>
    /// Fires once at the end of a turn -- the moment the last enemy has acted and
    /// before the next turn's number comes up. This is the original's
    /// EndTurnEventCondition (chapter 30) and, with a creature and a tile, its
    /// ArrivePositionTurnCondition (chapter 29): the chapters chain several of these
    /// behind one another with AddDependentEvent, so that one thing happens per turn
    /// -- a boss comes out the turn after his guard has fallen, the fortress spawns
    /// two more guards for every turn Youni stands at the console.
    ///
    /// FDMap.IsEndOfTurn is raised only for the one check GameMain runs at that
    /// moment, so the event can never match anywhere else in the turn.
    /// </summary>
    public class TurnEndEvent : FDConditionEvent
    {
        private int creatureId = 0;

        private FDPosition position = null;

        /// <summary>Matches every end of turn.</summary>
        public TurnEndEvent(int eventId, Action action) : base(eventId, action)
        {
        }

        /// <summary>Matches an end of turn at which <paramref name="creatureId"/> is standing on <paramref name="position"/>.</summary>
        public TurnEndEvent(int eventId, int creatureId, FDPosition position, Action action) : base(eventId, action)
        {
            this.creatureId = creatureId;
            this.position = position;
        }

        public override bool Match(FDMap gameMap)
        {
            if (!gameMap.IsEndOfTurn)
            {
                return false;
            }

            if (creatureId <= 0 || position == null)
            {
                return true;
            }

            FDCreature creature = gameMap.GetCreatureAt(position);
            return creature != null && creature.Id == creatureId;
        }
    }
}
