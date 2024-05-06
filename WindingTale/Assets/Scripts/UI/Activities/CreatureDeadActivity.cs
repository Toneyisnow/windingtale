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
        private GameObject mapNode = null;

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
            mapNode = gameInterface.GetComponent<GameFieldScene>().mapNode;
            foreach (int creatureId in creatureIds)
            {
                GameObject creature = mapNode.transform.Find(string.Format("creature_{0}", StringUtils.Digit3(creatureId))).gameObject;
                if (creature != null)
                {
                    creature.AddComponent<CreatureDying>();
                }
            }
        }


        public override void Update(GameObject gameInterface)
        {
            bool allGone = true;
            if (counter < 10)
            {
                counter++;
                return;
            }

            foreach (int creatureId in creatureIds)
            {
                GameObject creature = mapNode.transform.Find(string.Format("creature_{0}", StringUtils.Digit3(creatureId))).gameObject;
                if (creature != null && creature.GetComponent<CreatureDying>() != null)
                {
                    allGone = false;
                    break;
                }

                GameObject.Destroy(creature);
            }

            if (allGone)
            {
                this.HasFinished = true;
            }
        }
    }
}
