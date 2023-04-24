using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WindingTale.Core.ObjectModels;

namespace WindingTale.Core.Components.Packs
{
    public class CreatureDisposePack : PackBase
    {
        public int CreatureId
        {
            get; private set;
        }

        public CreatureDisposePack(int creatureId)
        {
            this.Type = PackType.CreatureDispose;

            this.CreatureId = creatureId;
        }
    }
}