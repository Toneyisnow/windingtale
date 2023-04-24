using WindingTale.Core.Map;

namespace WindingTale.Core.Events
{
    /// <summary>
    /// 
    /// </summary>
    public abstract class FDEvent
    {
        public bool IsActive { get; private set; }

        public abstract bool IsTriggered(GameMap gameMap);

        public abstract void Execute(GameMap gameMap);
    }
}