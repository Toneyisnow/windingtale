using WindingTale.Core.Definitions;
using WindingTale.Core.Map;

namespace WindingTale.Core.Events
{
    public class TeamEliminatedEvent : FDConditionEvent
    {
        private CreatureFaction faction = CreatureFaction.Enemy;

        public TeamEliminatedEvent(CreatureFaction faction)
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