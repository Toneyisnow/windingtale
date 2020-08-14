using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WindingTale.Core.ObjectModels;

namespace WindingTale.Core.Components.Packs
{
    public enum CreatureInfoType
    {
        SelectItem = 1,
        SelectMagic = 2,
        View = 3,
    }

    public class ShowCreatureInfoPack : PackBase
    {
        public CreatureData CreatureData
        {
            get; private set;
        }

        public CreatureInfoType Type
        {
            get; private set;
        }

        public ShowCreatureInfoPack(CreatureData data, CreatureInfoType type)
        {
            this.CreatureData = data;
            this.Type = type;
        }
    }
}
