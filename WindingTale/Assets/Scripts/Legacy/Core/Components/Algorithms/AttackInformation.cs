using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WindingTale.Core.Components.Algorithms
{
    public class BattleResults
    {
        public int HpBefore
        {
            get; private set;
        }

        public int HpAfter
        {
            get; private set;
        }

        public bool IsCritical
        {
            get; private set;
        }

        public BattleResults(int before, int after, bool critical)
        {
            this.HpBefore = before;
            this.HpAfter = after;
            this.IsCritical = critical;
        }

    }
}
