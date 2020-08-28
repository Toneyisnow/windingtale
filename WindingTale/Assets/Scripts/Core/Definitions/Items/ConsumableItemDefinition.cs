using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
using UnityEngine;
using WindingTale.Common;

namespace WindingTale.Core.Definitions.Items
{
    public enum ItemUseType
    {
        Hp,
        Mp,
    }

    public class ConsumableItemDefinition : ItemDefinition
    {
        public static ConsumableItemDefinition ReadFromFile(ResourceDataFile fileReader)
        {
            ConsumableItemDefinition def = new ConsumableItemDefinition();

            def.ItemId = fileReader.ReadInt();
            def.Price = fileReader.ReadInt();
            def.SellPrice = fileReader.ReadInt();

            def.UseType = (ItemUseType)fileReader.ReadInt();
            def.Quantity = fileReader.ReadInt();
            def.IsReusable = (fileReader.ReadInt() == 1);

            return def;
        }

        public ItemUseType UseType
        {
            get; private set;
        }

        public int Quantity
        {
            get; private set;
        }

        public override bool IsEquipment()
        {
            return false;
        }

        public override bool IsUsable()
        {
            return true;
        }

        public bool IsReusable
        {
            get; private set;
        }
    }
}