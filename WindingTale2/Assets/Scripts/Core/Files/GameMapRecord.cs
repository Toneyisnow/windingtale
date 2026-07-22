
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
        /// Which side's phase the save was taken in. Saves written before this field
        /// existed decode it as 0 (Friend), which is what they all were: the record menu
        /// is only reachable during the player's phase.
        /// </summary>
        public CreatureFaction TurnType;

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

        // Note: HasActioned is deliberately NOT recorded. The game only lets a battle be
        // saved at the start of a turn, before anyone has moved, so every creature in a
        // record is by construction still to act -- and a load lands at the start of that
        // same turn, where onPlayerTurn resets the whole board anyway. Recording it would
        // be storing a value that can only ever be false and is overwritten on the way in.

        public FDPosition Position;

        // Only for AI Creature
        public AITypes AIType;

        /// <summary>
        /// A copy that shares nothing mutable with this one. The lists a record is built
        /// from are the live creature's own (see GameMapRecordManager.ConvertCreatureToRecord),
        /// so anything that edits a record -- the healing in
        /// GameRecordManager.CreateFromMapRecord above all -- has to work on a copy or it
        /// would reach back into the creature it was taken from.
        /// </summary>
        public CreatureMapRecord Clone()
        {
            CreatureMapRecord copy = (CreatureMapRecord)this.MemberwiseClone();

            copy.ItemIds = this.ItemIds == null ? null : new List<int>(this.ItemIds);
            copy.MagicIds = this.MagicIds == null ? null : new List<int>(this.MagicIds);
            copy.Effects = this.Effects == null ? null : new List<CreatureEffects>(this.Effects);

            return copy;
        }
    }

    public class TreasureMapRecord
    {
        public int Id;
        public int ItemId;
        public bool HasOpened;

        /// <summary>
        /// Which chest model to draw. Saves written before this field existed
        /// deserialize it as 0, which is not a valid TreasureType; see
        /// GameMapRecordManager.ConvertRecordToTreasure for the fallback.
        /// </summary>
        public TreasureType Type;

        public FDPosition Position;
    }
}