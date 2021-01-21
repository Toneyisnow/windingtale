using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WindingTale.Core.Components.Algorithms
{
    public class FightInformation
    {
        public AttackInformation Attack1
        {
            get; private set;
        }

        public AttackInformation Attack2
        {
            get; private set;
        }

        public AttackInformation Back1
        {
            get; private set;
        }

        public AttackInformation Back2
        {
            get; private set;
        }

        public FightInformation(AttackInformation attack1, AttackInformation attack2, AttackInformation back1, AttackInformation back2)
        {
            this.Attack1 = attack1;
            this.Attack2 = attack2;
            this.Back1 = back1;
            this.Back2 = back2;

        }


    }
}
