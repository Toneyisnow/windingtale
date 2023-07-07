using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;
using WindingTale.Core.Common;

namespace WindingTale.Core.Algorithms
{

    public class DirectedPosition : FDPosition
    {
        public DirectedPosition FromDirection { get; private set; }


        public int LeftMovePoint
        {
            get; set;
        }

        public bool IsSkipped { get; set; }

        public DirectedPosition(FDPosition pos, int left, DirectedPosition from) : base(pos.X, pos.Y)
        {
            this.FromDirection = from;
            this.LeftMovePoint = left;
            this.IsSkipped = false;
        }
    }
    public class FDMoveRange
    {
        private HashSet<DirectedPosition> range = null;

        public FDMoveRange(DirectedPosition central)
        {
            range = new HashSet<DirectedPosition> { central };
        }

        public void Add(DirectedPosition pos)
        {
            range.Add(pos);
        }

        public DirectedPosition GetPosition(FDPosition position)
        {
            return range.FirstOrDefault(pos => pos.AreSame(position));
        }

        public bool Contains(FDPosition position) 
        {
            return GetPosition(position) != null;
        }

        public List<FDPosition> ToList()
        {
            return range.Where(pos => !pos.IsSkipped).ToList<FDPosition>();
        }

    }
}