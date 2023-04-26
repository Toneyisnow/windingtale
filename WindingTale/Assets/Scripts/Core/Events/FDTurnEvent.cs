using WindingTale.Core.Definitions;
using WindingTale.Core.Map;

namespace WindingTale.Core.Events
{
    public class FDTurnEvent : FDEvent
    {
        public  int TurnNo { get; private set; }

        public CreatureFaction TurnType { get; private set; }

        public FDTurnEvent(int turnNo, CreatureFaction turnType)
        {
            this.TurnNo = turnNo;
            this.TurnType = turnType;
        }
    }
}