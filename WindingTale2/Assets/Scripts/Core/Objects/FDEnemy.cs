
using WindingTale.Core.Definitions;

namespace WindingTale.Core.Objects
{
    /// <summary>
    /// Reprents an enemy in the game.
    /// </summary>
    public class FDEnemy : FDAICreature
    {
        public int DropItem
        {
            get; set;
        }


        public FDEnemy(int id) : base(id, CreatureFaction.Enemy)
        {
        }
    }
}