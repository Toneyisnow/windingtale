using System.Collections.Generic;

namespace WindingTale.Core.Files
{

    /// <summary>
    /// What the player carries out of a won battle and into the next chapter: the party
    /// and the purse, nothing else.
    ///
    /// Deliberately far smaller than GameMapRecord. Everything that made a battlefield a
    /// battlefield -- positions, the turn counter, chests, enemies, which events fired --
    /// dies with the chapter it belonged to. See GameRecordManager.CreateFromMapRecord
    /// for the conversion, which is also where the between-chapters healing happens.
    /// </summary>
    public class GameRecord
    {
        /// <summary>The chapter that was just won.</summary>
        public int ChapterId
        {
            get; set;
        }

        /// <summary>
        /// The whole party, the fallen included -- they come back at 0 HP, to be revived
        /// (or not) in the village.
        /// </summary>
        public List<CreatureMapRecord> Friends { get; set; }

        public int TotalMoney { get; set; }

    }
}
