using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WindingTale.Core.Definitions;

namespace WindingTale.Core.Components.Events.Conditions
{
    public class TurnEventCondition : EventCondition
    {
        public int TurnId
        {
            get; set;
        }

        public CreatureFaction TurnPhase
        {
            get; set;
        }

        public TurnEventCondition(int turnId, CreatureFaction phase)
        {
            this.Type = ConditionType.Turn;

            this.TurnId = turnId;
            this.TurnPhase = phase;
        }

        public override bool IsMatched(IGameAction gameAction)
        {
            return gameAction.TurnId() == TurnId && gameAction.TurnPhase() == TurnPhase;
        }
    }
}