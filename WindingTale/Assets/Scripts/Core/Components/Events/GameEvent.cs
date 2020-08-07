using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WindingTale.Core.Components.Events.Conditions;

namespace WindingTale.Core.Components.Events
{
    public class GameEvent
    {
        private Action<IGameAction> actionDelegate = null;


        public GameEvent(int eventId, EventCondition condition, Action<IGameAction> action)
        {
            this.EventId = eventId;
            this.Condition = condition;
            this.actionDelegate = action;

            this.IsActive = true;
        }

        public EventCondition Condition
        {
            get; set;
        }

        public int EventId
        {
            get; private set;
        }

        public bool IsActive
        {
            get; set;
        }

        public void Notify(IGameAction gameAction)
        {
            if (!this.IsActive)
            {
                return;
            }

            if(!this.Condition.IsMatched(gameAction))
            {
                return;
            }

            if (actionDelegate == null)
            {
                return;
            }

            actionDelegate(gameAction);

            this.IsActive = false;
        }
    }
}