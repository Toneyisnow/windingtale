using System;
using WindingTale.Core.Map;
using WindingTale.Scenes.GameFieldScene;

namespace WindingTale.Core.Events
{
    public abstract class FDConditionEvent : FDEvent
    {

        public abstract bool Match(FDMap gameMap);

        public FDConditionEvent(int eventId, Action action): base(eventId, action)
        {
            this.EventType = FDEventType.Condition;
        }


    }
}