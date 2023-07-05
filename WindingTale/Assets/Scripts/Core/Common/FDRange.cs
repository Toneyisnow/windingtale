using System.Collections.Generic;
using System.Linq;

namespace WindingTale.Core.Common
{
    public class FDRange
    {
        private HashSet<FDPosition> range = null;
        
        public FDRange(FDPosition initial)
        {
            range = new HashSet<FDPosition> { initial };
        }

        public FDRange()
        {
            range = new HashSet<FDPosition> { };
        }


        public void Add(FDPosition pos)
        {
            range.Add(pos);
        }

        public bool Contains(FDPosition position)
        {
            return this.ToList().Find( pos => pos.AreSame(position)) != null;
        }

        public List<FDPosition> ToList()
        {
            return range.ToList();
        }

    }
}