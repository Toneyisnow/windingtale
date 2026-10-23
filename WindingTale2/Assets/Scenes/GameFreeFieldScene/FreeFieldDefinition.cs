using System.Collections.Generic;
using Newtonsoft.Json;
using WindingTale.Core.Common;
using WindingTale.Core.Definitions;

namespace WindingTale.Scenes.GameFreeFieldScene
{
    /// <summary>
    /// Which way a creature on a free field looks. The rotations are CreatureWalk's: a
    /// creature model faces the camera (Down) unturned.
    /// </summary>
    public enum FreeFieldFacing
    {
        Down = 0,
        Up = 1,
        Left = 2,
        Right = 3,
    }

    /// <summary>
    /// A free-field map, Resources/Data/Maps/Map_NN.json: the field itself as a chapter
    /// carries it (ShapeMatrix decides where the hero may walk, RenderMatrix what is drawn,
    /// ShapeSet the folder of tile models, Obstacles the props standing on it), plus who
    /// stands where and what the hero can talk to. Written by
    /// Tools/MapPipeline/build_free_map.py.
    /// </summary>
    public class FreeFieldDefinition : ChapterDefinition
    {
        [JsonProperty(PropertyName = "Hero")]
        public FreeFieldHeroDefinition Hero { get; set; }

        [JsonProperty(PropertyName = "Creatures")]
        public List<FreeFieldCreatureDefinition> Creatures { get; set; }

        [JsonProperty(PropertyName = "Events")]
        public List<FreeFieldEventDefinition> Events { get; set; }

        /// <summary>
        /// The ChapterStrings-NN table (and FZB_Chapter-NN font) the map's talk is in; 0 for
        /// the map's own. Map 34 talks out of Map 33's table.
        /// </summary>
        [JsonProperty(PropertyName = "StringsTable")]
        public int StringsTable { get; set; }

        /// <summary>
        /// Optional: a hint shown in the top-left corner until the player first presses an
        /// arrow key (FreeFieldHintBar) -- Map 32's "按方向键操作角色".
        /// </summary>
        [JsonProperty(PropertyName = "Hint")]
        public string Hint { get; set; }

        /// <summary>
        /// Optional: the background music, a clip in Resources/Audios/Musics (Maps 33 and 34
        /// play "Shop_Church_HD_Final"); a map without one leaves the music alone.
        /// </summary>
        [JsonProperty(PropertyName = "Music")]
        public string Music { get; set; }
    }

    /// <summary>The creature the player walks about, and where it starts.</summary>
    public class FreeFieldHeroDefinition
    {
        [JsonProperty(PropertyName = "DefinitionId")]
        public int DefinitionId { get; set; }

        [JsonProperty(PropertyName = "X")]
        public int X { get; set; }

        [JsonProperty(PropertyName = "Y")]
        public int Y { get; set; }

        [JsonProperty(PropertyName = "Facing")]
        public string Facing { get; set; }
    }

    /// <summary>
    /// A creature that stands on the field: a real creature definition (DefinitionId, as
    /// the party members are), or just a look (AnimationId's icon) for the extras.
    /// </summary>
    public class FreeFieldCreatureDefinition
    {
        [JsonProperty(PropertyName = "Id")]
        public int Id { get; set; }

        [JsonProperty(PropertyName = "DefinitionId")]
        public int DefinitionId { get; set; }

        /// <summary>"Lying": on her side, face to the camera, head to the left (see FreeFieldPose).</summary>
        [JsonProperty(PropertyName = "Pose")]
        public string Pose { get; set; }

        [JsonProperty(PropertyName = "AnimationId")]
        public int AnimationId { get; set; }

        [JsonProperty(PropertyName = "X")]
        public int X { get; set; }

        [JsonProperty(PropertyName = "Y")]
        public int Y { get; set; }

        [JsonProperty(PropertyName = "Facing")]
        public string Facing { get; set; }
    }

    /// <summary>How an event starts.</summary>
    public enum FreeFieldTrigger
    {
        /// <summary>The hero comes to face one of the Positions (the default).</summary>
        Face,

        /// <summary>The hero arrives on one of the Positions.</summary>
        Step,

        /// <summary>The hero arrives on a tile next to one of the Positions ("walks up to" it).</summary>
        Near,
    }

    /// <summary>
    /// A talk on the map: when its Trigger fires on one of Positions, its Talks play one
    /// after another, and once they are over its Then runs. A Once event plays the first
    /// time only; one with Requires waits until those events (by Id) have played.
    /// </summary>
    public class FreeFieldEventDefinition
    {
        [JsonProperty(PropertyName = "Id")]
        public int Id { get; set; }

        [JsonProperty(PropertyName = "Trigger")]
        public string Trigger { get; set; }

        [JsonProperty(PropertyName = "Once")]
        public bool Once { get; set; }

        [JsonProperty(PropertyName = "Requires")]
        public List<int> Requires { get; set; }

        [JsonProperty(PropertyName = "Then")]
        public FreeFieldThenDefinition Then { get; set; }

        [JsonProperty(PropertyName = "Actions")]
        public List<FreeFieldActionDefinition> Actions { get; set; }

        [JsonProperty(PropertyName = "Positions")]
        public List<ObstaclePosition> Positions { get; set; }

        public FreeFieldTrigger TriggerKind
        {
            get
            {
                return !string.IsNullOrEmpty(Trigger) && System.Enum.TryParse(Trigger, true, out FreeFieldTrigger kind)
                    ? kind
                    : FreeFieldTrigger.Face;
            }
        }

        /// <summary>Whether the hero, standing on <paramref name="position"/>, is next to one of the Positions.</summary>
        public bool IsNextTo(FDPosition position)
        {
            if (Positions == null || position == null)
            {
                return false;
            }

            return Positions.Exists(p => System.Math.Abs(p.X - position.X) + System.Math.Abs(p.Y - position.Y) == 1);
        }

        [JsonProperty(PropertyName = "Talks")]
        public List<FreeFieldTalkDefinition> Talks { get; set; }

        public bool Covers(FDPosition position)
        {
            if (Positions == null || position == null)
            {
                return false;
            }

            return Positions.Exists(p => p.X == position.X && p.Y == position.Y);
        }
    }

    /// <summary>
    /// One conversation: the lines Conversation-NN-CC-From .. -To of ChapterStrings-NN,
    /// the i-th spoken with Speakers[i]'s portrait (an animation id; 0 is the narrator).
    /// </summary>
    public class FreeFieldTalkDefinition
    {
        [JsonProperty(PropertyName = "ConversationId")]
        public int ConversationId { get; set; }

        [JsonProperty(PropertyName = "From")]
        public int From { get; set; }

        [JsonProperty(PropertyName = "To")]
        public int To { get; set; }

        [JsonProperty(PropertyName = "Speakers")]
        public List<int> Speakers { get; set; }
    }

    /// <summary>
    /// What happens once an event's talk is over: Map moves on to that free-field map;
    /// Chapter starts that battle chapter in GameFieldScene; Spawn puts creatures on this one.
    /// </summary>
    public class FreeFieldThenDefinition
    {
        [JsonProperty(PropertyName = "Map")]
        public int Map { get; set; }

        [JsonProperty(PropertyName = "Chapter")]
        public int Chapter { get; set; }

        [JsonProperty(PropertyName = "Spawn")]
        public List<FreeFieldCreatureDefinition> Spawn { get; set; }
    }

    /// <summary>
    /// Something that happens in the middle of an event's talk, After or Before one of its
    /// lines ([conversation, sequence]): Walk moves a creature (by Id) to a tile, and the
    /// talk waits until it is there; Stand puts a creature that is lying down (Spawn Pose
    /// "Lying") back on its feet, facing front; StopMusic fades the background music out.
    /// </summary>
    public class FreeFieldActionDefinition
    {
        [JsonProperty(PropertyName = "After")]
        public List<int> After { get; set; }

        [JsonProperty(PropertyName = "Before")]
        public List<int> Before { get; set; }

        [JsonProperty(PropertyName = "Walk")]
        public FreeFieldWalkDefinition Walk { get; set; }

        [JsonProperty(PropertyName = "Stand")]
        public int Stand { get; set; }

        [JsonProperty(PropertyName = "StopMusic")]
        public bool StopMusic { get; set; }

        public bool IsAfter(int conversationId, int sequenceId)
        {
            return Matches(After, conversationId, sequenceId);
        }

        public bool IsBefore(int conversationId, int sequenceId)
        {
            return Matches(Before, conversationId, sequenceId);
        }

        private static bool Matches(List<int> line, int conversationId, int sequenceId)
        {
            return line != null && line.Count == 2 && line[0] == conversationId && line[1] == sequenceId;
        }
    }

    /// <summary>A creature (by Id) walking to the tile X, Y.</summary>
    public class FreeFieldWalkDefinition
    {
        [JsonProperty(PropertyName = "Creature")]
        public int Creature { get; set; }

        [JsonProperty(PropertyName = "X")]
        public int X { get; set; }

        [JsonProperty(PropertyName = "Y")]
        public int Y { get; set; }
    }
}
