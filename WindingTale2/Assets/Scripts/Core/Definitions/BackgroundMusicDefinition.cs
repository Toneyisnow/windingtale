using Newtonsoft.Json;

namespace WindingTale.Core.Definitions
{
    /// <summary>
    /// The chapter's background music, one clip name per situation. Matches the
    /// "BackgroundMusic" object in the chapter JSON, e.g.
    ///   "BackgroundMusic": { "Field": "Battle_2_HD_Final_V3", "Enemy": "Battle_Enemy_1_HD_1_Final", "Village": "" }
    /// Each value is a clip name under Resources/Audios (no folder, no extension); an
    /// empty value means that situation has no music -- chapter 1 has no village, so its
    /// Village is left empty.
    /// </summary>
    public class BackgroundMusicDefinition
    {
        /// <summary>Plays through the player's turn on the battlefield.</summary>
        [JsonProperty(PropertyName = "Field")]
        public string Field { get; set; }

        /// <summary>Plays through the enemy's turn on the battlefield.</summary>
        [JsonProperty(PropertyName = "Enemy")]
        public string Enemy { get; set; }

        /// <summary>Plays in the village between this chapter and the next.</summary>
        [JsonProperty(PropertyName = "Village")]
        public string Village { get; set; }
    }
}
