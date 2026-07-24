using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using UnityEngine;
using WindingTale.Core.Definitions;
using WindingTale.Core.Objects;

namespace WindingTale.Core.Files
{

    public class GameRecordManager
    {
        /// <summary>
        /// How many save slots the player has, persisted as record-00.sav .. record-19.sav.
        /// GetAllFiles walks the whole range; SaveToFile / LoadFromFile are indexed into it.
        /// </summary>
        public const int RecordSlotCount = 20;

        // The between-chapters record (party + purse) is small and never sensitive, but the
        // save is still encrypted so a player cannot hand-edit the JSON to rewrite their
        // party. This is obfuscation, not security -- the key ships in the build -- so a
        // fixed AES key/IV is enough. Both are exactly the sizes AES-256 wants (32 / 16 bytes).
        private static readonly byte[] EncryptionKey = Encoding.UTF8.GetBytes("WindingTaleRecordKey_v1_32bytes!");
        private static readonly byte[] EncryptionIv = Encoding.UTF8.GetBytes("WT_RecordIv_16by");

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

            // The battle just won was the map's chapter; the party now belongs to the next
            // one. Advance the chapter here so the village -- and the save taken in it -- is
            // always the start of the chapter to come, never a replay of the one finished.
            record.ChapterId = mapRecord.ChapterId + 1;
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

        /// <summary>
        /// Reads back the record in slot <paramref name="recordIndex"/>, or null when that
        /// slot is empty. Pure file work, so a caller can read the record before deciding
        /// what to do with it (the load flow confirms it before entering the village).
        ///
        /// Stays quiet about an empty slot -- GetAllFiles probes every slot this way, where
        /// absence is the ordinary answer, not a fault.
        /// </summary>
        public static GameRecord LoadFromFile(int recordIndex)
        {
            string fullFilePath = GetSaveFilePath(recordIndex);

            if (!File.Exists(fullFilePath))
            {
                return null;
            }

            try
            {
                byte[] encrypted = File.ReadAllBytes(fullFilePath);
                string json = Decrypt(encrypted);
                return JsonConvert.DeserializeObject<GameRecord>(json);
            }
            catch (Exception e)
            {
                // A slot that will not decode -- truncated, hand-edited, or written by an
                // incompatible build -- is treated as empty rather than crashing the menu.
                Debug.LogWarning(string.Format(
                    "Cannot read game record slot {0}: {1}", recordIndex, e.Message));
                return null;
            }
        }

        /// <summary>
        /// Writes <paramref name="record"/> into slot <paramref name="recordIndex"/> as
        /// encrypted JSON, overwriting whatever was there. A null record is ignored rather
        /// than written as an empty save.
        /// </summary>
        public static void SaveToFile(int recordIndex, GameRecord record)
        {
            if (record == null)
            {
                Debug.LogWarning(string.Format("No record to save into slot {0}.", recordIndex));
                return;
            }

            string json = JsonConvert.SerializeObject(record);
            byte[] encrypted = Encrypt(json);

            File.WriteAllBytes(GetSaveFilePath(recordIndex), encrypted);

            Debug.Log(string.Format("Game record saved to slot {0}.", recordIndex));
        }

        /// <summary>
        /// Reads every slot that holds a save, keyed by its slot index. Empty slots are
        /// left out, so the count of the returned dictionary is how many saves exist and
        /// its keys are which slots they are in -- what the record menu lists.
        /// </summary>
        public static Dictionary<int, GameRecord> GetAllFiles()
        {
            Dictionary<int, GameRecord> records = new Dictionary<int, GameRecord>();

            for (int recordIndex = 0; recordIndex < RecordSlotCount; recordIndex++)
            {
                GameRecord record = LoadFromFile(recordIndex);
                if (record != null)
                {
                    records[recordIndex] = record;
                }
            }

            return records;
        }

        private static byte[] Encrypt(string plainText)
        {
            using (Aes aes = Aes.Create())
            {
                aes.Key = EncryptionKey;
                aes.IV = EncryptionIv;

                using (ICryptoTransform encryptor = aes.CreateEncryptor())
                {
                    byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
                    return encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);
                }
            }
        }

        private static string Decrypt(byte[] cipherBytes)
        {
            using (Aes aes = Aes.Create())
            {
                aes.Key = EncryptionKey;
                aes.IV = EncryptionIv;

                using (ICryptoTransform decryptor = aes.CreateDecryptor())
                {
                    byte[] plainBytes = decryptor.TransformFinalBlock(cipherBytes, 0, cipherBytes.Length);
                    return Encoding.UTF8.GetString(plainBytes);
                }
            }
        }

        /// <summary>
        /// Where slot <paramref name="recordIndex"/> lives on disk: record-00.sav ..
        /// record-19.sav. In the editor the saves sit next to the assets so they are easy
        /// to find; in a build they go to the platform's persistent data path. The same
        /// split GameMapRecordManager uses for the in-battle save.
        /// </summary>
        private static string GetSaveFilePath(int recordIndex)
        {
            string fileName = string.Format(@"record-{0:D2}.sav", recordIndex);

#if UNITY_EDITOR
            return Path.Combine(Application.dataPath, fileName);
#else
            return Path.Combine(Application.persistentDataPath, fileName);
#endif
        }
    }
}
