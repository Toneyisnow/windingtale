using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WindingTale.Common;
using WindingTale.Core.Definitions;

namespace WindingTale.Core.Components.Algorithms
{
    public class DistanceResolver
    {
        private GameField field = null;

        private FDPosition originalPosition = null;

        private Dictionary<FDPosition, float> distanceDict = null;

        private Queue<FDPosition> positionQueue = null;


        public DistanceResolver(GameField field)
        {
            this.field = field;

        }

        public void ResolveDistanceFrom(FDPosition originPos, FDPosition terminatePos)
        {
            this.originalPosition = originPos;
            this.distanceDict = new Dictionary<FDPosition, float>();
            this.positionQueue = new Queue<FDPosition>();

            this.SetKey(originPos, 0);

            while(positionQueue.Count > 0)
            {
                FDPosition pos = positionQueue.Dequeue();
                this.Walk(pos);

                if (pos.AreSame(terminatePos))
                {
                    break;
                }
            }

        }

        public float GetDistanceTo(FDPosition position)
        {
            if (distanceDict == null)
            {
                return 0;
            }

            if (distanceDict.ContainsKey(position))
            {
                return distanceDict[position];
            }
            else
            {
                // Distance Max
                return 999;
            }
        }

        private void Walk(FDPosition position)
        {
            if (!distanceDict.ContainsKey(position))
            {
                return;
            }

            float val = distanceDict[position];

            this.SetKey(FDPosition.At(position.X - 1, position.Y), val + 1);
            this.SetKey(FDPosition.At(position.X, position.Y - 1), val + 1);
            this.SetKey(FDPosition.At(position.X + 1, position.Y), val + 1);
            this.SetKey(FDPosition.At(position.X, position.Y + 1), val + 1);

            this.SetKey(FDPosition.At(position.X - 1, position.Y - 1), val + 1.4f);
            this.SetKey(FDPosition.At(position.X + 1, position.Y - 1), val + 1.4f);
            this.SetKey(FDPosition.At(position.X + 1, position.Y + 1), val + 1.4f);
            this.SetKey(FDPosition.At(position.X - 1, position.Y + 1), val + 1.4f);

        }

        private void SetKey(FDPosition position, float value)
        {
            if(position.X <= 0 || position.X > field.Width || position.Y <= 0 || position.Y > field.Height)
            {
                return;
            }

            if (distanceDict.ContainsKey(position))
            {
                float lastValue = distanceDict[position];
                if (lastValue <= value)
                {
                    return;
                }
            }

            ShapeDefinition shape = field.GetShapeAt(position);

            if (shape.Type != ShapeType.Gap)
            {
                distanceDict[position] = value;
                positionQueue.Enqueue(position);
            }
            else
            {
                distanceDict[position] = 999;
            }
        }



    }
}
