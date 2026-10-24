using UnityEngine;

namespace WindingTale.MapObjects.GameMap
{
    /// <summary>
    /// A figure GameMap.AddStillFigure stood on the map as scenery. It stands until the
    /// creature it shows is on the map itself -- woken, or loaded from a save that already
    /// has him -- or lies among the fallen, and then takes itself away.
    /// </summary>
    public class StillFigure : MonoBehaviour
    {
        private GameMap gameMap = null;
        private int creatureId = 0;

        public void Initialize(GameMap gameMap, int creatureId)
        {
            this.gameMap = gameMap;
            this.creatureId = creatureId;
        }

        void Update()
        {
            if (gameMap == null || gameMap.Map == null)
            {
                return;
            }

            if (gameMap.Map.GetCreatureById(creatureId) != null
                || gameMap.Map.DeadCreatures.Exists(c => c.Id == creatureId))
            {
                Destroy(gameObject);
            }
        }
    }
}
