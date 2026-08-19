using System;
using System.Collections.Generic;
using UnityEngineInternal;
using WindingTale.Core.Common;
using WindingTale.Core.Definitions;
using WindingTale.Core.Events;
using WindingTale.Core.Files;
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

        /// <summary>
        /// Puts a creature on the map where the chapter script asks for it. Enemies and
        /// NPCs belong to the chapter, so they are always built fresh from their definition.
        ///
        /// A friend may instead be one the player has been playing all along: if the party
        /// walked in from the village carrying this creature id, it is restored from that
        /// record -- level, HP, items, magic, experience -- and only its position comes from
        /// the chapter. Returns null for a friend who fell in an earlier chapter and has not
        /// been revived: they are still in the party record, but they do not take the field.
        /// </summary>
        public static FDCreature AddCreatureToMap(GameMain gameMain, CreatureFaction faction, int creatureId, int definitionId, FDPosition position, int dropItemId = 0, AITypes aiType = AITypes.AIType_Aggressive)
        {
            FDCreature creature = null;

            if (faction == CreatureFaction.Friend)
            {
                CreatureMapRecord carried = FindInParty(gameMain, creatureId);
                if (carried != null)
                {
                    if (carried.Hp <= 0)
                    {
                        // Fallen and not yet revived: the chapter goes on without them.
                        return null;
                    }

                    // On a copy: the restored creature would otherwise go on sharing its
                    // item and magic lists with the party record, which is meant to stay
                    // the snapshot of what walked in.
                    creature = GameMapRecordManager.CreateCreatureFromRecord(carried.Clone());
                }
            }

            if (creature == null)
            {
                CreatureDefinition definition = DefinitionStore.Instance.GetCreatureDefinition(definitionId);
                creature = faction == CreatureFaction.Friend ?
                     new FDCreature(creatureId, definition, faction) :
                     new FDAICreature(creatureId, definition, faction, aiType);
            }

            gameMain.gameMap.AddCreature(creature, position);

            return creature;
        }

        /// <summary>
        /// The party record's entry for this creature id, or null when the party carries no
        /// such creature -- which covers a battle nobody walked into (a New Game) as well as
        /// a friend who only joins during this chapter.
        /// </summary>
        private static CreatureMapRecord FindInParty(GameMain gameMain, int creatureId)
        {
            List<CreatureMapRecord> party = gameMain.PartyRecord?.Friends;
            return party?.Find(friend => friend.Id == creatureId);
        }


        public static void PushConversationsActivities(GameMain gameMain, int chapterId, int sequenceId, int start, int end)
        {
            ChapterDefinition chapterDefinition = DefinitionStore.Instance.LoadChapter(chapterId);

            for(int index = start; index <= end; index++)
            {
                Conversation conversation = Conversation.Create(chapterId, sequenceId, index);
                int creatureId = chapterDefinition.GetConversationCreatureId(conversation);
                
                gameMain.PushActivity(new TalkActivity(conversation, creatureId ));
            }
        }


        internal virtual void AdjustFriendsAfterWon()
        {
            // By default, do nothing. Each chapter can override this to adjust the friends after winning.
        }
    }
}