using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WindingTale.Chapters;
using WindingTale.Core.Common;
using WindingTale.Core.Definitions;
using WindingTale.Core.Objects;

namespace WindingTale.Core.Map
{
    public class FDMap 
    {
        public FDMap() { }

        public int TurnNo { get ; set; }

        public CreatureFaction TurnType { get; set; }

        public FDField Field { get; private set; }


        public List<FDCreature> Friends { get; private set; }

        public List<FDCreature> Enemies { get; private set; }

        public List<FDCreature> Npcs { get; private set; }

        public List<FDCreature> Creatures { get; private set; }

        public List<FDCreature> DeadCreatures
        {
            get; private set;
        }

        public FDCreature GetCreatureById(int creatureId)
        {
            return null;
        }


        public static FDMap loadFromChapter(int chapterId)
        {
            var map = new FDMap();

            DefinitionStore.Instance.LoadChapter(chapterId);
            ChapterDefinition chapterDefinition = ChapterLoader.LoadChapter(chapterId);
            map.Field = new FDField(chapterDefinition);

            return map;
        }

        public static FDMap loadFromRecord() { 
            return new FDMap();
        }


        public List<FDCreature> GetCreaturesInRange(List<FDPosition> range, CreatureFaction faction)
        {
            return null;
        }

        public List<FDCreature> GetOppositeCreatures(FDCreature  creature)
        {
            return null;
        }

        public FDCreature GetCreatureAt(FDPosition position) { 
            return null; 
        }

    }

}
