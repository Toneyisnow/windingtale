using System.Collections.Generic;
using Newtonsoft.Json;
using WindingTale.Core.Files;

namespace WindingTale.Core.Definitions
{
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