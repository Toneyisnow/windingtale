
namespace WindingTale.Core.Objects
{
    public class FDNpc : FDCreature
    {
        public AITypes AIType { get; private set; }

        public FDNpc(int id) : base(id, CreatureFaction.Npc)
        {
        }
    }
}