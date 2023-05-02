using System.Collections.Generic;
using System.Linq;

namespace WindingTale.Core.Common
{
    public class FDRange
    {
        private HashSet<FDPosition> range = null;

        public FDPosition central = null;

        public FDRange(FDPosition central)
        {
            range = new HashSet<FDPosition> { central };
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