
using System.Collections.Generic;
using WindingTale.Core.Common;
using WindingTale.Core.Definitions;
using WindingTale.Core.Map;
using WindingTale.Core.Objects;

namespace WindingTale.Core.Files
{
    /// <summary>
    /// Saved in the Battle Field so that could load from Continue button
    /// </summary>
    public class GameMapRecord
    {
        public int ChapterId;

        public int TurnNo;

        public List<CreatureMapRecord> Creatures;

        public List<CreatureMapRecord> DeadCreatures;

        /// <summary>
        /// This might be different than the definition of the map, because someone might opened it, or 
        /// exchanged some items.
        /// </summary>
        public List<TreasureMapRecord> Treasures;


        /// <summary>
        /// Save a list of the events that were triggered during the battle.
        /// </summary>
        public List<int> TriggeredEvents;




    }

    public class CreatureMapRecord
    {
        public int Id;
        public CreatureFaction Faction;
        public string Name;
        public int Level;
        public int Hp;
        public int Mp;
        public int Ap;
        public int Dp;
        public int Dx;
        public List<int> ItemIds;
        public List<int> MagicIds;
        public FDPosition Position;
    }

    public class TreasureMapRecord
    {
        public int ItemId;
        public int Money;
        public FDPosition Position;
    }
}