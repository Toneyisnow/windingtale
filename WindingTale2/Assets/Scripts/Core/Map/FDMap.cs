using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using WindingTale.Chapters;
using WindingTale.Core.Algorithms;
using WindingTale.Core.Common;
using WindingTale.Core.Definitions;
using WindingTale.Core.Definitions.Items;
using WindingTale.Core.Events;
using WindingTale.Core.Objects;

namespace WindingTale.Core.Map
{
    public class FDMap 
    {

        #region Properties

        public int ChapterId { get; set; }

        public int TurnNo { get; set; }

        /// <summary>
        /// The player's purse, carried across the battle and into the save.
        /// </summary>
        public int TotalMoney { get; set; }

        public CreatureFaction TurnType { get; set; }

        public FDField Field { get; private set; }


        public List<FDCreature> Creatures { get; set; }

        public List<FDCreature> DeadCreatures
        {
            get; set;
        }

        public List<FDTreasure> Treasures { get; set; }

        //// public List<int> TriggeredEvents { get; private set; }



        public List<FDCreature> Friends
        {
            get
            {
                return this.Creatures.FindAll(creature => creature.Faction == CreatureFaction.Friend);
            }
        }


        public List<FDCreature> Enemies
        {
            get
            {
                return this.Creatures.FindAll(creature => creature.Faction == CreatureFaction.Enemy);
            }
        }

        public List<FDCreature> Npcs
        {
            get
            {
                return this.Creatures.FindAll(creature => creature.Faction == CreatureFaction.Npc);
            }
        }

        #endregion

        #region Constructors

        public FDMap() {

            this.TurnNo = 1;
            this.TurnType = CreatureFaction.Friend;
            this.TotalMoney = 0;

            this.Creatures = new List<FDCreature>();
            this.DeadCreatures = new List<FDCreature>();
            this.Treasures = new List<FDTreasure>();

        }

        #endregion

        #region Public Static Methods

        public static FDMap LoadFromChapter(int chapterId)
        {
            Debug.Log("FDMap loadFromChapter");
            var map = new FDMap();
            map.ChapterId = chapterId;
            map.TurnNo = 0;
            map.TurnType = CreatureFaction.Enemy;

            ChapterDefinition chapterDefinition = DefinitionStore.Instance.LoadChapter(chapterId);
            map.Field = new FDField(chapterDefinition);
            map.Treasures = LoadTreasures(chapterDefinition);

            return map;
        }

        /// <summary>
        /// Builds the map's treasures from the chapter's "Treasures" array. Chapters with
        /// no treasures (or an older chapter file without the array) simply get none.
        /// </summary>
        private static List<FDTreasure> LoadTreasures(ChapterDefinition chapterDefinition)
        {
            List<FDTreasure> treasures = new List<FDTreasure>();

            if (chapterDefinition == null || chapterDefinition.Treasures == null)
            {
                return treasures;
            }

            foreach (TreasureDefinition definition in chapterDefinition.Treasures)
            {
                if (definition == null || definition.Position == null)
                {
                    continue;
                }

                FDTreasure treasure = new FDTreasure(definition.TreasureId, definition.ItemId);
                treasure.Position = FDPosition.At(definition.Position.X, definition.Position.Y);
                treasures.Add(treasure);
            }

            return treasures;
        }

        public static FDMap loadFromRecord()
        {
            return new FDMap();
        }


        #endregion

        #region Public Methods

        public FDCreature GetCreatureById(int creatureId)
        {
            return Creatures.Find(creature => creature.Id == creatureId);
        }

        public FDCreature GetCreatureAt(FDPosition position)
        {
            return Creatures.Find(creature => creature.Position.AreSame(position));
        }


        public FDTreasure GetTreasureAt(FDPosition position)
        {
            return Treasures.Find(treasure => treasure.Position.AreSame(position));
        }


        /// <summary>
        /// The enemy to recommend as the default target inside the given indicator range:
        /// of the enemies standing in it, the one farthest from the acting creature. Ties
        /// break on the lower creature id so the pick stays stable (the range is a set, so
        /// its own order means nothing). Null when the range holds no enemy.
        /// </summary>
        public FDCreature GetPreferredAttackTargetInRange(FDCreature creature, FDRange range)
        {
            if (creature == null)
            {
                return null;
            }

            return FindFarthestEnemyInRange(range, creature.Position);
        }

        /// <summary>
        /// Same, over the creature's own attack range -- the range its attack indicator
        /// shows. Pass targetPosition to range it from a tile the creature has yet to move
        /// to, rather than where it currently stands.
        /// </summary>
        public FDCreature GetPreferredAttackTargetInRange(FDCreature creature, FDPosition targetPosition = null)
        {
            AttackItemDefinition attackItem = creature.GetAttackItem();
            if (attackItem == null)
            {
                return null;
            }

            FDSpan span = attackItem.AttackScope;
            FDPosition attackFrom = targetPosition ?? creature.Position;

            DirectRangeFinder finder = new DirectRangeFinder(this.Field, attackFrom, span.Max, span.Min);
            return FindFarthestEnemyInRange(finder.CalculateRange(), attackFrom);
        }

        /// <summary>
        /// The friend or NPC to recommend as the default target inside the given indicator
        /// range: of those standing in it, the one with the lowest creature id. Set
        /// includeSelf to false to skip the acting creature itself (an item exchange needs
        /// a second party). Null when the range holds neither friend nor NPC.
        /// </summary>
        public FDCreature GetPreferredFriendOrNpcTargetInRange(FDCreature creature, FDRange range, bool includeSelf)
        {
            if (range == null)
            {
                return null;
            }

            HashSet<FDPosition> positions = new HashSet<FDPosition>(range.ToList());

            FDCreature first = null;

            foreach (FDCreature candidate in this.Creatures)
            {
                if (candidate.Faction == CreatureFaction.Enemy || !IsInPositions(candidate, positions))
                {
                    continue;
                }

                if (!includeSelf && creature != null && candidate.Id == creature.Id)
                {
                    continue;
                }

                if (first == null || candidate.Id < first.Id)
                {
                    first = candidate;
                }
            }

            return first;
        }

        private FDCreature FindFarthestEnemyInRange(FDRange range, FDPosition from)
        {
            if (range == null || from == null)
            {
                return null;
            }

            HashSet<FDPosition> positions = new HashSet<FDPosition>(range.ToList());

            FDCreature farthest = null;
            int farthestDistance = -1;

            foreach (FDCreature candidate in this.Creatures)
            {
                if (candidate.Faction != CreatureFaction.Enemy || !IsInPositions(candidate, positions))
                {
                    continue;
                }

                int distance = GetDirectDistance(from, candidate.Position);
                if (distance > farthestDistance || (distance == farthestDistance && candidate.Id < farthest.Id))
                {
                    farthestDistance = distance;
                    farthest = candidate;
                }
            }

            return farthest;
        }

        private static bool IsInPositions(FDCreature creature, HashSet<FDPosition> positions)
        {
            return creature.Position != null && positions.Contains(creature.Position);
        }

        // Manhattan distance -- the metric the direct ranges are themselves built on,
        // see DirectRangeFinder.
        private static int GetDirectDistance(FDPosition from, FDPosition to)
        {
            return Mathf.Abs(from.X - to.X) + Mathf.Abs(from.Y - to.Y);
        }

        public List<FDCreature> GetCreaturesInRange(List<FDPosition> range, CreatureFaction faction)
        {
            List<FDCreature> results = new List<FDCreature>();

            foreach(FDCreature creature in this.Creatures)
            {
                if (range.Find(pos => pos.AreSame(creature.Position)) != null && creature.Faction == faction)
                {
                    results.Add(creature);
                }
            }

            return results;
        }

        public List<FDCreature> GetOppositeCreatures(FDCreature  creature)
        {
            if (creature.Faction == CreatureFaction.Enemy) {
                return this.Creatures.FindAll(c => c.Faction == CreatureFaction.Friend || c.Faction == CreatureFaction.Npc);
            }

            return this.Enemies;
        }

        public List<FDCreature> GetAdjacentFriends(FDCreature creature)
        {
            List<FDCreature> candidates = this.Creatures.FindAll(c => c.Faction == CreatureFaction.Friend || c.Faction == CreatureFaction.Npc);
            return candidates.FindAll(c => c.Position.IsNextTo(creature.Position));
        }

        public bool HasAllCreaturesActioned(CreatureFaction faction)
        {
            return !this.Creatures.Any(creature => creature.Faction == faction && !creature.HasActioned);
        }


        public bool CanSaveGame()
        {
            // If none of friends have moved
            return true;
        }

        #endregion
    }

}
