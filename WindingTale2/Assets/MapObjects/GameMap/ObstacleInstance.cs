using UnityEngine;
using WindingTale.Core.Common;

namespace WindingTale.MapObjects.GameMap
{
    /// <summary>
    /// Runtime data for one instantiated obstacle: which tiles of the board it
    /// covers, plus the fade used when the cursor or a menu item lands on one of
    /// those tiles (the same treatment creatures get, see Creature.SetTransparency).
    /// The fade itself lives in MapObjectFade, shared with TreasureInstance.
    ///
    /// The footprint is the rectangle
    ///   [TileX, TileX + Width - 1] x [TileY, TileY + Height - 1]
    /// with (TileX, TileY) being the obstacle's Position from the chapter JSON,
    /// i.e. its top-left tile; the model extends into the map from there.
    /// </summary>
    public class ObstacleInstance : MapObjectFade
    {
        public int TileX { get; private set; }
        public int TileY { get; private set; }
        public int Width { get; private set; }
        public int Height { get; private set; }

        public void SetFootprint(int tileX, int tileY, int width, int height)
        {
            this.TileX = tileX;
            this.TileY = tileY;
            this.Width = Mathf.Max(1, width);
            this.Height = Mathf.Max(1, height);
        }

        /// <summary>
        /// True when the given tile falls inside this obstacle's footprint.
        /// </summary>
        public bool Covers(FDPosition position)
        {
            if (position == null)
            {
                return false;
            }

            return position.X >= TileX && position.X <= TileX + Width - 1
                && position.Y >= TileY && position.Y <= TileY + Height - 1;
        }
    }
}
