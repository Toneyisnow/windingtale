using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WindingTale.Common;

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

        public static AttackItemDefinition ReadFromFile(ResourceDataFile fileReader)
        {
            AttackItemDefinition def = new AttackItemDefinition();

            def.ItemId = fileReader.ReadInt();
            def.Category = (ItemCategory)fileReader.ReadInt();

            def.Price = fileReader.ReadInt();
            def.SellPrice = fileReader.ReadInt();
            def.Ap = fileReader.ReadInt();
            def.Dp = fileReader.ReadInt();
            def.Hit = fileReader.ReadInt();

            def.Scope = fileReader.ReadInt();
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

        public int Scope
        {
            get; private set;
        }
        public FDRange GetAttackRange()
        {
            return null;
        }

        public override bool IsEquipment()
        {
            return true;
        }

        public override bool IsUsable()
        {
            throw new System.NotImplementedException();
        }
    }
}