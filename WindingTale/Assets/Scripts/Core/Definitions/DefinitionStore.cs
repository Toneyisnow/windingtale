using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net.Mime;
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

        private Dictionary<int, OccupationDefinition> occupationDefinitions = null;

        private Dictionary<int, LevelUpDefinition> levelUpDefinitions = null;

        private Dictionary<int, LevelUpMagicDefinition> levelUpMagicDefinitions = null;

        private Dictionary<int, TransfersDefinition> transfersDefinitions = null;

        private Dictionary<int, ShopDefinition> shopDefinitions = null;

        private Dictionary<int, ChapterDefinition> chapterDefinitions = null;


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
            LoadOccupationDefinitions();
            LoadLevelUpDefinitions();
            LoadLevelUpMagicDefinitions();
            LoadTransfersDefinitions();
            LoadShopDefinitions();
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

        private void LoadOccupationDefinitions()
        {
            occupationDefinitions = new Dictionary<int, OccupationDefinition>();
            ResourceDataFile fileReader = new ResourceDataFile(@"Data/Occupation");
            int count = fileReader.ReadInt();

            for (int i = 0; i < count; i++)
            {
                OccupationDefinition def = OccupationDefinition.ReadFromFile(fileReader);
                occupationDefinitions[def.OccupationId] = def;
            }
        }

        private void LoadLevelUpDefinitions()
        {
            levelUpDefinitions = new Dictionary<int, LevelUpDefinition>();

            ResourceDataFile fileReader = new ResourceDataFile(@"Data/LevelUp");
            int levelUpCount = fileReader.ReadInt();

            for (int i = 0; i < levelUpCount; i++)
            {
                LevelUpDefinition def = LevelUpDefinition.ReadFromFile(fileReader);
                levelUpDefinitions[def.DefinitionId] = def;
            }
        }

        private void LoadLevelUpMagicDefinitions()
        {
            levelUpMagicDefinitions = new Dictionary<int, LevelUpMagicDefinition>();
            ResourceDataFile fileReader = new ResourceDataFile(@"Data/LevelUpMagic");
            
            LevelUpMagicDefinition def = null;
            while ((def = LevelUpMagicDefinition.ReadFromFile(fileReader)) != null)
            {
                levelUpMagicDefinitions[def.Key] = def;
            }
        }

        private void LoadTransfersDefinitions()
        {
            transfersDefinitions = new Dictionary<int, TransfersDefinition>();
            ResourceDataFile fileReader = new ResourceDataFile(@"Data/Transfer");

            int count = fileReader.ReadInt();
            for(int i = 0; i < count; i++)
            {
                TransfersDefinition def = TransfersDefinition.ReadFromFile(fileReader);
                transfersDefinitions[def.DefinitionId] = def;
            }
        }

        private void LoadShopDefinitions()
        {
            shopDefinitions = new Dictionary<int, ShopDefinition>();
            ResourceDataFile fileReader = new ResourceDataFile(@"Data/Shop");

            ShopDefinition def = null;
            while((def = ShopDefinition.ReadFromFile(fileReader)) != null)
            {
                shopDefinitions[def.Key] = def;
            }
        }

        /// <summary>
        /// Load two files: chapter_N.dat for json, chapter_N_data.dat for plain text
        /// </summary>
        /// <param name="chapterId"></param>
        /// <returns></returns>
        public ChapterDefinition LoadChapter(int chapterId)
        {
            if (chapterDefinitions == null)
            {
                chapterDefinitions = new Dictionary<int, ChapterDefinition>();
            }

            if (chapterDefinitions.ContainsKey(chapterId))
            {
                return chapterDefinitions[chapterId];
            }

            // Load Chapter
            ChapterDefinition chapter = ResourceJsonFile.Load<ChapterDefinition>(string.Format(@"Data/Chapters/Chapter_{0}", StringUtils.Digit2(chapterId)));
            chapter.ChapterId = chapterId;

            // Load Chapter ConversationId
            ResourceDataFile conversationIdFile = new ResourceDataFile(string.Format(@"Data/Chapters/Chapter_{0}_ConversationId", StringUtils.Digit2(chapterId)));
            chapter.ReadConversationIdsFromFile(conversationIdFile);

            // Load Chapter Creatures
            creatureChapterDefinitions = new Dictionary<int, CreatureDefinition>();
            ResourceDataFile fileReader2 = new ResourceDataFile(string.Format(@"Data/Chapters/Chapter_{0}_Creature", StringUtils.Digit2(chapterId)));
            int cCount = fileReader2.ReadInt();

            for (int i = 0; i < cCount; i++)
            {
                CreatureDefinition def = CreatureDefinition.ReadFromFile(fileReader2, creatureBaseDefinitions);
                creatureChapterDefinitions[def.DefinitionId] = def;
            }

            chapterDefinitions[chapterId] = chapter;
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
            if (this.magicDefinitions.ContainsKey(magicId))
            {
                return this.magicDefinitions[magicId];
            }
            return null;
        }

        public ItemDefinition GetItemDefinition(int itemId)
        {
            if (this.itemDefinitions.ContainsKey(itemId))
            {
                return this.itemDefinitions[itemId];
            }
            return null;
        }

        public OccupationDefinition GetOccupationDefinition(int occupationId)
        {
            if (this.occupationDefinitions.ContainsKey(occupationId))
            {
                return this.occupationDefinitions[occupationId];
            }
            return null;
        }

        public LevelUpDefinition GetLevelUpDefinition(int definitionId)
        {
            if (this.levelUpDefinitions.ContainsKey(definitionId))
            {
                return this.levelUpDefinitions[definitionId];
            }
            return null;
        }

        public LevelUpMagicDefinition GetLevelUpMagicDefinition(int definitionId, int level)
        {
            int key = LevelUpMagicDefinition.DefinitionKey(definitionId, level);
            if (this.levelUpMagicDefinitions.ContainsKey(key))
            {
                return this.levelUpMagicDefinitions[key];
            }
            return null;
        }

        public TransfersDefinition GetTransfersDefinition(int definitionId)
        {
            if (this.transfersDefinitions.ContainsKey(definitionId))
            {
                return this.transfersDefinitions[definitionId];
            }
            return null;
        }

        public ShopDefinition GetShopDefinition(int chapterId, ShopType shopType)
        {
            int key = ShopDefinition.DefinitionKey(chapterId, shopType);
            if (this.shopDefinitions.ContainsKey(key))
            {
                return this.shopDefinitions[key];
            }
            return null;
        }


    }
}