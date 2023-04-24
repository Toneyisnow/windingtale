using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WindingTale.Core.ObjectModels;

namespace WindingTale.Core.Components.Packs
{
    public class CreatureDeadPack : PackBase
    {
        public List<int> CreatureIds
        {
            get; private set;
        }

        public CreatureDeadPack(int creatureId)
        {
            this.Type = PackType.CreatureDead;

            if (creatureId <= 0)
            {
                throw new ArgumentNullException("creature");
            }

            CreatureIds = new List<int>();
            this.CreatureIds.Add(creatureId);
        }

        public CreatureDeadPack()
        {
            CreatureIds = new List<int>();
        }

        public void Add(int creatureId)
        {
            if (creatureId <= 0)
            {
                throw new ArgumentNullException("creature");
            }

            this.CreatureIds.Add(creatureId);
        }


    }
}