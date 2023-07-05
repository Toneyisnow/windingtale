using UnityEngine;
using WindingTale.Core.Common;
using WindingTale.UI.Common;
using WindingTale.UI.Scenes.Game;

namespace WindingTale.UI.Activities
{
    public class ClearRangeActivity : ActivityBase
    {
        public override void Start(GameObject gameInterface)
        {
            Debug.Log("ClearRangeActivity." );

            GameObject mapIndicators = gameInterface.GetComponent<GameInterface>().MapIndicators;

            foreach (Transform child in mapIndicators.transform)
            {
                if (child.name == "BlockIndicator")
                {
                    GameObject.Destroy(child.gameObject);
                }
            }

        }

    }
}