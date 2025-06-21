
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using WindingTale.Chapters;
using WindingTale.Core.Definitions;
using WindingTale.Core.Events;
using WindingTale.Core.Map;
using WindingTale.Core.Objects;
using WindingTale.MapObjects.CreatureIcon;
using WindingTale.MapObjects.GameMap;
using WindingTale.Scenes.GameFieldScene;

namespace WindingTale.Core.Files
{
    /// <summary>
    /// Saved in the Battle Field so that could load from Continue button
    /// </summary>
    public class GameMapRecordManager
    {

        public void LoadFromFile(string recordName, GameMain gameMain)
        {
            string fullFilePth = GetSaveFilePath(recordName);

            if (!File.Exists(fullFilePth))
            {
                Debug.LogWarning("No save file found.");
                return;
            }

            string json = File.ReadAllText(fullFilePth);
            GameMapRecord record = JsonConvert.DeserializeObject<GameMapRecord>(json);
            Debug.Log("Game Loaded: " + json);

            gameMain.gameMap.Initialize(record.ChapterId);
            List<FDEvent> chapterEvents = ChapterLoader.LoadEvents(gameMain, record.ChapterId);
            gameMain.eventHandler = new EventHandler(chapterEvents, gameMain);
            foreach (int eventId in record.TriggeredEvents)
            {
                FDEvent evt = chapterEvents.Find(e => e.EventId == eventId);
                if (evt != null)
                {
                    evt.SetActive(false); // Mark the event as inactive
                }
            }

            FDMap map = gameMain.gameMap.Map;
            map.TurnNo = record.TurnNo;

            foreach(CreatureMapRecord creatureRecord in record.Creatures)
            {
                FDCreature creature = ConvertRecordToCreature(creatureRecord);
                gameMain.gameMap.AddCreature(creature, creature.Position);
            }
            map.DeadCreatures = record.DeadCreatures.Select(creature => ConvertRecordToCreature(creature)).ToList();
            map.Treasures = record.Treasures.Select(treasure => ConvertRecordToTreasure(treasure)).ToList();

            
        }

        public void SaveToFile(string recordName, GameMain gameMain)
        {
            GameMapRecord gameMapRecord = new GameMapRecord();

            FDMap map = gameMain.gameMap.Map;
            List<int> triggeredEvents = gameMain.eventHandler.events.FindAll(evt => !evt.IsActive).Select( evt => evt.EventId).ToList();

            gameMapRecord.ChapterId = map.ChapterId;
            gameMapRecord.TurnNo = map.TurnNo;
            gameMapRecord.Creatures = map.Creatures.Select(creature => ConvertCreatureToRecord(creature) ).ToList();
            gameMapRecord.DeadCreatures = map.DeadCreatures.Select(creature => ConvertCreatureToRecord(creature)).ToList();
            gameMapRecord.Treasures = map.Treasures.Select(treasure => ConvertTreasureToRecord(treasure)).ToList();
            gameMapRecord.TriggeredEvents = triggeredEvents;

            string json = JsonConvert.SerializeObject(gameMapRecord);

            string fullFilePath = GetSaveFilePath(recordName);
            File.WriteAllText(fullFilePath, json);

            Debug.Log("Game Saved: " + json);
        }

        private CreatureMapRecord ConvertCreatureToRecord(FDCreature creature)
        {
            CreatureMapRecord record = new CreatureMapRecord();
            record.Id = creature.Id;
            record.DefinitionId = creature.Definition.DefinitionId;

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

            if (creature is FDAICreature aiCreature)
            {
                record.AIType = aiCreature.AIType;
            }

            return record;
        }

        private FDCreature ConvertRecordToCreature(CreatureMapRecord record)
        {
            CreatureDefinition definition = DefinitionStore.Instance.GetCreatureDefinition(record.DefinitionId);
            FDCreature creature = record.Faction == CreatureFaction.Friend ?
                new FDCreature(record.Id, definition, record.Faction) :
                new FDAICreature(record.Id, definition, record.Faction, record.AIType);

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

        private static string GetSaveFilePath(string recordName)
        {
            string fileName = string.Format(@"GameMapRecord_{0}.sav", recordName);

#if UNITY_EDITOR
            return Path.Combine(Application.dataPath, fileName);
#else
            return Path.Combine(Application.persistentDataPath, fileName);
#endif
        }
    }
}