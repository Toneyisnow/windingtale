using System;
using WindingTale.Core.Definitions;
using WindingTale.Core.Map;
using WindingTale.UI.Scenes.Game;

namespace WindingTale.Core.Events
{
    public class FDTurnEvent : FDEvent
    {
        public  int TurnNo { get; private set; }

        public CreatureFaction TurnType { get; private set; }

        public FDTurnEvent(int turnNo, CreatureFaction turnType, Action<GameMain> action) : base(action)
        {
            this.TurnNo = turnNo;
            this.TurnType = turnType;
        }
    }
}