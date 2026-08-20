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

        /// <summary>
        /// Fires at the *start* of the given phase: turn turnId, the phase belonging to
        /// turnType. Turn numbers are 1-based -- (1, Friend) is the opening of the battle,
        /// before the player has moved anything.
        ///
        /// Porting one of the original chapters needs care here, because the original fired
        /// its turn events at the *end* of a phase (TurnCondition only matched once every
        /// creature of that faction had acted). Each phase boundary therefore shifts by one:
        ///
        ///   [loadTurnEvent:TurnType_Friend Turn:0] -> (1, Friend)   (the opening cutscene)
        ///   [loadTurnEvent:TurnType_Friend Turn:N] -> (N, Npc)
        ///   [loadTurnEvent:TurnType_NPC    Turn:N] -> (N, Enemy)
        ///   [loadTurnEvent:TurnType_Enemy  Turn:N] -> (N + 1, Friend)
        ///
        /// The turn number itself does not shift: the original's runGame raised turnNo to 1
        /// before the first friend turn, so its rounds were 1-based too, and its Turn:0 was
        /// the special "before the battle starts" slot rather than a round of its own.
        /// </summary>
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
        ///
        /// Leave aiType out and the creature behaves the way its trade suggests -- see
        /// GetDefaultAiType.
        /// </summary>
        public static FDCreature AddCreatureToMap(GameMain gameMain, CreatureFaction faction, int creatureId, int definitionId, FDPosition position, int dropItemId = 0, AITypes? aiType = null)
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
                     new FDAICreature(creatureId, definition, faction, aiType ?? GetDefaultAiType(definition));
            }

            gameMain.gameMap.AddCreature(creature, position);

            return creature;
        }

        /// <summary>
        /// How a creature behaves when the chapter script does not say: healers (occupations
        /// 154 and 155) look after their own side, everybody else charges.
        /// </summary>
        private static AITypes GetDefaultAiType(CreatureDefinition definition)
        {
            if (definition != null && (definition.Occupation == 154 || definition.Occupation == 155))
            {
                return AITypes.AIType_Defensive;
            }

            return AITypes.AIType_Aggressive;
        }

        /// <summary>
        /// Switches how a creature already on the map behaves -- the guards that stop lying
        /// in wait once the party is spotted, the boss that stops holding its post.
        /// </summary>
        public static void SetCreatureAiType(GameMain gameMain, int creatureId, AITypes aiType)
        {
            FDAICreature creature = gameMain.gameMap.Map.GetCreatureById(creatureId) as FDAICreature;
            if (creature == null)
            {
                return;
            }

            creature.AIType = aiType;
        }

        /// <summary>
        /// Sends a creature running for a tile: it ignores the battle from now on and heads
        /// there as fast as it can. What happens when it arrives is up to the chapter -- an
        /// event watching that tile.
        /// </summary>
        public static void SetCreatureAiEscape(GameMain gameMain, int creatureId, FDPosition escapePosition)
        {
            FDAICreature creature = gameMain.gameMap.Map.GetCreatureById(creatureId) as FDAICreature;
            if (creature == null)
            {
                return;
            }

            creature.AIType = AITypes.AIType_Escape;
            creature.EscapePosition = escapePosition;
        }

        /// <summary>
        /// Sets a creature after one particular chest: it makes for it, takes what is inside,
        /// and then runs for the escape tile. If the party gets to the chest first it skips
        /// straight to running.
        /// </summary>
        public static void SetCreatureAiTreasure(GameMain gameMain, int creatureId, FDPosition treasurePosition, FDPosition escapePosition)
        {
            FDAICreature creature = gameMain.gameMap.Map.GetCreatureById(creatureId) as FDAICreature;
            if (creature == null)
            {
                return;
            }

            creature.AIType = AITypes.AIType_Treasure;
            creature.TreasurePosition = treasurePosition;
            creature.EscapePosition = escapePosition;
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