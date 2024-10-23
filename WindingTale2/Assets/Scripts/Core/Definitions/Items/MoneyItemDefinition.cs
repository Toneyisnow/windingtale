using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WindingTale.Core.Files;

namespace WindingTale.Core.Definitions.Items
{
    public class MoneyItemDefinition : ItemDefinition
    {
        public static MoneyItemDefinition ReadFromFile(ResourceDataFile fileReader, StringsDefinition stringsDefinition)
        {
            MoneyItemDefinition def = new MoneyItemDefinition();

            def.ItemId = fileReader.ReadInt();
            def.Amount = fileReader.ReadInt();

            def.Name = stringsDefinition.GetString(def.ItemId.ToString());
            return def;
        }

        public int Amount
        {
            get; set;
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