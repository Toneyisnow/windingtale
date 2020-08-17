using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WindingTale.Core.ObjectModels;

namespace WindingTale.Core.Components.Packs
{
    public enum CreatureInfoType
    {
        View = 1,
        SelectEquipItem = 2,
        SelectUseItem = 3,
        SelectAllItem = 4,
        SelectMagic = 5,
        
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
