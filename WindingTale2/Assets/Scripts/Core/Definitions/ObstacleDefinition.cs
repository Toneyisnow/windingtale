using Newtonsoft.Json;

namespace WindingTale.Core.Definitions
{
    /// <summary>
    /// An obstacle placed on the map: a 3D model (DefinitionKey) standing at a
    /// tile Position. Matches the "Obstacles" array in the chapter JSON, e.g.
    ///   { "Id": 1, "DefinitionKey": "dwelling_house_1", "Position": { "X": 10, "Y": 10 } }
    /// The model is loaded from Resources/Obstacles/{DefinitionKey}.obj
    /// </summary>
    public class ObstacleDefinition
    {
        [JsonProperty(PropertyName = "Id")]
        public int Id { get; set; }

        [JsonProperty(PropertyName = "DefinitionKey")]
        public string DefinitionKey { get; set; }

        [JsonProperty(PropertyName = "Position")]
        public ObstaclePosition Position { get; set; }

        /// <summary>
        /// Optional. Obstacles sharing a Group are the pieces of one building, cut
        /// apart only because a .vox is at most 10 tiles wide (chapter 05's cathedral
        /// and church). ObstaclesLayer shrinks them about the building's centre rather
        /// than each about its own, so no gap opens at the seam.
        /// </summary>
        [JsonProperty(PropertyName = "Group")]
        public string Group { get; set; }
    }

    public class ObstaclePosition
    {
        [JsonProperty(PropertyName = "X")]
        public int X { get; set; }

        [JsonProperty(PropertyName = "Y")]
        public int Y { get; set; }
    }
}
