using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WindingTale.Core.Common;
using WindingTale.Core.Files;

namespace WindingTale.Core.Definitions.Items
{
    public enum ItemCategory
    {

    }

    public class AttackItemDefinition : ItemDefinition
    {
        

        public AttackItemDefinition()
        {

        }

        public static AttackItemDefinition ReadFromFile(ResourceDataFile fileReader, StringsDefinition stringsDefinition)
        {
            AttackItemDefinition def = new AttackItemDefinition();

            def.ItemId = fileReader.ReadInt();
            def.Category = (ItemCategory)fileReader.ReadInt();

            def.Name = stringsDefinition.GetString(def.ItemId.ToString());


            def.Price = fileReader.ReadInt();
            def.SellPrice = fileReader.ReadInt();
            def.Ap = fileReader.ReadInt();
            def.Dp = fileReader.ReadInt();
            def.Hit = fileReader.ReadInt();

            int scope = fileReader.ReadInt();

            if (scope <= 2)
            {
                def.AttackScope = new FDSpan(1, scope);
            }
            else
            {
                def.AttackScope = new FDSpan(2, scope);
            }

            def.Ev = 0;     // Currently there is no weapon that ev > 0

            return def;
        }

        public ItemCategory Category
        {
            get; private set;
        }

        public int Ap
        {
            get; private set;
        }

        public int Dp
        {
            get; private set;
        }

        public int Hit
        {
            get; private set;
        }

        public int Ev
        {
            get; private set;
        }

        public int GetPoisonRate()
        {
            if(this.ItemId == 214 || this.ItemId == 247)
            {
                return 10;
            }

            if (this.ItemId == 215)
            {
                return 10;
            }

            return 0;
        }

        public FDSpan AttackScope
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