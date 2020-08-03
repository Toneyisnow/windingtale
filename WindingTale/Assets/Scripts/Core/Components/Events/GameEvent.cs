using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

        public void CheckAndExecute(IGameAction gameAction)
        {
            if (!this.IsActive)
            {
                return;
            }

            if(!this.Condition.Match(gameAction))
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