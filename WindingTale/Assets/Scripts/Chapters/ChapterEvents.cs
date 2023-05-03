using System;
using System.Collections.Generic;
using WindingTale.Core.Components.Events;
using WindingTale.Core.Definitions;
using WindingTale.Core.Events;
using WindingTale.UI.Scenes.Game;

namespace WindingTale.Chapters
{
    public abstract class ChapterEvents
    {
        private int curEventId = 0;

        public List<FDEvent> AllEvents { get; private set; }

        public ChapterEvents()
        {
            this.AllEvents = new List<FDEvent>();
        }

        protected void LoadTurnEvent(int turnId, CreatureFaction turnType, Action<GameMain> action)
        {
            FDTurnEvent ev = new FDTurnEvent(++curEventId, turnId, turnType, action);
            this.AllEvents.Add(ev);
        }

        protected void LoadDeadEvent(int creatureId, Action<GameMain> action)
        {
            CreatureDeadEvent dead = new CreatureDeadEvent(++curEventId, creatureId, action);
            this.AllEvents.Add(dead);
        }

        protected void LoadDyingEvent(int creatureId, Action<GameMain> action)
        {
            CreatureDyingEvent dead = new CreatureDyingEvent(++curEventId, creatureId, action);
            this.AllEvents.Add(dead);
        }

        protected void LoadTeamEvent(CreatureFaction faction, Action<GameMain> action)
        {
            TeamEliminatedEvent condition = new TeamEliminatedEvent(++curEventId, faction, action);
            this.AllEvents.Add(condition);
        }
    }
}