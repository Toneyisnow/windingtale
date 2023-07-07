using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using WindingTale.Core.Common;
using WindingTale.Core.Objects;
using WindingTale.UI.Common;
using WindingTale.UI.Scenes.Game;
using static UnityEditor.PlayerSettings;

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

        public CreatureRefreshActivity(int creatureId)
        {
            this.creatureIds = new List<int>() { creatureId };
        }

        public override void Start(GameObject gameInterface)
        {
            Transform mapObjects = gameInterface.GetComponent<GameInterface>().MapObjects.transform;
            foreach (int creatureId in creatureIds)
            {
                GameObject creatureObj = mapObjects.Find(string.Format("creature_{0}",  StringUtils.Digit3(creatureId))).gameObject;
                if (creatureObj != null)
                {
                    Creature creature = creatureObj.GetComponent<Creature>();
                    creatureObj.transform.SetPositionAndRotation(MapCoordinate.ConvertPosToVec3(creature.creature.Position), Quaternion.identity);

                    creature.SetGreyout(creature.creature.HasActioned);

                }
            }
        }


    }
}
