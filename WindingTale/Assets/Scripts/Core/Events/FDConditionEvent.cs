using System;
using WindingTale.Core.Map;
using WindingTale.UI.Scenes.Game;

namespace WindingTale.Core.Events
{
    public abstract class FDConditionEvent : FDEvent
    {

        public abstract bool Match(GameMap gameMap);

        public FDConditionEvent(int eventId, Action<GameMain> action): base(eventId, action)
        {
            this.EventType = FDEventType.Condition;
        }


    }
}