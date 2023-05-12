using System;
using System.Collections.Generic;
using UnityEngineInternal;
using WindingTale.Core.Common;
using WindingTale.Core.Definitions;
using WindingTale.Core.Events;
using WindingTale.Core.Objects;
using WindingTale.UI.Activities;
using WindingTale.UI.Scenes.Game;

namespace WindingTale.Chapters
{
    public abstract class ChapterEvents
    {
        protected int chapterId = 0;


        public List<FDEvent> AllEvents { get; private set; }

        public ChapterEvents(int chapterId)
        {
            this.chapterId = chapterId;
            this.AllEvents = new List<FDEvent>();
        }

        protected void LoadTurnEvent(int eventId, int turnId, CreatureFaction turnType, Action<GameMain> action)
        {
            FDTurnEvent ev = new FDTurnEvent(eventId, turnId, turnType, action);
            this.AllEvents.Add(ev);
        }

        protected void LoadDeadEvent(int eventId, int creatureId, Action<GameMain> action)
        {
            CreatureDeadEvent dead = new CreatureDeadEvent(eventId, creatureId, action);
            this.AllEvents.Add(dead);
        }

        protected void LoadDyingEvent(int eventId, int creatureId, Action<GameMain> action)
        {
            CreatureDyingEvent dead = new CreatureDyingEvent(eventId, creatureId, action);
            this.AllEvents.Add(dead);
        }

        protected void LoadTeamEvent(int eventId, CreatureFaction faction, Action<GameMain> action)
        {
            TeamEliminatedEvent condition = new TeamEliminatedEvent(eventId, faction, action);
            this.AllEvents.Add(condition);
        }


        public static void ShowConversations(GameMain gameMain, int chapterId, int sequenceId, int min, int max)
        {
            for(int index = min; index <= max; index++)
            {
                Conversation conversation = Conversation.Create(chapterId, sequenceId, index);

                // TODO: Get creature id from conversation
                int creatureId = 1;
                FDCreature creature = gameMain.GameMap.GetCreatureById(creatureId);

                gameMain.ActivityManager.Push(new TalkActivity(conversation, creature));
            }

        }
    }
}