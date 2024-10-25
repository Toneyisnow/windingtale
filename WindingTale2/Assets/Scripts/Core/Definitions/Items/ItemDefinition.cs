using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WindingTale.Core.Common;

namespace WindingTale.Core.Definitions
{

    public abstract class ItemDefinition
    {
        /// <summary>
        /// This is not used now, just using "is" operation
        /// </summary>
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
            get; protected set;
        }

        public int Price
        {
            get; protected set;
        }

        public int SellPrice
        {
            get; protected set;
        }

        /// <summary>
        /// This should be a localized string
        /// </summary>
        public string Name
        {
            get; set;
        }

        public abstract bool IsEquipment();

        public abstract bool IsUsable();

    }
}