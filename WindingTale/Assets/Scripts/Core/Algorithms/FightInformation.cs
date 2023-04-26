using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WindingTale.Core.Components.Algorithms
{
    public class FightInformation
    {
        public BattleResults Attack1
        {
            get; private set;
        }

        public BattleResults Attack2
        {
            get; private set;
        }

        public BattleResults Back1
        {
            get; private set;
        }

        public BattleResults Back2
        {
            get; private set;
        }

        public FightInformation(BattleResults attack1, BattleResults attack2, BattleResults back1, BattleResults back2)
        {
            this.Attack1 = attack1;
            this.Attack2 = attack2;
            this.Back1 = back1;
            this.Back2 = back2;

        }


    }
}
