using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WindingTale.Core.ObjectModels;

namespace WindingTale.Core.Components.Packs
{
    public class DisposeCreaturePack : PackBase
    {
        private FDCreature creature = null;

        public DisposeCreaturePack(FDCreature creature)
        {
            this.creature = creature;
        }
    }
}