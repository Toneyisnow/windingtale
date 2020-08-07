using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WindingTale.Core.ObjectModels;

namespace WindingTale.Core.Components.Packs
{
    public class ComposeCreaturePack : PackBase
    {
        public FDCreature Creature
        {
            get; private set;
        }

        public ComposeCreaturePack(FDCreature creature)
        {
            this.Type = PackType.ComposeCreature;
            this.Creature = creature;
        }
    }
}