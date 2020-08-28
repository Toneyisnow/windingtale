using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace WindingTale.Common
{
    /// <summary>
    /// This is the data structure for the creature moving scopes
    /// </summary>
    public class FDMoveRange : FDRange
    {
        private Dictionary<FDPosition, FDPosition> directionedScope = null;


        public FDMoveRange(FDPosition central) : base(central)
        {
            directionedScope = new Dictionary<FDPosition, FDPosition>();
            directionedScope[this.CentralPosition] = null;
        }

        public override List<FDPosition> Positions
        {
            get
            {
                if(this.positions == null)
                {
                    this.positions = directionedScope.Keys.ToList();
                }

                return this.positions;
            }
        }

        public void AddPosition(FDPosition position, FDPosition lastPosition)
        {
            directionedScope[position] = lastPosition;
        }

        public override void AddPosition(FDPosition position)
        {
            throw new NotImplementedException("Should not call AddPosition(arg1) in FDMoveRange, use the one with two arguments.");
        }
        /// <summary>
        /// Generate the moving path for that position
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        public FDMovePath GetPath(FDPosition position)
        {
            return FDMovePath.Create(position);
        }

        public override bool Contains(FDPosition position)
        {
            if (directionedScope == null)
            {
                return false;
            }

            return directionedScope.ContainsKey(position);
        }

    }
}
