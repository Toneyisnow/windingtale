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

        public bool AreSame(FDPosition other)
        {
            return this.X == other.X && this.Y == other.Y;
        }

        public FDPosition(int x, int y)
        {
            this.X = x;
            this.Y = y;
        }

        public override int GetHashCode()
        {
            return X * 100 + Y;
        }

        public static FDPosition At(int x, int y)
        {
            return new FDPosition(x, y);
        }

        public static FDPosition Invalid()
        {
            return new FDPosition(-1, -1);
        }
    }

}