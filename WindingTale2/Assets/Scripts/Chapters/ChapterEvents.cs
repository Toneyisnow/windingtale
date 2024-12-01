using System;
using System.Collections.Generic;
using UnityEngineInternal;
using WindingTale.Core.Common;
using WindingTale.Core.Definitions;
using WindingTale.Core.Events;
using WindingTale.Core.Objects;
using WindingTale.Scenes.GameFieldScene;
using WindingTale.Scenes.GameFieldScene.Activities;

namespace WindingTale.Chapters
{
    public abstract class ChapterEvents
    {
        protected int chapterId = 0;

        protected GameMain gameMain;


        public List<FDEvent> AllEvents { get; private set; }

        public ChapterEvents(GameMain gameMain, int chapterId)
        {
            this.gameMain = gameMain;
            this.chapterId = chapterId;
            this.AllEvents = new List<FDEvent>();
        }

        protected void LoadTurnEvent(int eventId, int turnId, CreatureFaction turnType, Action<GameMain> action)
        {
            FDTurnEvent ev = new FDTurnEvent(eventId, turnId, turnType, () => action(gameMain));
            this.AllEvents.Add(ev);
        }

        protected void LoadDeadEvent(int eventId, int creatureId, Action<GameMain> action)
        {
            CreatureDeadEvent dead = new CreatureDeadEvent(eventId, creatureId, () => action(gameMain));
            this.AllEvents.Add(dead);
        }

        protected void LoadDyingEvent(int eventId, int creatureId, Action<GameMain> action)
        {
            CreatureDyingEvent dead = new CreatureDyingEvent(eventId, creatureId, () => action(gameMain));
            this.AllEvents.Add(dead);
        }

        protected void LoadTeamEvent(int eventId, CreatureFaction faction, Action<GameMain> action)
        {
            TeamEliminatedEvent condition = new TeamEliminatedEvent(eventId, faction, () => action(gameMain));
            this.AllEvents.Add(condition);
        }

        public static FDCreature AddCreatureToMap(GameMain gameMain, CreatureFaction faction, int creatureId, int definitionId, FDPosition position, int dropItemId = 0, AITypes aiType = AITypes.AIType_Aggressive)
        {
            CreatureDefinition definition = DefinitionStore.Instance.GetCreatureDefinition(definitionId);
            FDCreature creature = faction == CreatureFaction.Friend ?
                 new FDCreature(creatureId, definition, faction) :
                 new FDAICreature(creatureId, definition, faction, aiType);

            gameMain.gameMap.AddCreature(creature, position);

            return creature;
        }


        public static void ShowConversations(GameMain gameMain, int chapterId, int sequenceId, int start, int end)
        {
            for(int index = start; index <= end; index++)
            {
                Conversation conversation = Conversation.Create(chapterId, sequenceId, index);

                // TODO: Get creature id from conversation
                int creatureId = 1;
                FDCreature creature = gameMain.gameMap.Map.GetCreatureById(creatureId);

                gameMain.PushActivity(new TalkActivity(conversation, creature ));
            }

        }
    }
}