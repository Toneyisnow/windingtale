using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WindingTale.Common;

namespace WindingTale.Core.Definitions
{

    public abstract class ItemDefinition
    {
        public enum ItemType
        {
            Attack = 1,
            Defend = 2,
            Consumable = 3,
            Money = 4,
            Special = 5
        }

        public int ItemId
        {
            get; set;
        }

        public ItemType Type
        {
            get; set;
        }

        public static ItemDefinition ReadFromFile(ResourceDataFile dataFile)
        {
            return null;
        }

        public bool IsEquipment()
        {
            return false;
        }

        public bool IsUsable()
        {
            return false;
        }

    }
}