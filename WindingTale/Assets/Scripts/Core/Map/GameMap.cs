using WindingTale.Core.Common;
using WindingTale.Core.Definitions;
using WindingTale.Core.Events;
using WindingTale.Core.Objects;
using WindingTile.Core.Files;

namespace WindingTale.Core.Map
{
    /// <summary>
    /// This is the main function for Battle Map in the game.
    /// </summary>
    public class GameMap
    {
        public GameMap()
        { }

        public GameField Field { get; private set; }

        private FDObject[] objects = null;

        private FDEvent events = null;

        public int TurnNo { get; private set; }

        public TurnType TurnType { get; private set; }



        public static GameMap LoadFromChapter(ChapterDefinition chapter, GameRecord record)
        {
            return null;
        }

        public static GameMap LoadFromSave(GameMapRecord mapRecord)
        {
            return null;
        }

    }
}