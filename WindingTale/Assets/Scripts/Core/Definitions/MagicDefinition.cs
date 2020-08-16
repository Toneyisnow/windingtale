using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WindingTale.Common;

namespace WindingTale.Core.Definitions
{
    /// <summary>
    /// The type of the mgaic
    /// </summary>
    public enum MagicType
    {
        Offensive,
        Defensive,
        Transmit,
    }

    public class MagicDefinition
    {
        public int MagicId
        {
            get; set;
        }

        public int MpCost
        {
            get; set;
        }

        public MagicType Type
        {
            get; set;
        }

        public MagicDefinition()
        {

        }

        public static MagicDefinition ReadFromFile(ResourceDataFile dataFile)
        {
            return null;
        }



    }
}