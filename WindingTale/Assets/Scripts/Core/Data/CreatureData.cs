using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WindingTale.Common;

namespace WindingTale.Core.Components
{
    public class CreatureData
    {
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
            get; set;
        }

        public List<int> Magics
        {
            get; set;
        }

        /// <summary>
        /// This is only used for Enemy
        /// </summary>
        public int DropItem
        {
            get; set;
        }
    }
}