using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using UnityEngine;
using WindingTale.Common;
using WindingTale.Core.Definitions;

namespace WindingTale.Core.Components
{
    public class CreatureData
    {
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
            data.Ex = definition.InitialEx;

            data.Items = new List<int>();
            data.Magics = new List<int>();
            data.Effects = new List<int>();

            return data;
        }

        public CreatureData()
        {
            this.Items = new List<int>();
            this.Magics = new List<int>();
            this.Effects = new List<int>();
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
            data.Ex = this.Ex;

            data.Items = Shared.CloneList<int>(this.Items);
            data.Magics = Shared.CloneList<int>(this.Magics);
            data.Effects = Shared.CloneList<int>(this.Effects);

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

        public int Ex
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
                return GameFormula.CalculateAp(this);
            }
        }

        public int CalculatedDx
        {
            get
            {
                return GameFormula.CalculateAp(this);
            }
        }

        public int CalculatedMv
        {
            get
            {
                return GameFormula.CalculateAp(this);
            }
        }

        public int CalculatedEx
        {
            get
            {
                return GameFormula.CalculateAp(this);
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

        public List<int> Effects
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

        }

        public void AddItem(int itemId)
        {

        }

        public void EquipItemAt(int itemIndex)
        {

        }

        public bool CanSpellMagic()
        {
            // If the current creature is not having "Forbidden" effect
            return this.Magics != null && this.Magics.Count > 0 && !this.Effects.Contains(1);
        }

        public void AddMagic(int magicId)
        {

        }
    }
}