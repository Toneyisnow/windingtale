using WindingTale.Common;
using WindingTale.Core.Components.Algorithms;
using WindingTale.Core.Definitions;
using WindingTale.Core.Map;
using WindingTale.Core.Objects;

namespace WindingTale.Core
{
    public class GameHandler
    {
        private GameMap gameMap = null;


        public GameHandler(GameMap map)
        {
            this.gameMap = map;
        }

        /// <summary>
        /// This function will actually update the creature and target, the returned
        /// result is only for UI display.
        /// </summary>
        /// <param name="creature"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        public AttackResult HandleCreatureAttack(FDCreature creature, FDCreature target)
        {
            return null;
        }

        /// <summary>
        /// This function will actually update the creature and target, the returned
        /// result is only for UI display.
        /// </summary>
        /// <param name="creature"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        public MagicResult HandleCreatureMagic(FDCreature creature, FDPosition position, MagicDefinition mgaic)
        {
            return null;
        }


        /// <summary>
        /// This function will actually update the creature and target, the returned
        /// result is only for UI display.
        /// </summary>
        /// <param name="creature"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        public ItemResult HandleCreatureItem(FDCreature creature, FDPosition position, ItemDefinition item)
        {
            return null;
        }




    }
}