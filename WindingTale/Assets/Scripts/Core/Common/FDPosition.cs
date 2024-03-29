using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace WindingTale.Core.Common
{
    public class FDPosition : IEquatable<FDPosition>
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

        public bool IsNextTo(FDPosition other)
        {
            return (other.X == this.X + 1 && other.Y == this.Y)
                || (other.X == this.X - 1 && other.Y == this.Y)
                || (other.X == this.X && other.Y == this.Y + 1)
                || (other.X == this.X && other.Y == this.Y - 1);
        }

        public int IsNextToDirection(FDPosition other)
        {
            if (other.X == this.X + 1 && other.Y == this.Y)
            {
                return 1;
            }

            if (other.X == this.X - 1 && other.Y == this.Y)
            {
                return 2;
            }

            if (other.X == this.X && other.Y == this.Y + 1)
            {
                return 3;
            }

            if (other.X == this.X && other.Y == this.Y - 1)
            {
                return 4;
            }

            return -1;
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

        public override string ToString()
        {
            return string.Format("[X: {0}, Y: {1}]", X, Y);
        }

        public static FDPosition At(int x, int y)
        {
            return new FDPosition(x, y);
        }

        public static FDPosition Invalid()
        {
            return new FDPosition(-1, -1);
        }

        public bool Equals(FDPosition other)
        {
            return this.AreSame(other);
        }

        public FDPosition[] GetAdjacentPositions()
        {
            return new FDPosition[4]
            {
                FDPosition.At(this.X - 1, this.Y),
                FDPosition.At(this.X, this.Y - 1),
                FDPosition.At(this.X + 1, this.Y),
                FDPosition.At(this.X, this.Y + 1)
            };
        }
    }

}