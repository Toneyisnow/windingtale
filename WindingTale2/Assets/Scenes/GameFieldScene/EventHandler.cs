using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WindingTale.Core.Events;

namespace WindingTale.Scenes.GameFieldScene
{
    public class EventHandler
    {
        public List<FDEvent> events { get; private set;  }

        private GameMain gameMain;

        public EventHandler(List<FDEvent> events, GameMain gameMain)
        {
            this.events = events;
            this.gameMain = gameMain;
        }

        public void notifyTurnEvents()
        {
            foreach(FDEvent eve in events)
            {
                if (!eve.IsActive || eve.EventType != FDEventType.Turn) continue;

                var turnEvent = (FDTurnEvent)eve;
                if (turnEvent.TurnNo == gameMain.gameMap.Map.TurnNo
                    && turnEvent.TurnType == gameMain.gameMap.Map.TurnType)
                {
                    eve.Execute();
                }
            }
        }

        public void notifyTriggeredEvents()
        {
            foreach (FDEvent eve in events)
            {
                if (!eve.IsActive || eve.EventType != FDEventType.Condition) continue;

                var conditionEvent = (FDConditionEvent)eve;
                if (conditionEvent.Match(gameMain.gameMap.Map))
                {
                    eve.Execute();
                }
            }
        }


    }

}