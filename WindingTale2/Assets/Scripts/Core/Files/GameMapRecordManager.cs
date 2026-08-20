
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
    /// Saved in the Battle Field so that could load from Continue button.
    ///
    /// This class owns only the *dynamic* half of a battle: where everyone stands, what
    /// state they are in, who has died, which chests are emptied, which events have
    /// already fired. The *static* half -- the field shapes, the obstacles, the chest
    /// placements, the chapter's event scripts -- is not in the save at all; it is
    /// rebuilt from the chapter definition by GameMain.LoadChapter, which must run
    /// first. See GameMain.ContinueFromRecord for the two steps in order.
    /// </summary>
    public class GameMapRecordManager
    {

        /// <summary>
        /// Whether any save exists to continue from -- what the Title scene checks, since
        /// it has no battle of its own and will follow the record into whichever chapter
        /// the record names.
        /// </summary>
        public static bool HasSavedGame(string recordName)
        {
            return File.Exists(GetSaveFilePath(recordName));
        }

        /// <summary>
        /// Whether a save exists *for this chapter* -- what the in-battle Load checks. That
        /// menu item means "restart this battle from my save", so a record belonging to
        /// another chapter is no more use to it than no record at all: taking it would drop
        /// the player onto a different map altogether. Only the Title scene's Continue is
        /// allowed to cross chapters.
        /// </summary>
        public static bool HasSavedGame(string recordName, int chapterId)
        {
            GameMapRecord record = ReadRecord(recordName);
            return record != null && record.ChapterId == chapterId;
        }

        /// <summary>
        /// Reads and deserializes the save, or null when there is none. Pure file work: it
        /// touches no game state, so the caller can read the ChapterId out of it and load
        /// that chapter before applying the rest.
        ///
        /// Stays quiet about a missing file -- it is also called to probe whether a save is
        /// there at all (see the overload above), where absence is an ordinary answer, not
        /// a fault. Callers that were promised a record warn for themselves.
        /// </summary>
        public static GameMapRecord ReadRecord(string recordName)
        {
            string fullFilePath = GetSaveFilePath(recordName);

            if (!File.Exists(fullFilePath))
            {
                return null;
            }

            string json = File.ReadAllText(fullFilePath);
            GameMapRecord record = JsonConvert.DeserializeObject<GameMapRecord>(json);

            if (record != null)
            {
                Debug.Log(string.Format(
                    "Game record read: chapter {0}, turn {1}.", record.ChapterId, record.TurnNo));
            }

            return record;
        }

        /// <summary>
        /// Lays the saved battle state over an already-loaded chapter. Everything the
        /// chapter definition provides is left exactly as LoadChapter built it -- this
        /// only writes back what the players changed during the battle.
        ///
        /// Precondition: gameMain.LoadChapter(record.ChapterId) has run, so the map,
        /// the field and the chapter's event list all exist.
        /// </summary>
        public void ApplyRecord(GameMapRecord record, GameMain gameMain)
        {
            if (record == null)
            {
                return;
            }

            FDMap map = gameMain.gameMap.Map;

            // Events already fired during the saved battle must not fire a second time.
            // The list itself came from the chapter, so only the active flags are ours.
            ApplyTriggeredEvents(gameMain.eventHandler, record.TriggeredEvents);

            ApplyTurn(map, record);
            map.TotalMoney = record.TotalMoney;

            // Remember which turn this battle was restored into, so the record menu can
            // refuse to save it straight back onto the same state. See FDMap.CanSaveGame.
            map.RestoredTurnNo = record.TurnNo;

            // The chapter's own creature spawns live inside its turn events, all of
            // which are now marked as fired, so the map starts empty and every creature
            // on it comes from the record.
            if (record.Creatures != null)
            {
                foreach (CreatureMapRecord creatureRecord in record.Creatures)
                {
                    FDCreature creature = ConvertRecordToCreature(creatureRecord);
                    gameMain.gameMap.AddCreature(creature, creature.Position);
                }
            }

            // Dead creatures are data only -- no icon is drawn for them -- but they are
            // still needed for the portraits their death conversations use.
            map.DeadCreatures = record.DeadCreatures == null
                ? new List<FDCreature>()
                : record.DeadCreatures.Select(creature => ConvertRecordToCreature(creature)).ToList();

            ApplyTreasures(map, record.Treasures);
        }

        private static void ApplyTriggeredEvents(EventHandler eventHandler, List<int> triggeredEvents)
        {
            if (eventHandler == null || triggeredEvents == null)
            {
                return;
            }

            foreach (int eventId in triggeredEvents)
            {
                FDEvent evt = eventHandler.events.Find(e => e.EventId == eventId);
                if (evt != null)
                {
                    evt.SetActive(false); // Mark the event as inactive
                }
            }
        }

        /// <summary>
        /// Rewinds the turn counter by exactly one phase, because the onKickOff that
        /// follows a load runs onStartNextTurn, which advances the phase (and rolls the
        /// turn number over on Enemy -> Friend). Landing one phase early makes that
        /// advance arrive at the phase the save was actually taken in.
        ///
        /// This is why a battle may only be saved at the start of a turn: the load resumes
        /// at a phase boundary, so it replays the saved phase from its beginning. Saving
        /// part-way through one would silently rewind whatever had already happened in it.
        /// </summary>
        private static void ApplyTurn(FDMap map, GameMapRecord record)
        {
            switch (record.TurnType)
            {
                case CreatureFaction.Npc:
                    map.TurnNo = record.TurnNo;
                    map.TurnType = CreatureFaction.Friend;
                    break;
                case CreatureFaction.Enemy:
                    map.TurnNo = record.TurnNo;
                    map.TurnType = CreatureFaction.Npc;
                    break;
                default:
                    // Friend: the previous phase is the Enemy phase of the turn before.
                    map.TurnNo = record.TurnNo - 1;
                    map.TurnType = CreatureFaction.Enemy;
                    break;
            }
        }

        /// <summary>
        /// Restores what a battle can change about a chest -- whether it has been emptied,
        /// and which item it holds after an exchange. The chests themselves came from the
        /// chapter (LoadChapter placed them and ObjectsLayer has already drawn them), so
        /// they are updated in place rather than swapped for a fresh list: replacing the
        /// list would leave the drawn chests keyed to objects nothing writes to any more.
        /// </summary>
        private void ApplyTreasures(FDMap map, List<TreasureMapRecord> treasureRecords)
        {
            if (treasureRecords == null)
            {
                return;
            }

            foreach (TreasureMapRecord treasureRecord in treasureRecords)
            {
                FDTreasure treasure = map.Treasures.Find(t => t.Id == treasureRecord.Id);
                if (treasure == null)
                {
                    // A chest the chapter no longer declares (an edited chapter file, or a
                    // chest that only ever existed in the save). Keep it: dropping it would
                    // lose an item the player can still collect.
                    treasure = ConvertRecordToTreasure(treasureRecord);
                    map.Treasures.Add(treasure);
                    continue;
                }

                treasure.UpdateItem(treasureRecord.ItemId);

                // HasOpened is write-once through Open(); an emptied chest must stay emptied
                // across a save/load or its contents could be collected twice.
                if (treasureRecord.HasOpened)
                {
                    treasure.Open();
                }
            }
        }

        /// <summary>
        /// Takes the snapshot, without deciding what becomes of it. SaveToFile writes it
        /// to the save slot; winning a chapter instead boils it down to the party that
        /// walks on to the village (see GameRecordManager.CreateFromMapRecord), and never
        /// touches the save file at all.
        /// </summary>
        public GameMapRecord BuildRecord(GameMain gameMain)
        {
            GameMapRecord gameMapRecord = new GameMapRecord();

            FDMap map = gameMain.gameMap.Map;
            List<int> triggeredEvents = gameMain.eventHandler.events.FindAll(evt => !evt.IsActive).Select( evt => evt.EventId).ToList();

            gameMapRecord.ChapterId = map.ChapterId;
            gameMapRecord.TurnNo = map.TurnNo;
            gameMapRecord.TurnType = map.TurnType;
            gameMapRecord.TotalMoney = map.TotalMoney;
            gameMapRecord.Creatures = map.Creatures.Select(creature => ConvertCreatureToRecord(creature) ).ToList();
            gameMapRecord.DeadCreatures = map.DeadCreatures.Select(creature => ConvertCreatureToRecord(creature)).ToList();
            gameMapRecord.Treasures = map.Treasures.Select(treasure => ConvertTreasureToRecord(treasure)).ToList();
            gameMapRecord.TriggeredEvents = triggeredEvents;

            return gameMapRecord;
        }

        /// <summary>
        /// Writes the battle's dynamic state to the save slot. Only ever called at the
        /// start of a turn, before anyone has moved (the record menu gates on
        /// FDMap.CanSaveGame), which is what lets the record leave per-creature turn
        /// state out entirely -- see CreatureMapRecord and ApplyTurn.
        /// </summary>
        public void SaveToFile(string recordName, GameMain gameMain)
        {
            GameMapRecord gameMapRecord = BuildRecord(gameMain);

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
            record.HpMax = creature.HpMax;
            record.MpMax = creature.MpMax;
            record.Ap = creature.Ap;
            record.Dp = creature.Dp;
            record.Dx = creature.Dx;
            record.Mv = creature.Mv;
            record.Exp = creature.Exp;
            record.ItemIds = creature.Items;
            record.MagicIds = creature.Magics;
            record.AttackItemIndex = creature.AttackItemIndex;
            record.DefendItemIndex = creature.DefendItemIndex;
            record.Effects = creature.Effects.ToList();
            record.Position = creature.Position;

            if (creature is FDAICreature aiCreature)
            {
                record.AIType = aiCreature.AIType;
                record.AIEscapePosition = aiCreature.EscapePosition;
                record.AITreasurePosition = aiCreature.TreasurePosition;
            }

            return record;
        }

        private FDCreature ConvertRecordToCreature(CreatureMapRecord record)
        {
            return CreateCreatureFromRecord(record);
        }

        /// <summary>
        /// Rebuilds a live creature from a saved record: the same conversion the battle load
        /// uses, exposed so a creature can be reconstructed outside a battle too -- the shop's
        /// creature picker turns each GameRecord.Friends entry back into an FDCreature this way
        /// to show its icon, name and info panel.
        /// </summary>
        public static FDCreature CreateCreatureFromRecord(CreatureMapRecord record)
        {
            CreatureDefinition definition = DefinitionStore.Instance.GetCreatureDefinition(record.DefinitionId);
            FDCreature creature = record.Faction == CreatureFaction.Friend ?
                new FDCreature(record.Id, definition, record.Faction) :
                new FDAICreature(record.Id, definition, record.Faction, record.AIType);

            if (creature is FDAICreature restoredAiCreature)
            {
                restoredAiCreature.EscapePosition = record.AIEscapePosition;
                restoredAiCreature.TreasurePosition = record.AITreasurePosition;
            }

            creature.Level = record.Level;
            creature.Hp = record.Hp;
            creature.Mp = record.Mp;
            creature.HpMax = record.HpMax;
            creature.MpMax = record.MpMax;
            creature.Ap = record.Ap;
            creature.Dp = record.Dp;
            creature.Dx = record.Dx;
            creature.Mv = record.Mv;
            creature.Exp = record.Exp;
            creature.Items = record.ItemIds;
            creature.Magics = record.MagicIds;
            creature.Position = record.Position;

            // The constructor equipped whatever the *definition* listed; the saved indices
            // point into the saved item list, which exchanges may have reordered. Re-equip
            // from the record so the creature keeps the weapon it actually had.
            creature.EquipItemAt(record.AttackItemIndex);
            creature.EquipItemAt(record.DefendItemIndex);

            if (record.Effects != null)
            {
                foreach (CreatureEffects effect in record.Effects)
                {
                    creature.Effects.Add(effect);
                }
            }

            return creature;
        }

        private TreasureMapRecord ConvertTreasureToRecord(FDTreasure treasure)
        {
            TreasureMapRecord record = new TreasureMapRecord();
            record.Id = treasure.Id;
            record.ItemId = treasure.ItemId;
            record.HasOpened = treasure.HasOpened;
            record.Type = treasure.Type;
            record.Position = treasure.Position;
            return record;
        }

        private FDTreasure ConvertRecordToTreasure(TreasureMapRecord record)
        {
            // Saves written before TreasureMapRecord.Type existed decode it as 0, which
            // is not a TreasureType. Fall back to RedBox rather than 0: an unrecognised
            // type would draw no chest at all, silently losing a still-unopened one.
            TreasureType type = System.Enum.IsDefined(typeof(TreasureType), record.Type)
                ? record.Type
                : TreasureType.RedBox;

            FDTreasure treasure = new FDTreasure(record.Id, record.ItemId, type);
            treasure.Position = record.Position;

            // HasOpened is write-once through Open(); an emptied chest must stay emptied
            // across a save/load or its contents could be collected twice.
            if (record.HasOpened)
            {
                treasure.Open();
            }

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