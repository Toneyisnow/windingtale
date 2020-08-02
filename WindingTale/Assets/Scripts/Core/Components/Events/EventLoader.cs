using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
        private GameEventManager eventManager = null;

        public EventLoader(GameEventManager eventManager)
        {
            this.eventManager = eventManager;
        }

        public virtual void LoadEvents()
        {

        }
    }
}
