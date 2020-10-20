using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WindingTale.Core.ObjectModels;

namespace WindingTale.Core.Components.Packs
{

    public class CreatureRefreshPack : PackBase
    {
        public FDCreature Creature
        {
            get; private set;
        }

        public CreatureRefreshPack(FDCreature creature)
        {
            this.Type = PackType.CreatureRefresh;
            this.Creature = creature.Clone();
        }
    }
}