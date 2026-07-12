using WindingTale.Core.Common;

namespace WindingTale.Scenes.GameFieldScene.Activities
{
    /// <summary>
    /// Slides the map cursor (and the follow camera) to a tile, finishing once the
    /// slide has settled. Reuses the same cursor/camera slide as conversations.
    /// </summary>
    public class SlideCursorActivity : ActivityBase
    {
        private readonly FDPosition target;

        public SlideCursorActivity(FDPosition target)
        {
            this.target = target;
        }

        public SlideCursorActivity(int x, int y) : this(FDPosition.At(x, y))
        {
        }

        public override void Start(GameMain gameMain)
        {
            if (target != null)
            {
                // Bottom => centered framing (no top dialog to clear).
                gameMain.gameMap.SlideCursorTo(target, GameCanvas.DialogPosition.Bottom);
            }

            this.HasFinished = false;
        }

        public override void Update(GameMain gameMain)
        {
            if (this.HasFinished)
            {
                return;
            }

            if (!gameMain.gameMap.IsSlideBusy)
            {
                this.HasFinished = true;
            }
        }
    }
}
