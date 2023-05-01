using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using WindingTale.Common;

namespace WindingTale.Core.Algorithms
{
    /// <summary>
    /// This is the data structure for the creature moving scopes
    /// </summary>
    public class MovePathFinder
    {
        private FDMoveRange moveRange = null;

        public MovePathFinder(FDMoveRange moveRange)
        {
            this.moveRange = moveRange;
        }
        

        /// <summary>
        /// Generate the moving path for that position
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        public FDMovePath GetPath(FDPosition position)
        {
            DirectedPosition directedPosition = moveRange.GetPosition(position);
            if (directedPosition == null || directedPosition.IsSkipped)
            {
                return null;
            }

            FDMovePath path = new FDMovePath();
            DirectedPosition current = directedPosition;
            DirectedPosition next = current.FromDirection;
            int lastDirection = -1;
            while (next != null)
            {
                int direction = next.IsNextToDirection(current);
                if (lastDirection < 0 || direction != lastDirection)
                {
                    path.InsertToHead(current);
                }

                lastDirection = direction;
                current = next;
                next = current.FromDirection;
            }

            return path;
        }
    }
}
