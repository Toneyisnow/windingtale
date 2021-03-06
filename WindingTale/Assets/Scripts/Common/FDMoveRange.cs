﻿using System;
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

        private List<FDPosition> skipPositions = null;

        public FDMoveRange(FDPosition central) : base(central)
        {
            directionedScope = new Dictionary<FDPosition, FDPosition>();
            directionedScope[this.CentralPosition] = null;

            skipPositions = new List<FDPosition>();
        }

        public override List<FDPosition> Positions
        {
            get
            {
                this.positions = directionedScope.Keys.ToList();

                for(int pos = this.positions.Count - 1; pos >= 0; pos--)
                {
                    if (skipPositions.Contains(this.positions[pos]))
                    {
                        this.positions.RemoveAt(pos);
                    }
                }

                return this.positions;
            }
        }

        public void AddPosition(FDPosition position, FDPosition lastPosition)
        {
            directionedScope[position] = lastPosition;
        }

        public void AddSkipPosition(FDPosition position)
        {
            if (!skipPositions.Contains(position))
            {
                skipPositions.Add(position);
            }
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
            if (!directionedScope.ContainsKey(position) || skipPositions.Contains(position))
            {
                return null;
            }

            FDMovePath path = new FDMovePath();
            FDPosition current = position;
            FDPosition next = directionedScope[current];
            int lastDirection = -1;
            while(next != null)
            {
                int direction = next.IsNextToDirection(current);
                if (lastDirection < 0 || direction != lastDirection)
                {
                    path.InsertToHead(current);
                }

                lastDirection = direction;
                current = next;
                next = directionedScope[current];
            }

            return path;
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
