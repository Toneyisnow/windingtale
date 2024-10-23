using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WindingTale.Core.Files;

namespace WindingTale.Core.Definitions.Items
{
    public class SpecialItemDefinition : ItemDefinition
    {

        public static SpecialItemDefinition ReadFromFile(ResourceDataFile fileReader, StringsDefinition stringsDefinition)
        {
            SpecialItemDefinition def = new SpecialItemDefinition();

            def.ItemId = fileReader.ReadInt();
            def.Price = fileReader.ReadInt();
            def.SellPrice = fileReader.ReadInt();

            def.Name = stringsDefinition.GetString(def.ItemId.ToString());
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