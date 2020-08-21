using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using WindingTale.Common;
using WindingTale.Core.Definitions.Items;

namespace WindingTale.Core.Definitions
{


    public class DefinitionStore
    {
        private static DefinitionStore instance = null;

        private Dictionary<int, CreatureDefinition> creatureDefinitions = null;
        private Dictionary<int, CreatureDefinition> creatureBaseDefinitions = null;
        private Dictionary<int, CreatureDefinition> creatureChapterDefinitions = null;

        private Dictionary<int, MagicDefinition> magicDefinitions = null;

        private Dictionary<int, ItemDefinition> itemDefinitions = null;

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
            itemDefinitions = new Dictionary<int, ItemDefinition>();
            ResourceDataFile fileReader = new ResourceDataFile(@"Data/Item");

            int usableItemCount = fileReader.ReadInt();
            for (int i = 0; i < usableItemCount; i++)
            {
                ConsumableItemDefinition def = ConsumableItemDefinition.ReadFromFile(fileReader);
                itemDefinitions[def.ItemId] = def;
            }

            int attackItemCount = fileReader.ReadInt();
            for (int i = 0; i < attackItemCount; i++)
            {
                AttackItemDefinition def = AttackItemDefinition.ReadFromFile(fileReader);
                itemDefinitions[def.ItemId] = def;
            }

            int defendItemCount = fileReader.ReadInt();
            for (int i = 0; i < defendItemCount; i++)
            {
                DefendItemDefinition def = DefendItemDefinition.ReadFromFile(fileReader);
                itemDefinitions[def.ItemId] = def;
            }

            int specialItemCount = fileReader.ReadInt();
            for (int i = 0; i < specialItemCount; i++)
            {
                SpecialItemDefinition def = SpecialItemDefinition.ReadFromFile(fileReader);
                itemDefinitions[def.ItemId] = def;
            }

            int moneyItemCount = fileReader.ReadInt();
            for (int i = 0; i < moneyItemCount; i++)
            {
                MoneyItemDefinition def = MoneyItemDefinition.ReadFromFile(fileReader);
                itemDefinitions[def.ItemId] = def;
            }
        }

        private void LoadMagicDefinitions()
        {
            magicDefinitions = new Dictionary<int, MagicDefinition>();
            ResourceDataFile fileReader = new ResourceDataFile(@"Data/Magic");
            int magicCount = fileReader.ReadInt();

            for (int i = 0; i < magicCount; i++)
            {
                MagicDefinition def = MagicDefinition.ReadFromFile(fileReader);
                magicDefinitions[def.MagicId] = def;
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
            return null;
        }

        public ItemDefinition GetItemDefinition(int itemId)
        {
            return null;
        }
    }
}