using System;
using WindingTale.Core.Definitions;

namespace WindingTale.Core.Objects
{

    public class FDCreature : FDObject
    {
        public CreatureFaction Faction { get; private set; }

        public bool HasActioned { get; set; }


        public FDCreature(int id, CreatureFaction faction) : base(id, ObjectType.Creature)
        {
            this.Faction = faction;
        }
    }
}