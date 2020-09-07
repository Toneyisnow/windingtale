using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WindingTale.Common;
using WindingTale.Core.Definitions;
using WindingTale.Core.ObjectModels;

namespace WindingTale.Core.Components.Algorithms
{

    public class MoveRangeQueueObject
    {
        public FDPosition Position
        {
            get; set;
        }

        public int LeftMovePoint
        {
            get; set;
        }

        public MoveRangeQueueObject(FDPosition pos, int left)
        {
            this.Position = pos;
            this.LeftMovePoint = left;
        }
    }

    public class MoveRangeFinder
    {
        private IGameAction gameAction;

        private GameField gameField = null;
        private FDCreature creature = null;

        private Queue<MoveRangeQueueObject> positionQueue = null;

        public MoveRangeFinder(IGameAction gameAction, FDCreature creature)
        {
            this.gameAction = gameAction;
            this.gameField = gameAction.GetField();
            this.creature = creature;
        }

        public FDMoveRange CalculateMoveRange()
        {
            FDPosition central = this.creature.Position;
            FDMoveRange range = new FDMoveRange(central);

            int movePoint = creature.Data.CalculatedMv;
            positionQueue = new Queue<MoveRangeQueueObject>();

            range.AddPosition(central, null);
            positionQueue.Enqueue(new MoveRangeQueueObject(central, movePoint));

            while (positionQueue.Count > 0)
            {
                MoveRangeQueueObject queueObject = positionQueue.Dequeue();
                WalkOnPosition(queueObject.Position, queueObject.LeftMovePoint, range);
            }

            return range;
        }

        private void WalkOnPosition(FDPosition position, int leftMovePoint, FDMoveRange range)
        {
            int moveCost = GetMoveCost(position, creature);
            if (moveCost == -1 || leftMovePoint < moveCost)
            {
                // Nothing to walk
                return;
            }

            // If this is ZOC, stop the move
            if (!position.AreSame(this.creature.Position) && HasAdjacentEnemy(position, creature.Faction))
            {
                return;
            }

            FDPosition[] directions = new FDPosition[4]
            {
                FDPosition.At(position.X - 1, position.Y),
                FDPosition.At(position.X, position.Y - 1),
                FDPosition.At(position.X + 1, position.Y),
                FDPosition.At(position.X, position.Y + 1)
            };

            int leftPoint = leftMovePoint - moveCost;
            foreach (FDPosition direction in directions)
            {
                if (direction.X < 0 || direction.X >= gameField.Width
                    || direction.Y < 0 || direction.Y >= gameField.Height)
                {
                    continue;
                }

                if (range.Contains(direction))
                {
                    continue;
                }

                // If already occupied by creature
                FDCreature existing = gameAction.GetCreatureAt(direction);
                if (existing != null)
                {
                    continue;
                }

                if (GetMoveCost(direction, creature) == -1)
                {
                    // Cannot land on target direction
                    continue;
                }

                range.AddPosition(direction, position);
                positionQueue.Enqueue(new MoveRangeQueueObject(direction, leftPoint));
            }
        }

        private int GetMoveCost(FDPosition position, FDCreature creature)
        {
            ShapeDefinition targetShape = gameField.GetShapeAt(position.X, position.Y);
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

        private bool HasAdjacentEnemy(FDPosition position, CreatureFaction faction)
        {
            FDPosition[] directions = new FDPosition[4]
            {
                FDPosition.At(position.X - 1, position.Y),
                FDPosition.At(position.X, position.Y - 1),
                FDPosition.At(position.X + 1, position.Y),
                FDPosition.At(position.X, position.Y + 1)
            };

            foreach (FDPosition direction in directions)
            {
                FDCreature c = gameAction.GetCreatureAt(direction);
                if (c == null)
                {
                    continue;
                }

                if ((faction == CreatureFaction.Friend || faction == CreatureFaction.Npc)
                    && c.Faction == CreatureFaction.Enemy)
                {
                    return true;
                }

                if ((c.Faction == CreatureFaction.Friend || c.Faction == CreatureFaction.Npc)
                    && faction == CreatureFaction.Enemy)
                {
                    return true;
                }
            }

            return false;
        }


        public FDMoveRange CalculateMoveRangeSample()
        {
            FDPosition central = this.creature.Position;
            FDMoveRange range = new FDMoveRange(central);

            for(int k = 1; k <= 5; k++)
            {
                for(int t= 0; t <= k; t++)
                {
                    int posX = central.X + t;
                    int posY = central.Y + (k - t);
                    range.AddPosition(FDPosition.At(posX, posY), central);

                    posX = central.X - t;
                    posY = central.Y + (k - t);
                    range.AddPosition(FDPosition.At(posX, posY), central);

                    posX = central.X + t;
                    posY = central.Y - (k - t);
                    range.AddPosition(FDPosition.At(posX, posY), central);

                    posX = central.X - t;
                    posY = central.Y - (k - t);
                    range.AddPosition(FDPosition.At(posX, posY), central);
                }
            }
            
            return range;
        }
    }
}