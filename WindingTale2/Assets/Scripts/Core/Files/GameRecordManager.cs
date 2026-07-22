using System.Collections.Generic;
using System.Linq;
using WindingTale.Core.Definitions;
using WindingTale.Core.Objects;

namespace WindingTale.Core.Files
{

    public class GameRecordManager
    {
        /// <summary>
        /// Boils a finished battle down to what the player takes with them, and applies
        /// the rest between chapters:
        ///
        ///   - everyone still standing is back to full HP and MP;
        ///   - everyone who fell rejoins the party at 0 HP, to be revived in the village;
        ///   - nobody is still poisoned, frozen, forbidden or buffed -- effects belong to
        ///     the battle they were cast in.
        ///
        /// Enemies and NPCs are dropped: they belong to the chapter, not to the player.
        /// Positions are carried across untouched but mean nothing off the battlefield --
        /// the next chapter places the party itself.
        /// </summary>
        public static GameRecord CreateFromMapRecord(GameMapRecord mapRecord)
        {
            GameRecord record = new GameRecord();
            record.Friends = new List<CreatureMapRecord>();

            if (mapRecord == null)
            {
                return record;
            }

            record.ChapterId = mapRecord.ChapterId;
            record.TotalMoney = mapRecord.TotalMoney;

            foreach (CreatureMapRecord survivor in SelectFriends(mapRecord.Creatures))
            {
                // The map only ever holds the living -- the dying are moved off it -- but
                // the rule is written out anyway, so a record that somehow carries a
                // fallen creature in the wrong list is not healed back to life by it.
                if (survivor.Hp <= 0)
                {
                    continue;
                }

                CreatureMapRecord healed = survivor.Clone();
                healed.Hp = healed.HpMax;
                healed.Mp = healed.MpMax;
                healed.Effects = new List<CreatureEffects>();

                record.Friends.Add(healed);
            }

            foreach (CreatureMapRecord fallen in SelectFriends(mapRecord.DeadCreatures))
            {
                CreatureMapRecord revived = fallen.Clone();
                revived.Hp = 0;
                revived.Effects = new List<CreatureEffects>();

                record.Friends.Add(revived);
            }

            return record;
        }

        private static IEnumerable<CreatureMapRecord> SelectFriends(List<CreatureMapRecord> creatures)
        {
            return creatures == null
                ? Enumerable.Empty<CreatureMapRecord>()
                : creatures.Where(creature => creature.Faction == CreatureFaction.Friend);
        }

        public GameRecord LoadFromFile(string recordName)
        {
            return null;
        }

        public void SaveToFile(string recordName, GameRecord record)
        {
            return;
        }
    }
}
