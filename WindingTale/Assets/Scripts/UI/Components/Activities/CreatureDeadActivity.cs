using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WindingTale.Core.ObjectModels;
using WindingTale.UI.MapObjects;

namespace WindingTale.UI.Components.Activities
{
    public class CreatureDeadActivity : ActivityBase
    {
        private List<int> creatureIds = null;


        public CreatureDeadActivity(List<int> creatureIds)
        {
            if (creatureIds == null)
            {
                throw new ArgumentNullException("creatures");
            }

            this.creatureIds = creatureIds;
        }

        public override void Start(IGameInterface gameInterface)
        {
            foreach (int creatureId in creatureIds)
            {
                UICreature creature = gameInterface.GetUICreature(creatureId);
                creature.SetAnimateState(UICreature.AnimateStates.Dying);
            }
        }

        public override void Update(IGameInterface gameInterface)
        {
            bool allGone = true;
            foreach (int creatureId in creatureIds)
            {
                UICreature creature = gameInterface.GetUICreature(creatureId);
                if (creature != null)
                {
                    allGone = false;
                }
            }

            if (allGone)
            {
                this.HasFinished = true;
            }
        }
    }
}
