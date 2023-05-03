using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WindingTale.UI.MapObjects;
using WindingTale.UI.Scenes.Game;

namespace WindingTale.UI.Activities
{
    /// <summary>
    /// Refresh the creature on UI with latest data
    /// </summary>
    public class CreatureRefreshActivity : ActivityBase
    {
        private List<int> creatureIds = null;


        public CreatureRefreshActivity(List<int> creatureIds)
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
            }
        }

        public override void Update(IGameInterface gameInterface)
        {
            this.HasFinished = true;
        }

    }
}
