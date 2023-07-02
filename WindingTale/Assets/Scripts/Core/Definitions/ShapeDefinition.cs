using Newtonsoft.Json;
using UnityEngine;

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
        public int Id
        {
            get; set;
        }

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
                switch (this.Type)
                {
                    case ShapeType.Plain: return 1;
                    case ShapeType.Blocked: return -1;
                    case ShapeType.Forest: return 1;
                    case ShapeType.BlackForest: return 1;
                    case ShapeType.Marsh: return 3;
                    case ShapeType.Gap: return -1;
                    default: return -1;
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

}