using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WindingTale.Core.Components.Algorithms;
using WindingTale.Core.ObjectModels;

namespace WindingTale.Core.Components.Packs
{
    public class BattleFightPack : PackBase
    {
        public FDCreature Subject
        {
            get; private set;
        }

        public FDCreature Target
        {
            get; private set;
        }

        public FightInformation FightInformation
        {
            get; private set;
        }


        public BattleFightPack(FDCreature subject, FDCreature target, FightInformation fightInfo) : base()
        {
            this.Type = PackType.BattleFight;

            this.Subject = subject;
            this.Target = target;
            this.FightInformation = fightInfo;
        }



    }
}
