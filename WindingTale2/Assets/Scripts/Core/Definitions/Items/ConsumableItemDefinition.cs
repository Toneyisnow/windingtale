using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
using UnityEditor.Localization.Plugins.XLIFF.V12;
using UnityEngine;
using WindingTale.Core.Common;
using WindingTale.Core.Files;

namespace WindingTale.Core.Definitions.Items
{
    /// <summary>
    /// @deprecated Should use EffectType instead
    /// </summary>
    public enum ItemUseType
    {
        Hp = 1,
        Mp = 2,
        AntiFreeze = 3,
        AntiPoison = 4,
        HpMax = 5,
        MpMax = 6,
        Ap = 7,
        Dp = 8,
        Mv = 9,
        Dx = 10,
        EyeStar = 11,
        EyeDark = 12,
        EyeIce = 13,
        EyeFire = 14
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
        public override ItemType GetItemType()
        {
            return ItemType.Consumable;
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

        public override string ToAttributeString()
        {
            string attriType = string.Empty;
            switch(this.UseType)
            {
                case ItemUseType.Hp:
                    attriType = "HP";
                    break;
                case ItemUseType.Mp:
                    attriType = "MP";
                    break;
                case ItemUseType.Ap:
                    attriType = "AP";
                    break;
                case ItemUseType.Dp:
                    attriType = "DP";
                    break;
                case ItemUseType.Mv:
                    attriType = "MV";
                    break;
                case ItemUseType.Dx:
                    attriType = "DX";
                    break;
                default: break;
            }

            string str = (attriType != string.Empty) ? string.Format("{0}+{1}", attriType, Quantity) : string.Empty;
            return str;
        }
    }
}