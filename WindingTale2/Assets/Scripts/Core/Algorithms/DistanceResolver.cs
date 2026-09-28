using System;
using System.Collections.Generic;
using WindingTale.Core.Common;
using WindingTale.Core.Definitions;
using WindingTale.Core.Map;
using WindingTale.Core.Objects;

namespace WindingTale.Core.Algorithms
{
    /// <summary>
    /// Walking distances across the field from one tile, used by the AI to judge which
    /// tile of its move range gets it closest to where it is heading, and which opponent
    /// is nearest on foot.
    ///
    /// A distance is what the walk actually costs the creature: steps go in the four
    /// directions a creature moves in, each costing the move points of the tile stepped
    /// onto, and a tile the creature cannot stand on (Blocked for walkers -- mountains,
    /// rocks, the dense trees -- and Gap for everyone) is never passed through. An earlier
    /// version counted every tile but a Gap as open ground and stepped diagonally too: it
    /// saw a way straight through a mountain, and through the corner between two trees
    /// that touch diagonally, so creatures marched to the foot of the rock -- or into the
    /// corner -- and dithered there turn after turn.
    ///
    /// Creatures standing on the field are not obstacles here; they move, and the move
    /// range (MoveRangeFinder) already keeps a creature from walking through its enemies.
    /// </summary>
    public class DistanceResolver
    {
        /// <summary>What GetDistanceTo reports for a tile the walk cannot reach (or never got to).</summary>
        public const float Unreachable = 999;

        private readonly FDField field = null;

        /// <summary>Whose feet the distances are measured for; null measures for plain ground movers.</summary>
        private readonly FDCreature creature = null;

        private float[,] distances = null;

        public DistanceResolver(FDField field) : this(field, null)
        {
        }

        public DistanceResolver(FDField field, FDCreature creature)
        {
            this.field = field;
            this.creature = creature;
        }

        /// <summary>
        /// Resolves the distance of every tile from originPos, cheapest first, stopping once
        /// terminatePos is settled: every tile no farther than it then has its final
        /// distance, and anything past it may still read as Unreachable.
        /// </summary>
        public void ResolveDistanceFrom(FDPosition originPos, FDPosition terminatePos)
        {
            int width = field.Width;
            int height = field.Height;

            distances = new float[width + 1, height + 1];
            for (int x = 0; x <= width; x++)
            {
                for (int y = 0; y <= height; y++)
                {
                    distances[x, y] = Unreachable;
                }
            }

            if (!IsInField(originPos.X, originPos.Y))
            {
                return;
            }

            bool[,] settled = new bool[width + 1, height + 1];
            SortedSet<(float distance, int x, int y)> open = new SortedSet<(float, int, int)>();

            distances[originPos.X, originPos.Y] = 0;
            open.Add((0, originPos.X, originPos.Y));

            while (open.Count > 0)
            {
                var current = open.Min;
                open.Remove(current);

                if (settled[current.x, current.y])
                {
                    continue;
                }
                settled[current.x, current.y] = true;

                if (terminatePos != null && current.x == terminatePos.X && current.y == terminatePos.Y)
                {
                    break;
                }

                Relax(open, settled, current.distance, current.x - 1, current.y);
                Relax(open, settled, current.distance, current.x + 1, current.y);
                Relax(open, settled, current.distance, current.x, current.y - 1);
                Relax(open, settled, current.distance, current.x, current.y + 1);
            }
        }

        public float GetDistanceTo(FDPosition position)
        {
            if (distances == null)
            {
                return 0;
            }

            if (!IsInField(position.X, position.Y))
            {
                return Unreachable;
            }

            return distances[position.X, position.Y];
        }

        private void Relax(SortedSet<(float, int, int)> open, bool[,] settled, float fromDistance, int x, int y)
        {
            if (!IsInField(x, y) || settled[x, y])
            {
                return;
            }

            int cost = GetStepCost(x, y);
            if (cost < 0)
            {
                return;
            }

            float distance = fromDistance + cost;
            if (distance < distances[x, y])
            {
                // A stale entry for the old distance may stay in the set; it is skipped
                // as already settled when it comes up.
                distances[x, y] = distance;
                open.Add((distance, x, y));
            }
        }

        /// <summary>The move points a step onto (x, y) costs, or -1 when it cannot be stood on.</summary>
        private int GetStepCost(int x, int y)
        {
            ShapeDefinition shape = field.GetShapeAt(FDPosition.At(x, y));
            if (shape == null)
            {
                return -1;
            }

            if (creature == null)
            {
                return shape.MoveCost;
            }

            return MoveRangeFinder.GetMoveCost(shape, creature);
        }

        private bool IsInField(int x, int y)
        {
            return x >= 1 && x <= field.Width && y >= 1 && y <= field.Height;
        }
    }
}
