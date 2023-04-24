using WindingTale.Core.Map;

namespace WindingTale.Core.Events
{
    public class FDTurnEvent : FDEvent
    {
        public int TurnNo { get; private set; }

        public int TurnType { get; private set; }

        public FDTurnEvent()
    {
        }

        public override void Execute(GameMap gameMap)
        {
            throw new System.NotImplementedException();
        }

        public override bool IsTriggered(GameMap gameMap)
        {
            return gameMap.TurnNo == TurnNo && gameMap.TurnType == TurnType;
        }
    }