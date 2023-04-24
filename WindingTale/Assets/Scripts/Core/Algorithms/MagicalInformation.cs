using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WindingTale.Core.Components.Algorithms
{
    public class MagicalInformation
    {
        public List<AttackInformation> Informations
        {
            get; private set;
        }

        public MagicalInformation()
        {
            this.Informations = new List<AttackInformation>();
        }

        public void AddInformation(AttackInformation info)
        {
            this.Informations.Add(info);
        }

    }
}
