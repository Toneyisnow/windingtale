using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WindingTale.Core.Components.ActionStates;

namespace WindingTale.Core.Components
{
    /// <summary>
    /// 
    /// </summary>
    public class GameManager : IGameAction
    {
        private GameStateDispatcher dispatcher = null;

        
        public void StartGame(int stageId, GameStageRecord record)
        {

        }

        public void LoadGame(GameBattleRecord battleRecord)
        {

        }

        public GameBattleRecord SaveGame()
        {
            return null;
        }

        public GameStateDispatcher GetDispatcher()
        {
            return dispatcher;
        }


    }
}