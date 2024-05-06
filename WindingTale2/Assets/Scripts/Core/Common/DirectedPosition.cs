using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace WindingTale.Core.Common
{
    /// <summary>
    /// Directed Position for calculating the Move path
    /// </summary>
    public class DirectedPosition : FDPosition
    {
        public DirectedPosition FromDirection { get; private set; }


        public int LeftMovePoint
        {
            get; set;
        }

        public bool IsSkipped { get; set; }

        public DirectedPosition(FDPosition pos, int left, DirectedPosition from) : base(pos.X, pos.Y)
        {
            this.FromDirection = from;
            this.LeftMovePoint = left;
            this.IsSkipped = false;
        }
    }
}
