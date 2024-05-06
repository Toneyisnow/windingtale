using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WindingTale.Core.Files;

namespace WindingTale.Core.Definitions
{
    /// <summary>
    /// 
    /// </summary>
    public enum ShopType
    {
        Amor = 1,
        Item = 2,
        Secret = 3,
        Bar = 4,
        Church = 5,
    }

    public class ShopDefinition
    {
       
        public static int DefinitionKey(int chapterId, ShopType type)
        {
            return chapterId * 10 + type.GetHashCode();
        }

        public int ChapterId
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

        public int Key
        {
            get
            {
                return DefinitionKey(this.ChapterId, this.Type);
            }
        }

        public ShopDefinition()
        {
            this.Items = new List<int>();
        }

        public static ShopDefinition ReadFromFile(ResourceDataFile reader)
        {
            ShopDefinition def = new ShopDefinition();

            def.ChapterId = reader.ReadInt();
            if (def.ChapterId == -1)
            {
                return null;
            }

            def.Type = (ShopType)reader.ReadInt();

            int count = reader.ReadInt();
            for(int i= 0; i < count; i++)
            {
                int itemId = reader.ReadInt();
                def.Items.Add(itemId);
            }
            
            return def;
        }
    }
}