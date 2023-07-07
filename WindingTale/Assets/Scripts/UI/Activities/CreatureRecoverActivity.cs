using UnityEngine;
using WindingTale.Core.Objects;
using WindingTale.UI.Scenes.Game;

namespace WindingTale.UI.Activities
{
    public class CreatureRecoverActivity : ActivityBase
    {
        private FDCreature creature = null;

        public CreatureRecoverActivity(FDCreature creature)
        {
            this.creature = creature;
        }


        public override void Start(GameObject gameInterface)
        {
            Debug.Log("CreatureRecoverActivity started.");
        }
    }
}