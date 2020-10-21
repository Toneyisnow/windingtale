using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using UnityEngine;
using WindingTale.Common;
using WindingTale.Core.Definitions;
using WindingTale.Core.Definitions.Items;

namespace WindingTale.Core.Components.Data
{
    public class CreatureData
    {
        public enum CreatureEffects
        {
            EnhancedAp = 1,
            EnhancedDp = 2,
            Forbidden = 3,
            Frozen = 4,
            Poisoned = 5,
        }

        public enum AITypes
        {
            AIType_Aggressive = 0,
            AIType_Defensive = 1,
            AIType_Guard = 2,
            AIType_Escape = 3,
            AIType_StandBy = 4,
            AIType_Treasure = 5,
            AIType_UnNoticable = 6,     // This one is special, that is set on AI target objects
        }

        public static CreatureData FromDefinition(int creatureId, CreatureDefinition definition)
        {
            CreatureData data = new CreatureData();
            data.CreatureId = creatureId;
            data.DefinitionId = definition.DefinitionId;

            data.Level = definition.InitialLevel;
            data.Hp = data.HpMax = definition.InitialHp;
            data.Mp = data.MpMax = definition.InitialMp;

            data.Ap = definition.InitialAp;
            data.Dp = definition.InitialDp;
            data.Dx = definition.InitialDx;
            data.Mv = definition.InitialMv;
            data.Exp = definition.InitialEx;

            data.Items = definition.Items;
            data.Magics = definition.Magics;

            // Get Equiped items
            data.AttackItemIndex = -1;
            data.DefendItemIndex = -1;
            for (int i = 0; i < data.Items.Count; i++)
            {
                int itemId = data.Items[i];
                ItemDefinition item = DefinitionStore.Instance.GetItemDefinition(itemId);
                if (item != null && item is AttackItemDefinition)
                {
                    data.AttackItemIndex = i;
                }
                if (item != null && item is DefendItemDefinition)
                {
                    data.DefendItemIndex = i;
                }
            }

            data.Effects = new List<CreatureEffects>();
            
            return data;
        }

        public CreatureData()
        {
            this.Items = new List<int>();
            this.Magics = new List<int>();
            this.Effects = new List<CreatureEffects>();

            this.AttackItemIndex = -1;
            this.DefendItemIndex = -1;
        }

        public CreatureData Clone()
        {
            CreatureData data = new CreatureData();
            data.CreatureId = this.CreatureId;
            data.DefinitionId = this.DefinitionId;

            data.Level = this.Level;
            data.Hp = this.Hp;
            data.Mp = this.Mp;
            data.HpMax = this.HpMax;
            data.MpMax = this.MpMax;

            data.Ap = this.Ap;
            data.Dp = this.Dp;
            data.Dx = this.Dx;
            data.Mv = this.Mv;
            data.Exp = this.Exp;

            data.Items = Shared.CloneList<int>(this.Items);
            data.Magics = Shared.CloneList<int>(this.Magics);
            data.Effects = Shared.CloneList<CreatureEffects>(this.Effects);

            data.AttackItemIndex = this.AttackItemIndex;
            data.DefendItemIndex = this.DefendItemIndex;

            data.LastGainedExperience = this.LastGainedExperience;
            data.AIType = this.AIType;

            return data;
        }

        public int CreatureId
        {
            get; set;
        }

        public int DefinitionId
        {
            get; set;
        }

        public int AttackItemIndex
        {
            get; private set;
        }

        public int DefendItemIndex
        {
            get; private set;
        }

        public int Level
        {
            get; set;
        }

        public int Hp
        {
            get; set;
        }

        public int Mp
        {
            get; set;
        }

        public int HpMax
        {
            get; set;
        }

        public int MpMax
        {
            get; set;
        }

        public int Ap
        {
            get; set;
        }

        public int Dp
        {
            get; set;
        }

        public int Dx
        {
            get; set;
        }

        public int Mv
        {
            get; set;
        }

        public int Exp
        {
            get; set;
        }

        public int Hit
        {
            get; set;
        }

        public int Ev
        {
            get; set;
        }

        public int LastGainedExperience
        {
            get; set;
        }

        /// <summary>
        /// Only valid when the creature is enemy or Npc
        /// </summary>
        public AITypes AIType
        {
            get; set;
        }

        public int CalculatedAp
        {
            get
            {
                return GameFormula.CalculateAp(this);
            }
        }

        public int CalculatedDp
        {
            get
            {
                return GameFormula.CalculateDp(this);
            }
        }

        public int CalculatedDx
        {
            get
            {
                return GameFormula.CalculateDx(this);
            }
        }

        public int CalculatedMv
        {
            get
            {
                return this.Mv;
            }
        }

        public int CalculatedEv
        {
            get
            {
                return GameFormula.CalculateEv(this);
            }
        }

        public int CalculatedHit
        {
            get
            {
                return GameFormula.CalculateHit(this);
            }
        }


        public List<int> Items
        {
            get; private set;
        }

        public List<int> Magics
        {
            get; private set;
        }

        public List<CreatureEffects> Effects
        {
            get; private set;
        }

        /// <summary>
        /// This is only used for Enemy
        /// </summary>
        public int DropItem
        {
            get; set;
        }

        public bool IsItemsFull()
        {
            return this.Items != null && this.Items.Count >= 8;
        }

        public void RemoveItemAt(int itemIndex)
        {
            if (itemIndex < 0 || itemIndex >= this.Items.Count)
            {
                return;
            }

            this.Items.RemoveAt(itemIndex);
        }

        public int GetItemAt(int itemIndex)
        {
            if (this.Items == null || itemIndex < 0 || itemIndex >= this.Items.Count)
            {
                return -1;
            }

            return this.Items[itemIndex];
        }

        public void AddItem(int itemId)
        {
            if(IsItemsFull())
            {
                return;
            }

            this.Items.Add(itemId);
        }

        public bool HasItem()
        {
            return this.Items != null && this.Items.Count > 0;
        }

        public bool HasItem(int itemId)
        {
            return this.HasItem() && this.Items.Contains(itemId);
        }

        public void EquipItemAt(int itemIndex)
        {
            int itemId = this.Items[itemIndex];
            ItemDefinition item = DefinitionStore.Instance.GetItemDefinition(itemId);

            if (!item.IsEquipment())
            {
                return;
            }

            if (item is AttackItemDefinition)
            {
                // Attack Item
                this.AttackItemIndex = itemIndex;
            }
            else if (item is DefendItemDefinition)
            {
                // Defend Item
                this.DefendItemIndex = itemIndex;
            }

        }

        public void UpdateHp(int deltaHp)
        {
            this.Hp += deltaHp;

            this.Hp = Math.Max(this.Hp, 0);
            this.Hp = Math.Min(this.Hp, this.HpMax);
        }

        public void UpdateMp(int deltaMp)
        {
            this.Mp += deltaMp;

            this.Mp = Math.Max(this.Mp, 0);
            this.Mp = Math.Min(this.Mp, this.MpMax);
        }

        public void SetLastGainedExperience(int exp)
        {
            LastGainedExperience = (exp > 99) ? 99 : exp;
        }

        
        public AttackItemDefinition GetAttackItem()
        {
            int itemId = this.GetItemAt(this.AttackItemIndex);

            ItemDefinition item = DefinitionStore.Instance.GetItemDefinition(itemId);
            if (item == null)
            {
                return null;
            }

            return item as AttackItemDefinition;
        }

        public DefendItemDefinition GetDefendItem()
        {
            int itemId = this.GetItemAt(this.DefendItemIndex);

            ItemDefinition item = DefinitionStore.Instance.GetItemDefinition(itemId);
            if (item == null)
            {
                return null;
            }

            return item as DefendItemDefinition;
        }

        public int GetMagicAt(int magicIndex)
        {
            if (this.Magics != null || magicIndex < 0 || magicIndex >= this.Magics.Count)
            {
                return -1;
            }

            return this.Magics[magicIndex];
        }

        public bool CanSpellMagic()
        {
            // If the current creature is not having "Forbidden" effect
            return this.HasMagic() && !this.Effects.Contains(CreatureEffects.Forbidden);
        }

        public bool HasMagic()
        {
            return this.Magics != null && this.Magics.Count > 0;
        }

        public bool HasAfterMoveMagic()
        {
            if(!HasMagic())
            {
                return false;
            }
            
            foreach(int magicId in this.Magics)
            {
                MagicDefinition magic = DefinitionStore.Instance.GetMagicDefinition(magicId);
                if (magic.AllowAfterMove)
                {
                    return true;
                }
            }

            return false;
        }

        public bool CanAttack()
        {
            return this.AttackItemIndex >= 0;
        }

        public void AddMagic(int magicId)
        {
            if(this.Magics == null)
            {
                this.Magics = new List<int>();
            }

            if (!this.Magics.Contains(magicId))
            {
                this.Magics.Add(magicId);
            }
        }

        public void SetEffect(CreatureEffects effect)
        {
            if (this.Effects == null)
            {
                this.Effects = new List<CreatureEffects>();
            }

            this.Effects.Add(effect);
        }

        public LevelUpInfo LevelUp()
        {
            return null;
        }
    }
}