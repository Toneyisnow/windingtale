using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WindingTale.Core.Common;
using WindingTale.Core.Files;

namespace WindingTale.Core.Definitions
{
    public class LevelUpDefinition
    {
        public int DefinitionId
        {
            get; 
            private set; 
        }

        public FDSpan HpRange
        {
            get; private set;
        }

        public FDSpan MpRange
        {
            get; private set;
        }

        public FDSpan ApRange
        {
            get; private set;
        }

        public FDSpan DpRange
        {
            get; private set;
        }

        public FDSpan DxRange
        {
            get; private set;
        }

        public static LevelUpDefinition ReadFromFile(ResourceDataFile reader)
        {
            LevelUpDefinition def = new LevelUpDefinition();

            def.DefinitionId = reader.ReadInt();

            def.HpRange = reader.ReadSpan();
            def.MpRange = reader.ReadSpan();
            def.ApRange = reader.ReadSpan();
            def.DpRange = reader.ReadSpan();
            def.DxRange = reader.ReadSpan();

            return def;
        }
    }
}
