using WindingTale.Core.Definitions;
using WindingTale.Core.Events;
using WindingTale.Core.Objects;
using WindingTale.Core.Files;
using System.Collections.Generic;
using WindingTale.Core.Common;
using WindingTale.Core.Algorithms;
using WindingTale.Core.Definitions.Items;
using System;
using WindingTale.Chapters;

namespace WindingTale.Core.Map
{
    /// <summary>
    /// This is the main function for Battle Map in the game.
    /// </summary>
    public class GameMap
    {
        
        public int ChapterId { get; private set; }

        public GameField Field { get; private set; }


        /// <summary>
        /// 
        /// </summary>
        public List<FDEvent> Events { get; private set; }

        public List<FDTreasure> Treasures { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        public List<FDCreature> Creatures { get; private set; }

        public List<FDCreature> DeadCreatures { get; private set; }

        public List<FDCreature> Friends
        {
            get
            {
                return this.Creatures.FindAll(c => c.Faction == CreatureFaction.Friend);
            }
        }

        public List<FDCreature> Npcs
        {
            get
            {
                return this.Creatures.FindAll(c => c.Faction == CreatureFaction.Npc);
            }
        }
        public List<FDCreature> Enemies
        {
            get
            {
                return this.Creatures.FindAll(c => c.Faction == CreatureFaction.Enemy);
            }
        }

        public List<FDCreature> GetOppositeCreatures(FDCreature creature)
        {
            if (creature.Faction == CreatureFaction.Enemy)
            {
                List<FDCreature> arr = new List<FDCreature>();
                arr.AddRange(this.Friends);
                arr.AddRange(this.Npcs);
                return arr;
            }
            else
            {
                return this.Enemies;
            }
        }

        #region Constructors

        public GameMap()
        {
            this.Creatures = new List<FDCreature>();
        }

        #endregion


        #region Calculated Functions

        public FDCreature GetCreatureById(int creatureId)
        {
            return this.Creatures.Find(c => c.Id == creatureId);
        }


        internal FDCreature GetCreatureAt(FDPosition position)
        {
            return this.Creatures.Find(c => c.Position.AreSame(position));
        }

        public List<FDCreature> GetAdjacentFriends(int creatureId)
        {
            FDCreature creature = GetCreatureById(creatureId);

            List<FDCreature> friends = new List<FDCreature>();
            foreach (FDPosition position in creature.Position.GetAdjacentPositions())
            {
                FDCreature friend = GetCreatureAt(position);
                if (friend != null && friend.Faction == CreatureFaction.Friend)
                {
                    friends.Add(friend);
                }
            }
            return friends;
        }

        public List<FDCreature> GetCreaturesInRange(List<FDPosition> range, CreatureFaction faction)
        {
            List<FDCreature> creatures = new List<FDCreature>();
            foreach (FDPosition position in range)
            {
                FDCreature creature = GetCreatureAt(position);
                if (creature != null && creature.Faction == faction)
                {
                    creatures.Add(creature);
                }
            }
            return creatures;
        }

        public FDCreature GetPreferredAttackTargetInRange(FDCreature creature)
        {
            AttackItemDefinition attackItem = creature.GetAttackItem();
            if (attackItem == null)
            {
                return null;
            }

            FDSpan span = attackItem.AttackScope;
            DirectRangeFinder finder = new DirectRangeFinder(this.Field, creature.Position, span.Max, span.Min);
            FDRange range = finder.CalculateRange();

            // Get a preferred target in range
            foreach (FDPosition position in range.ToList())
            {
                FDCreature target = this.GetCreatureAt(position);
                if (target != null && target.Faction == CreatureFaction.Enemy)
                {
                    return target;
                }
            }

            return null;
        }

        public FDTreasure GetTreatureAt(FDPosition position)
        {
            return this.Treasures.Find(t => t.Position.AreSame(position));
        }

        public List<FDEvent> GetActiveEvents()
        {
            return this.Events.FindAll(ev => ev.IsActive);
        }


        public bool CanSaveGame()
        {
            return this.Friends.Find(friend => friend.HasActioned) == null;
        }

        #endregion

        public static GameMap LoadFromChapter(ChapterDefinition chapter, GameRecord record)
        {
            GameMap map = new GameMap();
            map.Events = ChapterLoader.LoadEvents(chapter.ChapterId);

            return map;
        }

        public static GameMap LoadFromSave(GameMapRecord mapRecord)
        {
            return null;
        }

    }
}