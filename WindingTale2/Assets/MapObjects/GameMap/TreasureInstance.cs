using UnityEngine;
using WindingTale.Core.Common;
using WindingTale.Core.Definitions;
using WindingTale.Core.Objects;

namespace WindingTale.MapObjects.GameMap
{
    /// <summary>
    /// One treasure chest on the board. Unlike an obstacle a chest always occupies
    /// exactly one tile, so there is no footprint rectangle -- just the tile it sits
    /// on. Fading (cursor / menu / a creature standing on it) comes from MapObjectFade.
    ///
    /// The chest model lives on a child object rather than on this one, because the
    /// closed and opened models are different meshes: swapping state destroys the
    /// child and instantiates the other model, while this component (and its cached
    /// fade state) survives.
    /// </summary>
    public class TreasureInstance : MapObjectFade
    {
        public int TreasureId { get; private set; }
        public int TileX { get; private set; }
        public int TileY { get; private set; }

        /// <summary>
        /// The chest kind this object was built from. Held here rather than re-read
        /// from the map on every remount: a map restored from an old save may have
        /// fallen back to a default type (see GameMapRecordManager), and a chest must
        /// not change colour halfway through a battle just because it was opened.
        /// </summary>
        public TreasureType Type { get; private set; }

        /// <summary>Which model is currently instantiated under this object.</summary>
        public bool ShowingOpened { get; private set; }

        public void SetTreasure(FDTreasure treasure)
        {
            this.TreasureId = treasure.Id;
            this.TileX = treasure.Position.X;
            this.TileY = treasure.Position.Y;
            this.Type = treasure.Type;
        }

        public bool Covers(FDPosition position)
        {
            return position != null && position.X == TileX && position.Y == TileY;
        }

        /// <summary>
        /// Records which model is now mounted. The caller does the instantiate/destroy;
        /// this clears the cached fade state, because the new model's materials come in
        /// opaque regardless of what the old ones were showing.
        /// </summary>
        public void SetShowingOpened(bool opened)
        {
            this.ShowingOpened = opened;
            ForgetFadeState();
        }
    }
}
