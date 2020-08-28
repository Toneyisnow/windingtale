using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WindingTale.Core.ObjectModels;

namespace WindingTale.Core.Components.Packs
{

    public class RefreshCreaturePack : PackBase
    {
        public FDCreature Creature
        {
            get; private set;
        }

        public RefreshCreaturePack(FDCreature creature)
        {
            this.Type = PackType.RefreshCreature;
            this.Creature = creature.Clone();
        }
    }
}