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

        public int MoveCost
        {
            get
            {
                switch(this.Type)
                {
                    case ShapeType.Plain: return 1;
                    case ShapeType.Blocked: return -1;
                    case ShapeType.Forest: return 1;
                    case ShapeType.BlackForest: return 1;
                    case ShapeType.Marsh: return 3;
                    case ShapeType.Gap: return -1;
                    default:return -1;
                }
            }
        }

        public int MoveCostForKnight
        {
            get
            {
                switch (this.Type)
                {
                    case ShapeType.Plain: return 1;
                    case ShapeType.Blocked: return -1;
                    case ShapeType.Forest: return 2;
                    case ShapeType.BlackForest: return 2;
                    case ShapeType.Marsh: return 2;
                    case ShapeType.Gap: return -1;
                    default: return -1;
                }
            }
        }
        public bool CanFly
        {
            get
            {
                switch (this.Type)
                {
                    case ShapeType.Plain: return true;
                    case ShapeType.Blocked: return true;
                    case ShapeType.Forest: return true;
                    case ShapeType.BlackForest: return true;
                    case ShapeType.Marsh: return true;
                    case ShapeType.Gap: return false;
                    default: return true;
                }
            }
        }

        public int AdjustedAp
        {
            get
            {
                switch (this.Type)
                {
                    case ShapeType.Plain: return 5;
                    case ShapeType.Blocked: return 0;
                    case ShapeType.Forest: return -5;
                    case ShapeType.BlackForest: return -5;
                    case ShapeType.Marsh: return -5;
                    case ShapeType.Gap: return -5;
                    default: return 0;
                }
            }
        }

        public int AdjustedDp
        {
            get
            {
                switch (this.Type)
                {
                    case ShapeType.Plain: return 0;
                    case ShapeType.Blocked: return 0;
                    case ShapeType.Forest: return 10;
                    case ShapeType.BlackForest: return 10;
                    case ShapeType.Marsh: return -5;
                    case ShapeType.Gap: return 0;
                    default: return 0;
                }
            }
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

        public Dictionary<string, int> ConversationIds
        {
            get; private set;
        }

        public virtual void LoadEvents()
        {

        }

        public void ReadConversationIdsFromFile(ResourceDataFile dataFile)
        {
            this.ConversationIds = new Dictionary<string, int>();
            string key;
            while ((key = dataFile.ReadString()) != string.Empty)
            {
                int value = dataFile.ReadInt();
                this.ConversationIds[key] = value;
            }
        }

    }
}