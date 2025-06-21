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

        // void StartNewGame();

        // void LoadGame();

        void ContinueGame();

        void SaveGame();

        void OnQuit();

        void OnGameOver();

        void OnGameWin();

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