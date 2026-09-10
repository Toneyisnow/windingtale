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
                if (!eve.IsActive || !eve.IsDependencySatisfied || eve.EventType != FDEventType.Turn) continue;

                var turnEvent = (FDTurnEvent)eve;
                if (turnEvent.TurnNo == gameMain.gameMap.Map.TurnNo
                    && turnEvent.TurnType == gameMain.gameMap.Map.TurnType)
                {
                    eve.Execute();
                }
            }
        }

        /// <summary>
        /// Fires every condition event that is due. Which ones are due is decided for the
        /// whole list first and only then are they executed, so an event that another
        /// event in the same pass waits on (FDEvent.AddDependentEvent) cannot go off in
        /// that same pass: a chain of dependent events advances one link per check.
        ///
        /// That is what the original did -- it walked its list backwards, and a chapter
        /// registers a dependency before what depends on it -- and chapters 29 and 30
        /// rely on it: their chains of TurnEndEvents are meant to fire one per turn.
        /// </summary>
        public void notifyTriggeredEvents()
        {
            List<FDEvent> due = new List<FDEvent>();
            foreach (FDEvent eve in events)
            {
                if (!eve.IsActive || !eve.IsDependencySatisfied || eve.EventType != FDEventType.Condition) continue;

                var conditionEvent = (FDConditionEvent)eve;
                if (conditionEvent.Match(gameMain.gameMap.Map))
                {
                    due.Add(eve);
                }
            }

            foreach (FDEvent eve in due)
            {
                eve.Execute();
            }
        }


    }

}