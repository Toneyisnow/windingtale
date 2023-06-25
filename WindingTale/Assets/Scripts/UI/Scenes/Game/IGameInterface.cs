

using WindingTale.Core.Algorithms;
using WindingTale.Core.Common;
using WindingTale.Core.Objects;

namespace WindingTale.UI.Scenes.Game
{
    public interface IGameInterface
    {
        /////public UICreature GetUICreature(int creatureId);
        ///

        public void AddCreatureUI(FDCreature creature, FDPosition position);

        public void MoveCreatureUI(FDCreature creature, FDMovePath path);
    }
}