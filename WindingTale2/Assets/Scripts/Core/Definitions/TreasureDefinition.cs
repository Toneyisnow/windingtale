using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WindingTale.Core.Common;

namespace WindingTale.Core.Definitions
{

    public enum TreasureType
    {
        RedBox = 1,
        BlueBox = 2,
        Hidden = 3,
    }

    /// <summary>
    /// A treasure placed on the map, converted from the legacy Chapter-NN.dat trailing
    /// block (posX posY type itemId). Matches the "Treasures" array in the chapter JSON:
    ///   { "Id": 1, "Type": 1, "Position": { "X": 8, "Y": 14 }, "ItemId": 101 }
    /// Id is generated per chapter and is unique; Type is the box kind (see TreasureType).
    /// </summary>
    public class TreasureDefinition
    {
        [JsonProperty(PropertyName = "Id")]
        public int TreasureId
        {
            get; set;
        }

        [JsonProperty(PropertyName = "Type")]
        public TreasureType Type
        {
            get; set;
        }

        [JsonProperty(PropertyName = "Position")]
        public TreasurePosition Position
        {
            get; set;
        }

        [JsonProperty(PropertyName = "ItemId")]
        public int ItemId
        {
            get; set;
        }
    }

    /// <summary>
    /// Tile coordinates as they appear in the chapter JSON. A plain data holder for the
    /// same reason ObstaclePosition is one: FDPosition has no parameterless constructor
    /// and read-only X/Y, so it cannot be deserialized into directly.
    /// </summary>
    public class TreasurePosition
    {
        [JsonProperty(PropertyName = "X")]
        public int X { get; set; }

        [JsonProperty(PropertyName = "Y")]
        public int Y { get; set; }
    }
}
