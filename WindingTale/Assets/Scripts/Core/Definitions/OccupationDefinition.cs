using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WindingTale.Common;

namespace WindingTale.Core.Definitions
{
    public class OccupationDefinition
    {
        public int OccupationId
        {
            get; private set;
        }

        public List<int> AttackItemCategories
        {
            get; private set;
        }

        public List<int> DefendItemCategories
        {
            get; private set;
        }

        public int MagicDefendRate
        {
            get; private set;
        }

        public static OccupationDefinition ReadFromFile(ResourceDataFile reader)
        {
            OccupationDefinition def = new OccupationDefinition();

            def.OccupationId = reader.ReadInt();

            int count = reader.ReadInt();
            def.AttackItemCategories = new List<int>();
            for (int i = 0; i < count; i++)
            {
                int category = reader.ReadInt();
                def.AttackItemCategories.Add(category);
            }

            count = reader.ReadInt();
            def.DefendItemCategories = new List<int>();
            for (int i = 0; i < count; i++)
            {
                int category = reader.ReadInt();
                def.DefendItemCategories.Add(category);
            }

            def.MagicDefendRate = reader.ReadInt();

            return def;
        }

        public bool CanUseAttackItem(int attackItemCategory)
        {
            return this.AttackItemCategories.Contains(attackItemCategory);
        }

        public bool CanUseDefendItem(int defendItemCategory)
        {
            return this.DefendItemCategories.Contains(defendItemCategory);
        }
    }
}