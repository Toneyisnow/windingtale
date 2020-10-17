using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WindingTale.Common;
using WindingTale.Core.Definitions;
using WindingTale.Core.ObjectModels;

namespace WindingTale.Core.Components.Algorithms
{
    public class DirectRangeFinder
    {
        private GameField gameField = null;
        private FDPosition central = null;

        private int outterLength = 0;
        private int innerLength = 0;

        public DirectRangeFinder(GameField gameField, FDPosition position, int outterLength, int innerLength = 0)
        {
            this.gameField = gameField;
            this.central = position;

            this.outterLength = outterLength;
            this.innerLength = innerLength;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public FDRange CalculateRange()
        {
            FDRange range = new FDRange(central);

            //// range.AddPosition(central);
            for (int k = this.innerLength; k <= this.outterLength; k++)
            {
                for (int t = 0; t <= k; t++)
                {
                    int posX = central.X + t;
                    int posY = central.Y + (k - t);
                    AddValidPosition(range, FDPosition.At(posX, posY));

                    posX = central.X - t;
                    posY = central.Y + (k - t);
                    AddValidPosition(range, FDPosition.At(posX, posY));

                    posX = central.X + t;
                    posY = central.Y - (k - t);
                    AddValidPosition(range, FDPosition.At(posX, posY));

                    posX = central.X - t;
                    posY = central.Y - (k - t);
                    AddValidPosition(range, FDPosition.At(posX, posY));
                }
            }

            return range;
        }

        public void AddValidPosition(FDRange range, FDPosition pos)
        {
            if (pos.X <= 0 || pos.X > gameField.Width || pos.Y <= 0 || pos.Y > gameField.Height)
            {
                return;
            }

            range.AddPosition(pos);
        }
    }
}