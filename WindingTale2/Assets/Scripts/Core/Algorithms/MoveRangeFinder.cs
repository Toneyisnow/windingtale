using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using WindingTale.Core.Common;
using WindingTale.Core.Definitions;
using WindingTale.Core.Map;
using WindingTale.Core.Objects;

namespace WindingTale.Core.Algorithms
{

    public class MoveRangeFinder
    {
        private FDMap gameMap = null;

        private FDCreature creature = null;


        public MoveRangeFinder(FDMap gameMap, FDCreature creature)
        {
            this.gameMap = gameMap;
            this.creature = creature;
        }

        public FDMoveRange CalculateMoveRange()
        {
            int movePoint = creature.CalculatedMv;
            DirectedPosition central = new DirectedPosition(creature.Position, movePoint, null);
            FDMoveRange range = new FDMoveRange(central);

            Queue<DirectedPosition> positionQueue = new Queue<DirectedPosition>();

            positionQueue.Enqueue(central);

            while (positionQueue.Count > 0)
            {
                WalkOnPosition(positionQueue, range);
            }

            // Remove the friend creatures from range, except the creature itself
            List<FDCreature> creatureInRange = gameMap.GetCreaturesInRange(range.ToList(), creature.Faction);
            if (creature.Faction == CreatureFaction.Friend)
            {
                creatureInRange.AddRange(gameMap.GetCreaturesInRange(range.ToList(), CreatureFaction.Npc));
            }
            else if (creature.Faction == CreatureFaction.Npc)
            {
                creatureInRange.AddRange(gameMap.GetCreaturesInRange(range.ToList(), CreatureFaction.Friend));
            }

            creatureInRange.ForEach(c =>
            {
                if (c.Id != creature.Id)
                {
                    DirectedPosition directed = range.GetPosition(c.Position);
                    if (directed != null)
                    {
                        directed.IsSkipped = true;
                    }
                }
            });

            return range;
        }

        private void WalkOnPosition(Queue<DirectedPosition> positionQueue, FDMoveRange range)
        {
            DirectedPosition queuePosition = positionQueue.Dequeue();
            
            int moveCost = GetMoveCost(queuePosition, creature);
            if (moveCost == -1 || queuePosition.LeftMovePoint < moveCost)
            {
                // Nothing to walk
                return;
            }

            // If this is ZOC, stop the move
            if (!queuePosition.AreSame(creature.Position) && HasAdjacentEnemy(queuePosition))
            {
                return;
            }

            int leftPoint = queuePosition.LeftMovePoint - moveCost;
            foreach (FDPosition position in queuePosition.GetAdjacentPositions())
            {
                if (position.X <= 0 || position.X > gameMap.Field.Width
                    || position.Y <= 0 || position.Y > gameMap.Field.Height)
                {
                    continue;
                }

                if (range.ToList().Find(pos => pos.AreSame(position)) != null)
                {
                    continue;
                }

                // If already occupied by creature
                FDCreature existing = gameMap.GetCreatureAt(position);
                if (existing != null && existing.IsOppositeFaction(creature))
                {
                    continue;
                }

                if (GetMoveCost(position, creature) == -1)
                {
                    // Cannot land on target direction
                    continue;
                }

                DirectedPosition directed = new DirectedPosition(position, leftPoint, queuePosition);

                range.Add(directed);
                positionQueue.Enqueue(directed);
            }
        }

        private int GetMoveCost(FDPosition position, FDCreature creature)
        {
            ShapeDefinition targetShape = gameMap.Field.GetShapeAt(position);
            if (targetShape == null)
            {
                return -1;
            }

            int moveCost = 0;
            if (creature.Definition.CanFly())
            {
                if (targetShape.CanFly)
                {
                    moveCost = 1;
                }
                else
                {
                    moveCost = -1;
                }
            }
            else if (creature.Definition.IsKnight())
            {
                moveCost = targetShape.MoveCostForKnight;
            }
            else
            {
                moveCost = targetShape.MoveCost;
            }

            return moveCost;
        }

        private bool HasAdjacentEnemy(FDPosition position)
        {
            foreach (FDPosition direction in position.GetAdjacentPositions())
            {
                FDCreature c = gameMap.GetCreatureAt(direction);
                if (c == null)
                {
                    continue;
                }

                if (this.creature.IsOppositeFaction(c))
                {
                    return true;
                }
            }

            return false;
        }
    }
}