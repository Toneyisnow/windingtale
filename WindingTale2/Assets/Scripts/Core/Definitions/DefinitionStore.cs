using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net.Mime;
using UnityEngine;
using WindingTale.Core.Common;
using WindingTale.Core.Definitions.Items;
using WindingTale.Core.Files;

namespace WindingTale.Core.Definitions
{
    /// <summary>
    /// Main place in game to store all definitions
    /// </summary>
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

        private Dictionary<int, FightAnimation> fightAnimations = null;

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

            LoadFightAnimations();
        }

        private void LoadCreatureDefinitions()
        {
            // Load Strings file
            StringsDefinition creatureNames = ResourceJsonFile.Load<StringsDefinition>("Strings/Creature.strings");
            if (creatureNames == null)
            {
                throw new Exception("Cannot find strings for creatures.");
            }
            StringsDefinition creatureRaces = ResourceJsonFile.Load<StringsDefinition>("Strings/Race.strings");
            if (creatureRaces == null)
            {
                throw new Exception("Cannot find strings for creatures.");
            }

            creatureDefinitions = new Dictionary<int, CreatureDefinition>();

            ResourceDataFile fileReader = new ResourceDataFile(@"Data/Creature");
            int creatureCount = fileReader.ReadInt();

            for(int i = 0; i < creatureCount; i++)
            {
                CreatureDefinition def = CreatureDefinition.ReadFromFile(fileReader);
                def.Name = creatureNames.GetString(StringUtils.Digit3(def.DefinitionId));
                def.RaceName = creatureRaces.GetString(StringUtils.Digit3(def.Race));
                creatureDefinitions[def.DefinitionId] = def;
            }

            creatureBaseDefinitions = new Dictionary<int, CreatureDefinition>();

            ResourceDataFile fileReader2 = new ResourceDataFile(@"Data/LeveledCreature");
            int creatureBaseCount = fileReader2.ReadInt();

            for (int i = 0; i < creatureBaseCount; i++)
            {
                CreatureDefinition def = CreatureDefinition.ReadBaseFromFile(fileReader2);
                def.Name = creatureNames.GetString(StringUtils.Digit3(def.DefinitionId));
                creatureBaseDefinitions[def.DefinitionId] = def;
            }


        }

        private void LoadItemDefinitions()
        {
            // Load Strings file
            StringsDefinition stringsDefinition = ResourceJsonFile.Load<StringsDefinition>("Strings/Item.strings");
            if (stringsDefinition == null)
            {
                throw new Exception("Cannot find definition for items.");
            }


            itemDefinitions = new Dictionary<int, ItemDefinition>();
            ResourceDataFile fileReader = new ResourceDataFile(@"Data/Item");

            int usableItemCount = fileReader.ReadInt();
            for (int i = 0; i < usableItemCount; i++)
            {
                ConsumableItemDefinition def = ConsumableItemDefinition.ReadFromFile(fileReader);
                def.Name = stringsDefinition.GetString(def.ItemId.ToString());

                itemDefinitions[def.ItemId] = def;
            }

            int attackItemCount = fileReader.ReadInt();
            for (int i = 0; i < attackItemCount; i++)
            {
                AttackItemDefinition def = AttackItemDefinition.ReadFromFile(fileReader);
                def.Name = stringsDefinition.GetString(def.ItemId.ToString());
                itemDefinitions[def.ItemId] = def;
            }

            int defendItemCount = fileReader.ReadInt();
            for (int i = 0; i < defendItemCount; i++)
            {
                DefendItemDefinition def = DefendItemDefinition.ReadFromFile(fileReader);
                def.Name = stringsDefinition.GetString(def.ItemId.ToString());
                itemDefinitions[def.ItemId] = def;
            }

            int specialItemCount = fileReader.ReadInt();
            for (int i = 0; i < specialItemCount; i++)
            {
                SpecialItemDefinition def = SpecialItemDefinition.ReadFromFile(fileReader);
                def.Name = stringsDefinition.GetString(def.ItemId.ToString());
                itemDefinitions[def.ItemId] = def;
            }

            int moneyItemCount = fileReader.ReadInt();
            for (int i = 0; i < moneyItemCount; i++)
            {
                MoneyItemDefinition def = MoneyItemDefinition.ReadFromFile(fileReader);
                def.Name = stringsDefinition.GetString(def.ItemId.ToString());
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
            // Load Strings file
            StringsDefinition stringsDefinition = ResourceJsonFile.Load<StringsDefinition>("Strings/Occupation.strings");
            if (stringsDefinition == null)
            {
                throw new Exception("Cannot find strins file for occupations.");
            }

            occupationDefinitions = new Dictionary<int, OccupationDefinition>();
            ResourceDataFile fileReader = new ResourceDataFile(@"Data/Occupation");
            int count = fileReader.ReadInt();

            for (int i = 0; i < count; i++)
            {
                OccupationDefinition def = OccupationDefinition.ReadFromFile(fileReader, stringsDefinition);
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

        private void LoadFightAnimations()
        {
            fightAnimations = new Dictionary<int, FightAnimation>();
            ResourceDataFile fileReader = new ResourceDataFile(@"Data/FightAnimation");

            FightAnimation def = null;
            while ((def = FightAnimation.ReadFromFile(fileReader)) != null)
            {
                fightAnimations[def.AnimationId] = def;
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

        public FightAnimation GetFightAnimation(int animationId)
        {
            if (this.fightAnimations.ContainsKey(animationId))
            {
                return this.fightAnimations[animationId];
            }
            return null;
        }


    }
}