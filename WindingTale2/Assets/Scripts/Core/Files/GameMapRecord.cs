
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

        /// <summary>
        /// The player's purse at the moment of the save.
        /// </summary>
        public int TotalMoney;

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
        public int DefinitionId;

        public CreatureFaction Faction;
        public int Level;
        public int Hp;
        public int Mp;

        // Maxima and Mv/Exp grow with level-ups, so they cannot be recovered from the
        // creature definition the way the initial values can -- they must be recorded.
        public int HpMax;
        public int MpMax;
        public int Ap;
        public int Dp;
        public int Dx;
        public int Mv;
        public int Exp;

        public List<int> ItemIds;
        public List<int> MagicIds;

        // Equipment is an index into ItemIds, and item exchange reorders that list, so the
        // definition's own equip order is not a valid substitute after the battle starts.
        public int AttackItemIndex;
        public int DefendItemIndex;

        // Poisoned / frozen / stat buffs currently on the creature.
        public List<CreatureEffects> Effects;

        public FDPosition Position;

        // Only for AI Creature
        public AITypes AIType;
    }

    public class TreasureMapRecord
    {
        public int Id;
        public int ItemId;
        public bool HasOpened;
        public FDPosition Position;
    }
}