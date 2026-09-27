using WindingTale.Core.Algorithms;
using WindingTale.Core.Common;
using WindingTale.Core.Objects;
using WindingTale.Scenes.GameFieldScene;

namespace WindingTale.AI.Delegates
{
    /// <summary>
    /// Lends the AI's walking rules to the party's march (see GameMain.StartMarch): a friend
    /// on the march heads for the marching point exactly the way an enemy heads for a target
    /// with nobody in reach -- as far along as its move points allow, to the reachable tile
    /// nearest the point by walking distance. The march drives the walk itself (see
    /// MarchMoveActivity), so this only answers where the creature goes.
    /// </summary>
    public class AIMarchDelegate : AIDelegate
    {
        public AIMarchDelegate(GameMain gameMain, FDCreature creature) : base(gameMain, creature)
        {
        }

        /// <summary>A march never leaves a creature half-acted: whatever asks it to act just ends its turn.</summary>
        public override void TakeAction()
        {
            this.EndTurn();
        }

        /// <summary>The path this turn's march takes towards the point; null when there is nowhere to go.</summary>
        public FDMovePath PlanMarch(FDPosition marchPoint)
        {
            return this.DecidePositionAndPath(marchPoint);
        }
    }
}
