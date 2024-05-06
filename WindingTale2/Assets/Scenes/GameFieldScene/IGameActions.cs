using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WindingTale.Core.Common;
using WindingTale.Core.Objects;

namespace WindingTale.Scenes.GameFieldScene
{

    public interface IGameActions
    {
        #region Game Cycles

        void startNewGame();

        void loadGame();

        void continueGame();

        void saveGame();

        void onQuit();

        void onGameOver();

        void onGameWin();

        #endregion

        #region Creature Related Operation

        void creatureMove(Creature creature, FDPosition pos);

        void creatureAttack(Creature creature, Creature target);

        void creatureMagic(Creature creature, FDPosition pos, int magicId);

        void creatureRest(Creature creature);

        void endTurnForAll();

        #endregion
    }
}