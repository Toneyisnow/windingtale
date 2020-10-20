using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WindingTale.Common;

namespace WindingTale.Core.Components.Packs
{
    public class CreatureMovePack : PackBase
    {
        public int CreatureId
        {
            get; private set;
        }

        public FDMovePath MovePath
        {
            get; private set;
        }

        public CreatureMovePack(int creatureId, FDMovePath movePath)
        {
            this.Type = PackType.CreatureMove;
            this.CreatureId = creatureId;
            this.MovePath = movePath;
        }

    }
}
