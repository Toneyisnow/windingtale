using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;
using WindingTale.Core.Common;

namespace WindingTale.Core.Algorithms
{
    
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
            DirectedPosition directedPosition = GetPosition(position);
            return directedPosition != null && !directedPosition.IsSkipped;
        }

        public List<FDPosition> ToList()
        {
            return range.Where(pos => !pos.IsSkipped).ToList<FDPosition>();
        }

    }
}