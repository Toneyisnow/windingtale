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

        public CreatureFaction TurnType { get; set; }

        public FDField Field { get; private set; }


        public List<FDCreature> Creatures { get; private set; }

        public List<FDCreature> DeadCreatures
        {
            get; private set;
        }

        public List<FDTreasure> Treasures { get; private set; }

        public List<int> TriggeredEvents { get; private set; }



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

            this.Creatures = new List<FDCreature>();
            this.DeadCreatures = new List<FDCreature>();
            this.Treasures = new List<FDTreasure>();

            this.TriggeredEvents = new List<int>();
        }

        #endregion

        #region Public Static Methods

        public static FDMap LoadFromChapter(int chapterId)
        {
            Debug.Log("FDMap loadFromChapter");
            var map = new FDMap();
            map.ChapterId = chapterId;

            ChapterDefinition chapterDefinition = DefinitionStore.Instance.LoadChapter(chapterId);
            map.Field = new FDField(chapterDefinition);

            return map;
        }

        public static FDMap LoadFromMapRecord(int chapterId, List<FDCreature> creatures, List<FDCreature> deadCreatures, List<FDTreasure> treasures, List<int> triggeredEvents, int turnNo = 1)
        {
            Debug.Log("FDMap LoadFromMapRecord");
            var map = LoadFromChapter(chapterId);

            map.TurnNo = turnNo;
            map.Creatures = creatures ?? new List<FDCreature>();
            map.DeadCreatures = deadCreatures ?? new List<FDCreature>();
            map.Treasures = treasures ?? new List<FDTreasure>();
            map.TriggeredEvents = triggeredEvents ?? new List<int>();
            return map;
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


        public FDCreature GetPreferredAttackTargetInRange(FDCreature creature, FDPosition targetPosition = null)
        {
            AttackItemDefinition attackItem = creature.GetAttackItem();
            if (attackItem == null)
            {
                return null;
            }

            FDSpan span = attackItem.AttackScope;
            Debug.Log("AttackScope: " + span.ToString());

            DirectRangeFinder finder = new DirectRangeFinder(this.Field, targetPosition ?? creature.Position, span.Max, span.Min);
            FDRange range = finder.CalculateRange();
            foreach (FDPosition pos in range.ToList())
            {
                Debug.Log("AttackScope range: " + pos.ToString());
            }

            foreach (FDCreature c in this.Enemies)
            {
                Debug.Log("Enemy: " + c.Position.ToString());
            }

            // Get a preferred target in range
            foreach (FDPosition pos in range.ToList())
            {
                FDCreature target = this.GetCreatureAt(pos);
                if (target != null && target.Faction == CreatureFaction.Enemy)
                {
                    return target;
                }
            }

            return null;
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
