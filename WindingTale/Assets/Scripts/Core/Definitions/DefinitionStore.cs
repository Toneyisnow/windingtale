using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using WindingTale.Common;

namespace WindingTale.Core.Definitions
{


    public class DefinitionStore
    {
        private static DefinitionStore instance = null;

        private Dictionary<int, CreatureDefinition> creatureDefinitions = null;
        private Dictionary<int, CreatureDefinition> creatureBaseDefinitions = null;
        private Dictionary<int, CreatureDefinition> creatureChapterDefinitions = null;

        private Dictionary<int, MagicDefinition> magicDefinitions = null;

        private DefinitionStore()
        {

        }

        public static DefinitionStore Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new DefinitionStore();
                    instance.LoadAll();
                }
                return instance;
            }
        }

        private void LoadAll()
        {
            LoadCreatureDefinitions();

            LoadItemDefinitions();

            LoadMagicDefinitions();
        }

        private void LoadCreatureDefinitions()
        {
            creatureDefinitions = new Dictionary<int, CreatureDefinition>();

            ResourceDataFile fileReader = new ResourceDataFile(@"Data/Creature");
            int creatureCount = fileReader.ReadInt();

            for(int i = 0; i < creatureCount; i++)
            {
                CreatureDefinition def = CreatureDefinition.ReadFromFile(fileReader);
                creatureDefinitions[def.DefinitionId] = def;
            }

            creatureBaseDefinitions = new Dictionary<int, CreatureDefinition>();

            ResourceDataFile fileReader2 = new ResourceDataFile(@"Data/LeveledCreature");
            int creatureBaseCount = fileReader2.ReadInt();

            for (int i = 0; i < creatureBaseCount; i++)
            {
                CreatureDefinition def = CreatureDefinition.ReadBaseFromFile(fileReader2);
                creatureBaseDefinitions[def.DefinitionId] = def;
            }


        }

        private void LoadItemDefinitions()
        {

        }

        private void LoadMagicDefinitions()
        {
            magicDefinitions = new Dictionary<int, MagicDefinition>();
            ResourceDataFile fileReader = new ResourceDataFile(@"Data/Magic");
            int magicCount = fileReader.ReadInt();

            for (int i = 0; i < magicCount; i++)
            {
                MagicDefinition def = MagicDefinition.ReadFromFile(fileReader);
                creatureDefinitions[def.DefinitionId] = def;
            }
        }

        /// <summary>
        /// Load two files: chapter_N.dat for json, chapter_N_data.dat for plain text
        /// </summary>
        /// <param name="chapterId"></param>
        /// <returns></returns>
        public ChapterDefinition LoadChapter(int chapterId)
        {
            ChapterDefinition chapter = ResourceJsonFile.Load<ChapterDefinition>(string.Format(@"Data/Chapters/Chapter_{0}", chapterId));
            chapter.ChapterId = chapterId;

            // Load Chapter Creatures
            creatureChapterDefinitions = new Dictionary<int, CreatureDefinition>();
            ResourceDataFile fileReader2 = new ResourceDataFile(string.Format(@"Data/Chapters/Chapter_{0}_Creature", chapterId));
            int cCount = fileReader2.ReadInt();

            for (int i = 0; i < cCount; i++)
            {
                CreatureDefinition def = CreatureDefinition.ReadFromFile(fileReader2, creatureBaseDefinitions);
                creatureChapterDefinitions[def.DefinitionId] = def;
            }

            return chapter;
        }

        public CreatureDefinition GetCreatureDefinition(int creatureDefId)
        {
            if (creatureDefinitions.ContainsKey(creatureDefId))
            {
                return creatureDefinitions[creatureDefId];
            }

            if (creatureChapterDefinitions.ContainsKey(creatureDefId))
            {
                return creatureChapterDefinitions[creatureDefId];
            }

            return null;
        }

        public MagicDefinition GetMagicDefinition(int magicId)
        {
            if (this.magic)
        }

    }
}