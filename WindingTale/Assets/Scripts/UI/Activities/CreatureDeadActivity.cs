using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using WindingTale.Core.Common;
using WindingTale.UI.Scenes;
using WindingTale.UI.Scenes.Game;

namespace WindingTale.UI.Activities
{
    public class CreatureDeadActivity : ActivityBase
    {
        private List<int> creatureIds = null;

        private int counter = 0;

        public CreatureDeadActivity(List<int> creatureIds)
        {
            if (creatureIds == null)
            {
                throw new ArgumentNullException("creatures");
            }

            this.creatureIds = creatureIds;
        }

        public override void Start(GameObject gameInterface)
        {
            GameObject mapNode = gameInterface.GetComponent<GameFieldScene>().mapNode;

            foreach (int creatureId in creatureIds)
            {
                GameObject creature = mapNode.transform.Find(string.Format("creature_{0}", StringUtils.Digit3(creatureId))).gameObject;
                if (creature != null)
                {
                    GameObject.Destroy(creature);
                }
                ////UICreature creature = gameInterface.GetUICreature(creatureId);
                ////creature.SetAnimateState(UICreature.AnimateStates.Dying);
            }
        }



        public override void Update(GameObject gameInterface)
        {
            bool allGone = true;
            foreach (int creatureId in creatureIds)
            {
                ////UICreature creature = gameInterface.GetUICreature(creatureId);
                ////if (creature != null)
                {
                    allGone = false;
                }
            }

            if (counter ++ > 60)
            {
                this.HasFinished = true;
            }

            if (allGone)
            {
                this.HasFinished = true;
            }
        }

    }
}
