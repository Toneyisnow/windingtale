using System;
using WindingTale.Core.Definitions;
using WindingTale.Core.Map;
using WindingTale.UI.Scenes.Game;

namespace WindingTale.Core.Events
{
    public class TeamEliminatedEvent : FDConditionEvent
    {
        private CreatureFaction faction = CreatureFaction.Enemy;

        public TeamEliminatedEvent(int eventId, CreatureFaction faction, Action<GameMain> game) : base(eventId, game)
        {
            this.faction = faction;
        }


        public override bool Match(GameMap gameMap)
        {
            if (faction == CreatureFaction.Enemy && gameMap.Enemies.Count == 0)
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