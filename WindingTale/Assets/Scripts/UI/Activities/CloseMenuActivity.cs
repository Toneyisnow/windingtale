using UnityEngine;
using WindingTale.UI.Scenes.Game;

namespace WindingTale.UI.Activities
{
    public class CloseMenuActivity : ActivityBase
    {

        public override void Start(GameObject gameInterface)
        {
            Transform gameIndicatorRoot = gameInterface.GetComponent<GameInterface>().MapIndicators.transform;
            Transform menu = gameIndicatorRoot.Find("Menu");
            if (menu != null)
            {
                MonoBehaviour.Destroy(menu.gameObject);
            }
        }

        public override void Update(GameObject gameInterface)
        {
            this.HasFinished = true;
        }

    }
}