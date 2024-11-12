using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WindingTale.Core.Algorithms;
using WindingTale.Core.Common;
using WindingTale.Core.Objects;
using WindingTale.MapObjects.CreatureIcon;

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

        void creatureMoveAsync(FDCreature creature, FDMovePath path);

        void creatureAttackAsync(FDCreature creature, FDCreature target);

        void creatureMagic(FDCreature creature, FDPosition pos, int magicId);

        void creatureRest(FDCreature creature);

        void endTurnForAll();

        #endregion
    }
}