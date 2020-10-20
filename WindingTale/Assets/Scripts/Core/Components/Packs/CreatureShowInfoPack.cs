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

    public class CreatureShowInfoPack : PackBase
    {
        public FDCreature Creature
        {
            get; private set;
        }

        public CreatureInfoType InfoType
        {
            get; private set;
        }

        public CreatureShowInfoPack(FDCreature creature, CreatureInfoType type)
        {
            this.Type = PackType.CreatureShowInfo;
            this.Creature = creature;
            this.InfoType = type;
        }
    }
}
