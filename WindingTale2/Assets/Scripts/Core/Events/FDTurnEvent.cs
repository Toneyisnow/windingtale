using System;
using WindingTale.Core.Definitions;
using WindingTale.Core.Map;
namespace WindingTale.Core.Events
{
    public class FDTurnEvent : FDEvent
    {
        public  int TurnNo { get; private set; }

        public CreatureFaction TurnType { get; private set; }

        public FDTurnEvent(int eventId, int turnNo, CreatureFaction turnType, Action action) : base(eventId, action)
        {
            this.TurnNo = turnNo;
            this.TurnType = turnType;
        }
    }
}