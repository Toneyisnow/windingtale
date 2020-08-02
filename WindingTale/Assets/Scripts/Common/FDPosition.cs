using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace WindingTale.Common
{
    public class FDPosition
    {
        public int X
        {
            get; private set;
        }

        public int Y
        {
            get; private set;
        }

        public FDPosition(int x, int y)
        {
            this.X = x;
            this.Y = y;
        }

        public static FDPosition At(int x, int y)
        {
            return new FDPosition(x, y);
        }

    }

}