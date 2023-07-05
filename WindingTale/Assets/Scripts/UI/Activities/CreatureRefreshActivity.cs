using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

        public override void Start(GameObject gameInterface)
        {
            Transform mapObjects = gameInterface.GetComponent<GameInterface>().MapObjects.transform;
            foreach (int creatureId in creatureIds)
            {
                GameObject creatureObj = mapObjects.Find(string.Format("creature_{0}",  StringUtils.Digit3(creatureId))).gameObject;
                if (creatureObj != null)
                {
                    Creature creatureData = creatureObj.GetComponent<Creature>();
                    creatureObj.transform.SetPositionAndRotation(MapCoordinate.ConvertPosToVec3(creatureData.creature.Position), Quaternion.identity);

                }
            }
        }


    }
}
