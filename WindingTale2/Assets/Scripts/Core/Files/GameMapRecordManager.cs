
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using WindingTale.Core.Map;
using WindingTale.Core.Objects;

namespace WindingTale.Core.Files
{
    /// <summary>
    /// Saved in the Battle Field so that could load from Continue button
    /// </summary>
    public class GameMapRecordManager
    {

        public FDMap LoadFromFile(string recordName)
        {
            string saveKey = generateKey(recordName);
            if (PlayerPrefs.HasKey(saveKey))
            {
                string json = PlayerPrefs.GetString(saveKey);
                GameMapRecord record = JsonConvert.DeserializeObject<GameMapRecord>(json);
                Debug.Log("Game Loaded: " + json);

                FDMap map = FDMap.LoadFromMapRecord(record.ChapterId,
                    record.Creatures.Select(creature => ConvertRecordToCreature(creature)).ToList(),
                    record.DeadCreatures.Select(creature => ConvertRecordToCreature(creature)).ToList(),
                    record.Treasures.Select(treasure => ConvertRecordToTreasure(treasure)).ToList(),
                    record.TriggeredEvents);

                return map;
            }
            else
            {
                Debug.LogWarning("No save data found. Returning new default data.");
            }
            return null;
        }

        public void SaveToFile(string recordName, FDMap map)
        {
            GameMapRecord gameMapRecord = new GameMapRecord();

            gameMapRecord.ChapterId = map.ChapterId;
            gameMapRecord.TurnNo = map.TurnNo;
            gameMapRecord.Creatures = map.Creatures.Select(creature => ConvertCreatureToRecord(creature) ).ToList();
            gameMapRecord.DeadCreatures = map.DeadCreatures.Select(creature => ConvertCreatureToRecord(creature)).ToList();
            gameMapRecord.Treasures = map.Treasures.Select(treasure => ConvertTreasureToRecord(treasure)).ToList();
            gameMapRecord.TriggeredEvents = map.TriggeredEvents;

            string json = JsonConvert.SerializeObject(gameMapRecord);
            PlayerPrefs.SetString(generateKey(recordName), json);
            PlayerPrefs.Save();
            Debug.Log("Game Saved: " + json);
        }

        private string generateKey(string recordName)
        {
            return "GameMapRecord_" + recordName;
        }


        private CreatureMapRecord ConvertCreatureToRecord(FDCreature creature)
        {
            CreatureMapRecord record = new CreatureMapRecord();
            record.Id = creature.Id;
            record.Faction = creature.Faction;
            record.Level = creature.Level;
            record.Hp = creature.Hp;
            record.Mp = creature.Mp;
            record.Ap = creature.Ap;
            record.Dp = creature.Dp;
            record.Dx = creature.Dx;
            record.ItemIds = creature.Items;
            record.MagicIds = creature.Magics;
            record.Position = creature.Position;
            return record;
        }

        private FDCreature ConvertRecordToCreature(CreatureMapRecord record)
        {
            FDCreature creature = new FDCreature(record.Id, record.Faction);
            creature.Level = record.Level;
            creature.Hp = record.Hp;
            creature.Mp = record.Mp;
            creature.Ap = record.Ap;
            creature.Dp = record.Dp;
            creature.Dx = record.Dx;
            creature.Items = record.ItemIds;
            creature.Magics = record.MagicIds;
            creature.Position = record.Position;
            return creature;
        }   


        private TreasureMapRecord ConvertTreasureToRecord(FDTreasure treasure)
        {
            TreasureMapRecord record = new TreasureMapRecord();
            record.ItemId = treasure.ItemId;
            record.Money = treasure.Money;
            record.Position = treasure.Position;
            return record;
        }

        private FDTreasure ConvertRecordToTreasure(TreasureMapRecord record)
        {
            FDTreasure treasure = new FDTreasure(record.ItemId, record.Money);
            treasure.Position = record.Position;
            return treasure;
        }
    }
}