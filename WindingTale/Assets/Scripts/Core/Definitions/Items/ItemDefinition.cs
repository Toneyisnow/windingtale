using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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


    }
}