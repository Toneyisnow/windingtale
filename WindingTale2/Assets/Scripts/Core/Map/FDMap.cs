using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WindingTale.Chapters;
using WindingTale.Core.Algorithms;
using WindingTale.Core.Common;
using WindingTale.Core.Definitions;
using WindingTale.Core.Definitions.Items;
using WindingTale.Core.Objects;

namespace WindingTale.Core.Map
{
    public class FDMap 
    {

        #region Properties

        public int TurnNo { get; set; }

        public CreatureFaction TurnType { get; set; }

        public FDField Field { get; private set; }


        public List<FDCreature> Creatures { get; private set; }

        public List<FDCreature> DeadCreatures
        {
            get; private set;
        }


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
        }

        #endregion

        #region Public Static Methods

        public static FDMap loadFromChapter(int chapterId)
        {
            var map = new FDMap();

            DefinitionStore.Instance.LoadChapter(chapterId);
            ChapterDefinition chapterDefinition = ChapterLoader.LoadChapter(chapterId);
            map.Field = new FDField(chapterDefinition);

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
            return null;
        }

        public FDCreature GetCreatureAt(FDPosition position)
        {
            return null;
        }


        public FDTreasure GetTreasureAt(FDPosition position)
        {
            return null;
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
            return null;
        }

        public List<FDCreature> GetOppositeCreatures(FDCreature  creature)
        {
            return null;
        }

        public List<FDCreature> GetAdjacentFriends(FDCreature creature)
        {
            return null;
        }


        public bool CanSaveGame()
        {
            // If none of friends have moved
            return true;
        }

        #endregion
    }

}
