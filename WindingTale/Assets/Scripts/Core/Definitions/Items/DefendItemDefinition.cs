using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WindingTale.Common;

namespace WindingTale.Core.Definitions.Items
{
    /// <summary>
    /// 
    /// </summary>
    public class DefendItemDefinition : ItemDefinition
    {
        public static DefendItemDefinition ReadFromFile(ResourceDataFile fileReader)
        {
            DefendItemDefinition def = new DefendItemDefinition();

            def.ItemId = fileReader.ReadInt();
            def.Category = (ItemCategory)fileReader.ReadInt();
            def.Price = fileReader.ReadInt();
            def.SellPrice = fileReader.ReadInt();

            def.Dp = fileReader.ReadInt();
            def.Ev = fileReader.ReadInt();

            return def;
        }

        public ItemCategory Category
        {
            get; private set;
        }

        public int Dp
        {
            get; private set;
        }

        public int Ev
        {
            get; private set;
        }
        public override bool IsEquipment()
        {
            return true;
        }

        public override bool IsUsable()
        {
            return false;
        }
    }
}