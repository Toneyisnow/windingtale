using WindingTale.Core.Common;
using WindingTale.Core.Definitions;
using WindingTale.Core.Events;
using WindingTale.Core.Objects;
using WindingTale.Core.Files;
using System.Collections.Generic;
using WindingTale.Common;
using System;

namespace WindingTale.Core.Map
{
    /// <summary>
    /// This is the main function for Battle Map in the game.
    /// </summary>
    public class GameMap
    {
        public GameMap()
        { 
        }

        public GameField Field { get; private set; }


        /// <summary>
        /// 
        /// </summary>
        public List<FDEvent> Events { get; private set; }

        public List<FDTreasure> Treasures { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        public List<FDCreature> Creatures { get; private set; }

        public List<FDCreature> DeadCreatures { get; private set; }

        public List<FDCreature> Friends
        {
            get
            {
                return this.Creatures.FindAll(c => c.Faction == CreatureFaction.Friend);
            }
        }

        public List<FDCreature> Npcs
        {
            get
            {
                return this.Creatures.FindAll(c => c.Faction == CreatureFaction.Npc);
            }
        }
        public List<FDCreature> Enemies
        {
            get
            {
                return this.Creatures.FindAll(c => c.Faction == CreatureFaction.Enemy);
            }
        }

        public FDCreature GetCreatureById(int creatureId)
        {
            return this.Creatures.Find(c => c.Id == creatureId);
        }


        internal FDCreature GetCreatureAt(FDPosition position)
        {
            return this.Creatures.Find(c => c.Position.AreSame(position));
        }


        internal FDTreasure GetTreatureAt(FDPosition position)
        {
            return this.Treasures.Find(t => t.Position.AreSame(position));
        }

        public List<FDEvent> GetActiveEvents()
        {
            return this.Events.FindAll(ev => ev.IsActive);
        }

        public static GameMap LoadFromChapter(ChapterDefinition chapter, GameRecord record)
        {
            return null;
        }

        public static GameMap LoadFromSave(GameMapRecord mapRecord)
        {
            return null;
        }

    }
}