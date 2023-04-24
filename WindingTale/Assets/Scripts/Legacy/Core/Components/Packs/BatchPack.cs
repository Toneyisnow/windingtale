using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WindingTale.Core.Components.Packs
{
    public class BatchPack : PackBase
    {
        public List<PackBase> Packs
        {
            get; private set;
        }

        public BatchPack()
        {
            this.Type = PackType.Batch;
            this.Packs = new List<PackBase>();

        }

        public void Add(PackBase pack)
        {
            this.Packs.Add(pack);
        }

    }
}