using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WindingTale.Core.Definitions;

namespace WindingTale.Core.Components.Events
{
    public class EventLoaderFactory
    {
        public static EventLoader GetEventLoader(int chapterId, GameEventManager eventManager)
        {
            switch(chapterId)
            {
                case 1:
                    return new EventChapter1(eventManager);
                case 2:
                    return new EventChapter2(eventManager);
                default:
                    return null;
            }
        }
    }


    public abstract class EventLoader
    {
        protected GameEventManager eventManager = null;

        public EventLoader(GameEventManager eventManager)
        {
            this.eventManager = eventManager;
        }

        public virtual void LoadEvents()
        {

        }

        protected void LoadTurnEvent(int eventId, int turnId, CreatureFaction turnPhase, Action<IGameAction> action)
        {
            TurnEventCondition condition = new TurnEventCondition(turnId, turnPhase);
            GameEvent even = new GameEvent(eventId, condition, action);

            eventManager.RegisterEvent(even);
        }
    }
}
