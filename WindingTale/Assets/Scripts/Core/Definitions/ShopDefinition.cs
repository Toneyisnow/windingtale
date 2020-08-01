using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WindingTale.Core.Definitions
{
    public class ShopDefinition
    {
        public enum ShopType
        {
            Amor = 1,
            Item = 2,
            Secret = 3,
            Bar = 4,
            Church = 5,
        }

        public int StageId
        {
            get; set;
        }

        public ShopType Type
        {
            get; set;
        }

        public List<int> Items
        {
            get; set;
        }


    }
}