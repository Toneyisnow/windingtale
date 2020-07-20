using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FigAniTool2.Common
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

        public static bool AreSame(FDPosition pos1, FDPosition pos2)
        {
            return pos1.X == pos2.X && pos1.Y == pos2.Y;
        }

        public override bool Equals(object obj)
        {
            FDPosition another = (FDPosition)obj;
            return X == another.X && Y == another.Y;
        }

        public override int GetHashCode()
        {
            return this.X * 100 + this.Y;
        }
    }
}
