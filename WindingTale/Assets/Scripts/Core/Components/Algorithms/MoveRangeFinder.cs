using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WindingTale.Common;
using WindingTale.Core.ObjectModels;

namespace WindingTale.Core.Components.Algorithms
{
    public class MoveRangeFinder
    {
        private IGameAction gameAction;

        private FDCreature creature = null;

        public MoveRangeFinder(IGameAction gameAction, FDCreature creature)
        {
            this.gameAction = gameAction;
            this.creature = creature;
        }

        public FDMoveRange CalculateMoveRange()
        {
            FDPosition central = this.creature.Position;
            FDMoveRange range = new FDMoveRange(central);

            for(int k = 1; k < 5; k++)
            {
                for(int t= 0; t < k; t++)
                {
                    int posX = central.X + t;
                    int posY = central.Y + (k - t);
                    range.AddPosition(FDPosition.At(posX, posY));

                    posX = central.X - t;
                    posY = central.Y + (k - t);
                    range.AddPosition(FDPosition.At(posX, posY));

                    posX = central.X + t;
                    posY = central.Y - (k - t);
                    range.AddPosition(FDPosition.At(posX, posY));

                    posX = central.X - t;
                    posY = central.Y - (k - t);
                    range.AddPosition(FDPosition.At(posX, posY));
                }
            }
            
            return range;
        }
    }
}