
using WindingTale.Core.Definitions;

namespace WindingTale.Core.Objects
{
    /// <summary>
    /// 
    /// </summary>
    public class FDNpc : FDCreature
    {
        public AITypes AIType { get; private set; }

        public FDNpc(int id) : base(id, CreatureFaction.Npc)
        {
        }
    }
}