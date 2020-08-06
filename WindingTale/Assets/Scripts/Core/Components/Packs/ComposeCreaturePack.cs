using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WindingTale.Core.ObjectModels;

namespace WindingTale.Core.Components.Packs
{
    public class ComposeCreaturePack : PackBase
    {
        private FDCreature creature = null;

        public ComposeCreaturePack(FDCreature creature)
        {
            this.creature = creature;
        }
    }
}