using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WindingTale.Common;

namespace WindingTale.Core.Definitions
{
    public enum ShapeType
    {
        Plain = 0,
        Blocked = 1,
        Forest = 2,
        BlackForest = 3,
        Marsh = 4,
        Gap = 5,
    }

    public class ShapeDefinition
    {
        [JsonProperty(PropertyName = "Type")]
        public ShapeType Type
        {
            get; set;
        }

        [JsonProperty(PropertyName = "bg")]
        public int BattleGroundImage
        {
            get; set;
        }
    }

    public enum TreasureType
    {
        RedBox = 1,
        BlueBox = 2,
        Hiddne = 3,
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

    /// <summary>
    /// 
    /// </summary>
    public class ChapterDefinition
    {
        [JsonProperty(PropertyName= "Index")]
        public int ChapterId
        {
            get; set;
        }

        [JsonProperty(PropertyName = "Width")]
        public int Width
        {
            get; set;
        }

        [JsonProperty(PropertyName = "Height")]
        public int Height
        {
            get; set;
        }

        [JsonProperty(PropertyName = "ShapeMatrix")]
        public int[,] Map
        {
            get; set;
        }

        [JsonProperty(PropertyName = "Shapes")]
        public Dictionary<int, ShapeDefinition> ShapeDict
        {
            get; set;
        }

        public List<TreasureDefinition> Treasures
        {
            get; set;
        }

        public List<CreatureDefinition> CreatureDefinitions
        {
            get; set;
        }

    }
}