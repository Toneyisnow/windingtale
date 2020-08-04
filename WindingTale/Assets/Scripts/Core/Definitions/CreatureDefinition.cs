using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WindingTale.Common;

namespace WindingTale.Core.Definitions
{
    /// <summary>
    /// 
    /// </summary>
    public enum CreatureFaction
    {
        Friend = 0,
        Enemy = 1,
        Npc = 2,
    }

    /// <summary>
    /// 
    /// </summary>
    public class CreatureDefinition
    {
        public static readonly int MAX_ITEM_COUNT = 8;

        public static readonly int MAX_MAGIC_COUNT = 6;

        public int DefinitionId
        {
            get; set;
        }

        public int AnimationId
        {
            get; set;
        }

        public string Name
        {
            get; set;
        }

        public int Race
        {
            get; set;
        }

        public int Occupation
        {
            get; set;
        }

        public int InitialLevel
        {
            get; set;
        }

        public int InitialHp
        {
            get; set;
        }

        public int InitialMp
        {
            get; set;
        }

        public int InitialAp
        {
            get; set;
        }

        public int InitialDp
        {
            get; set;
        }

        public int InitialDx
        {
            get; set;
        }

        public int InitialMv
        {
            get; set;
        }

        public int InitialEx
        {
            get; set;
        }

        public List<int> Items
        {
            get; private set;
        }

        public List<int> Magics
        {
            get; private set;
        }



        public CreatureDefinition()
        {
            this.Items = new List<int>();
            this.Magics = new List<int>();
        }

        public static CreatureDefinition ReadFromFile(ResourceDataFile reader)
        {
            CreatureDefinition definition = new CreatureDefinition();

            definition.DefinitionId = reader.ReadInt();
            definition.AnimationId = definition.DefinitionId % 1000;
            definition.Name = "";       // TODO: Localize
            definition.Race = reader.ReadInt();
            definition.Occupation = reader.ReadInt();
            definition.InitialLevel = reader.ReadInt();
            definition.InitialAp = reader.ReadInt();
            definition.InitialDp = reader.ReadInt();
            definition.InitialDx = reader.ReadInt();
            definition.InitialHp = reader.ReadInt();
            definition.InitialMp = reader.ReadInt();
            definition.InitialMv = reader.ReadInt();
            definition.InitialEx = reader.ReadInt();

            int itemCount = reader.ReadInt();
            for(int i = 0; i < itemCount; i++)
            {
                int itemId = reader.ReadInt();
                definition.Items.Add(itemId);
            }

            int magicCount = reader.ReadInt();
            for (int i = 0; i < magicCount; i++)
            {
                int magicId = reader.ReadInt();
                definition.Magics.Add(magicId);
            }

            return definition;
        }

        /// <summary>
        /// Read creature base definition
        /// </summary>
        /// <param name="reader"></param>
        /// <returns></returns>
        public static CreatureDefinition ReadBaseFromFile(ResourceDataFile reader)
        {
            CreatureDefinition definition = new CreatureDefinition();

            definition.DefinitionId = reader.ReadInt();
            definition.AnimationId = definition.DefinitionId % 1000;
            definition.Name = "";       // TODO: Localize
            definition.Race = reader.ReadInt();
            definition.Occupation = reader.ReadInt();
            definition.InitialAp = reader.ReadInt();
            definition.InitialDp = reader.ReadInt();
            definition.InitialDx = reader.ReadInt();
            definition.InitialHp = reader.ReadInt();
            definition.InitialMp = reader.ReadInt();
            definition.InitialMv = reader.ReadInt();
            definition.InitialEx = reader.ReadInt();

            return definition;
        }

        /// <summary>
        /// Read creature definition according to base
        /// </summary>
        /// <param name="reader"></param>
        /// <returns></returns>
        public static CreatureDefinition ReadFromFile(ResourceDataFile reader, Dictionary<int, CreatureDefinition> creatureBases)
        {
            if (creatureBases == null)
            {
                return null;
            }

            CreatureDefinition definition = new CreatureDefinition();

            definition.DefinitionId = reader.ReadInt();
            int baseId = reader.ReadInt();

            definition.InitialLevel = reader.ReadInt();

            CreatureDefinition baseDef = creatureBases[baseId];
            if (baseDef == null)
            {
                return null;
            }

            definition.AnimationId = baseDef.AnimationId;
            definition.Name = baseDef.Name;
            definition.Race = baseDef.Race;
            definition.Occupation = baseDef.Occupation;
            definition.InitialAp = baseDef.InitialAp * definition.InitialLevel;
            definition.InitialDp = baseDef.InitialDp * definition.InitialLevel;
            definition.InitialDx = baseDef.InitialDx * definition.InitialLevel;
            definition.InitialHp = baseDef.InitialHp * definition.InitialLevel;
            definition.InitialMp = baseDef.InitialMp * definition.InitialLevel;
            definition.InitialMv = baseDef.InitialMv;
            definition.InitialEx = baseDef.InitialEx;

            int itemCount = reader.ReadInt();
            for (int i = 0; i < itemCount; i++)
            {
                int itemId = reader.ReadInt();
                definition.Items.Add(itemId);
            }

            int magicCount = reader.ReadInt();
            for (int i = 0; i < magicCount; i++)
            {
                int magicId = reader.ReadInt();
                definition.Magics.Add(magicId);
            }

            return definition;
        }

        public bool CanFly()
        {
            if (Occupation == 133 || Occupation == 171)
            {
                return true;
            }

            if (Race == 5)
            {
                return true;
            }

            if (Race == 6 && DefinitionId != 24)
            {
                return true;
            }

            if (Race == 9 && Occupation == 999)
            {
                return true;
            }

            return false;
        }

        public bool IsKnight()
        {
            return (Occupation == 133 || Occupation == 132);
        }

        public int GetMaxLevel()
        {
            if (Race == 7)
            {
                return 99;
            }

            return 40;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="itemId"></param>
        /// <returns></returns>
        public bool CanEquip(int itemId)
        {
            return true;
        }
    }
}
