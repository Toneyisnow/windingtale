using WindingTale.Core.Map;

namespace WindingTale.Core.Events
{
    public abstract class FDConditionEvent : FDEvent
    {

        public abstract bool Match(GameMap gameMap);

        public FDConditionEvent()
        {
            this.EventType = FDEventType.Condition;
        }


    }
}