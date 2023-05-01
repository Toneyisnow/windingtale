using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WindingTale.Common;

namespace WindingTale.Core.Definitions
{

    public enum TreasureType
    {
        RedBox = 1,
        BlueBox = 2,
        Hidden = 3,
    }

    public class TreasureDefinition
    {
        public int TreasureId
        {
            get; set;
        }

        public TreasureType Type
        {
            get; set;
        }

        public FDPosition Position
        {
            get; set;
        }

    }
}