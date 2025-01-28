using System.Collections.Generic;
using Newtonsoft.Json;
using WindingTale.Core.Common;
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

        public Dictionary<string, int> ConversationCreatureIds
        {
            get; private set;
        }

        public void ReadConversationIdsFromFile(ResourceDataFile dataFile)
        {
            this.ConversationCreatureIds = new Dictionary<string, int>();
            string key;
            while ((key = dataFile.ReadString()) != string.Empty)
            {
                int value = dataFile.ReadInt();
                this.ConversationCreatureIds[key] = value;
            }
        }

        /// <summary>
        /// Returns animation Id for the conversation, if return 0 it means the narrator
        /// </summary>
        /// <param name="conversation"></param>
        /// <returns></returns>
        public int GetConversationCreatureId(Conversation conversation)
        {
            string key = string.Format(@"Chapter_{0}-{0}-{1}-{2}-Id",
                StringUtils.Digit2(this.ChapterId),
                StringUtils.Digit2(conversation.ConversationId),
                StringUtils.Digit3(conversation.SequenceId));

            if (this.ConversationCreatureIds.ContainsKey(key))
            {
                return this.ConversationCreatureIds[key];
            }
            return 0;
        }

    }
}