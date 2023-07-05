using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WindingTale.Core.Common;
using WindingTale.Core.Components;
using WindingTale.UI.Common;
using WindingTale.UI.Scenes.Game;

namespace WindingTale.UI.Activities
{
    public class ShowRangeActivity : ActivityBase
    {
        public List<FDPosition> Positions { get; private set; }

        public ShowRangeActivity(List<FDPosition> positions)
        {
            this.Positions = positions;
        }

        public override void Start(GameObject gameInterface)
        {
            Debug.Log("ShowRangeActivity: " + this.Positions.Count);

            GameObject mapIndicators = gameInterface.GetComponent<GameInterface>().MapIndicators;
            GameObject indicatorPrefab = Resources.Load<GameObject>("Others/BlockIndicator/BlockIndicatorPrefab");

            foreach(FDPosition position in this.Positions)
            {
                GameObject indicator = MonoBehaviour.Instantiate(indicatorPrefab, mapIndicators.transform);
                indicator.name = "BlockIndicator";
                indicator.transform.SetLocalPositionAndRotation(MapCoordinate.ConvertPosToVec3(position), Quaternion.identity);
                
                //// GameInterface.Instance.ApplyDefaultMaterial(indicator.transform.Find("default").gameObject);
            }


        }


        public override void Update(GameObject gameInterface)
        {
            this.HasFinished = true;
        }
    }
}