using WindingTale.UI.Scenes.Game;

namespace WindingTale.UI.Activities
{
    public class CloseMenuActivity : ActivityBase
    {

        public override void Start(IGameInterface gameInterface)
        {
            throw new System.NotImplementedException();
        }

        public override void Update(IGameInterface gameInterface)
        {
            this.HasFinished = true;
        }

    }
}