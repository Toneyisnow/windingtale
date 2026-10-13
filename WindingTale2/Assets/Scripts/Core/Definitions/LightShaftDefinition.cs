using Newtonsoft.Json;

namespace WindingTale.Core.Definitions
{
    /// <summary>
    /// A column of light hanging from under the board into the void (chapter 24's three
    /// shafts). The 2D art painted them running down the map from under the island; the
    /// remaster cuts them out of the tiles and stands them up for real -- seen from the
    /// 45-degree field camera a column dropping straight down lands on screen exactly where
    /// the painted shaft ran south. Written by Tools/MapPipeline/void_tiles.py, e.g.
    ///   { "X": 14.5, "Y": 22.5, "Width": 1.5833, "EdgePixels": 6 }
    /// All in tile units, fractional: X is the shaft's centre, Y the line its front face
    /// hangs from (a little north of where the art first shows it, so the top is tucked
    /// under the cliff), Width its width; EdgePixels is how many original pixels its
    /// orange ramp takes on each side.
    /// </summary>
    public class LightShaftDefinition
    {
        [JsonProperty(PropertyName = "X")]
        public float X { get; set; }

        [JsonProperty(PropertyName = "Y")]
        public float Y { get; set; }

        [JsonProperty(PropertyName = "Width")]
        public float Width { get; set; }

        [JsonProperty(PropertyName = "EdgePixels")]
        public int EdgePixels { get; set; }
    }
}
