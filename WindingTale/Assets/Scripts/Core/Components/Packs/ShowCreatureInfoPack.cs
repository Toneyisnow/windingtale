using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WindingTale.Core.Components.Data;
using WindingTale.Core.ObjectModels;

namespace WindingTale.Core.Components.Packs
{
    public enum CreatureInfoType
    {
        SelectEquipItem = 1,
        SelectUseItem = 2,
        SelectAllItem = 3,
        SelectMagic = 4,
        View = 5,
    }

    public class ShowCreatureInfoPack : PackBase
    {
        public CreatureData CreatureData
        {
            get; private set;
        }

        public CreatureInfoType InfoType
        {
            get; private set;
        }

        public ShowCreatureInfoPack(CreatureData data, CreatureInfoType type)
        {
            this.CreatureData = data;
            this.InfoType = type;
        }
    }
}
