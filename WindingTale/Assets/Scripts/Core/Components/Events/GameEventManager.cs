using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using WindingTale.Core.Definitions;

namespace WindingTale.Core.Components.Events
{
    public class GameEventManager
    {
        private IGameAction gameAction = null;

        private List<GameEvent> Events = null;

        public GameEventManager(IGameAction actions)
        {
            this.gameAction = actions;
            this.Events = new List<GameEvent>();
        }

        public void RegisterEvent(GameEvent even)
        {
            this.Events.Add(even);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="turnId"></param>
        /// <param name="faction"></param>
        public void NotifyTurnEvents(int turnId, CreatureFaction faction)
        {

        }

        public void NotifyTriggeredEvents()
        {

        }





    }
}