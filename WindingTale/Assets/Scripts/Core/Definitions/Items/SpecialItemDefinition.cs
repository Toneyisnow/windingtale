using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WindingTale.Common;

namespace WindingTale.Core.Definitions.Items
{
    public class SpecialItemDefinition : ItemDefinition
    {

        public static SpecialItemDefinition ReadFromFile(ResourceDataFile fileReader)
        {
            SpecialItemDefinition def = new SpecialItemDefinition();

            def.ItemId = fileReader.ReadInt();
            def.Price = fileReader.ReadInt();
            def.SellPrice = fileReader.ReadInt();

            return def;
        }

        public override bool IsEquipment()
        {
            return false;
        }

        public override bool IsUsable()
        {
            return false;
        }
    }
}