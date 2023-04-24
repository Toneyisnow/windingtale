namespace WindingTale.Core.Objects
{
    public enum CreatureFaction
    {
        Friend = 0,
        Npc = 1,
        Enemy = 2,
    }

    public class FDCreature : FDObject
    {
        public CreatureFaction Faction { get; private set; }


        public FDCreature(int id, CreatureFaction faction) : base(id, ObjectType.Creature)
        {
            this.Faction = faction;
        }
    }
}