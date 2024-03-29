﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WindingTale.Core.Common;
using WindingTale.Core.Files;

namespace WindingTale.Core.Definitions
{
    /// <summary>
    /// 
    /// </summary>
    public class LevelUpMagicDefinition
    {
        public static int DefinitionKey(int definitionId, int level)
        {
            return definitionId * 100 + level;
        }

        public LevelUpMagicDefinition()
        {

        }

        public int CreatureDefinitionId
        {
            get; private set;
        }

        public int Level
        {
            get; private set;
        }

        public int MagicId
        {
            get; private set;
        }

        public int Key
        {
            get
            {
                return DefinitionKey(this.CreatureDefinitionId, this.Level);
            }
        }

        public static LevelUpMagicDefinition ReadFromFile(ResourceDataFile reader)
        {
            LevelUpMagicDefinition def = new LevelUpMagicDefinition();

            def.CreatureDefinitionId = reader.ReadInt();
            if (def.CreatureDefinitionId == -1)
            {
                return null;
            }

            def.Level = reader.ReadInt();
            def.MagicId = reader.ReadInt();

            return def;
        }
    }
}