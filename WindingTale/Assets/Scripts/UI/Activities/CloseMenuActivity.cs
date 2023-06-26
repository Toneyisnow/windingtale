using UnityEngine;
using WindingTale.UI.Scenes.Game;

namespace WindingTale.UI.Activities
{
    public class CloseMenuActivity : ActivityBase
    {

        public override void Start(GameObject gameInterface)
        {
            throw new System.NotImplementedException();
        }

        public override void Update(GameObject gameInterface)
        {
            this.HasFinished = true;
        }

    }
}