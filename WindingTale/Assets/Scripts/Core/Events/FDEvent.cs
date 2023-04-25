using WindingTale.Core.Map;

namespace WindingTale.Core.Events
{
    public enum FDEventType
    {
        Turn = 0,
        Condition = 1,
    }

    /// <summary>
    /// 
    /// </summary>
    public abstract class FDEvent
    {
        public bool IsActive { get; private set; }

        public FDEventType EventType { get; private set; }

        public abstract bool IsTriggered(GameMap gameMap);

        public abstract void Execute(GameMap gameMap);
    }
}