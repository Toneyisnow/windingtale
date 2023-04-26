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
        public bool IsActive { get; protected set; }

        public FDEventType EventType { get; protected set; }

        public void Execute(GameMap gameMap)
        {
            DoExecute(gameMap);
            this.IsActive = false;
        }

        /// <summary>
        /// Interface for higher level class to implement details
        /// </summary>
        /// <param name="gameMap"></param>
        public abstract void DoExecute(GameMap gameMap);
    }
}