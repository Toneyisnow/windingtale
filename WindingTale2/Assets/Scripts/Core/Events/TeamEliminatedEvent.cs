using System;
using System.Collections.Generic;
using System.Linq;
using WindingTale.Core.Definitions;
using WindingTale.Core.Map;

namespace WindingTale.Core.Events
{
    public class TeamEliminatedEvent : FDConditionEvent
    {
        private CreatureFaction faction = CreatureFaction.Enemy;

        /// <summary>Enemies of these definitions do not count: the team is eliminated with them still standing.</summary>
        private HashSet<int> ignoredDefinitionIds = new HashSet<int>();

        public TeamEliminatedEvent(int eventId, CreatureFaction faction, Action game, params int[] ignoredDefinitionIds) : base(eventId, game)
        {
            this.faction = faction;
            this.ignoredDefinitionIds.UnionWith(ignoredDefinitionIds);
        }


        public override bool Match(FDMap gameMap)
        {
            if (faction == CreatureFaction.Enemy && gameMap.Enemies.All(c =>
                c.Definition != null && ignoredDefinitionIds.Contains(c.Definition.DefinitionId)))
            {
                return true;
            }

            if (faction == CreatureFaction.Friend && gameMap.Friends.Count == 0)
            {
                return true;
            }

            if (faction == CreatureFaction.Npc && gameMap.Npcs.Count == 0)
            {
                return true;
            }

            return false;
        }
    }
}