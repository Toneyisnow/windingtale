using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WindingTale.Core.Common;
using WindingTale.Core.Map;

namespace WindingTale.Scenes.GameFieldScene.ActionStates
{
    public abstract class IActionState
    {
        protected GameMain gameMain;

        protected PlayerInterface playerInterface;

        protected FDMap fdMap
        {
            get
            {
                return this.gameMain.gameMap.Map;
            }
        }


        public IActionState(GameMain game)
        {
            this.gameMain = game;
            this.playerInterface = PlayerInterface.getDefault();
        }

        public abstract void onEnter();

        public abstract void onExit();

        public abstract IActionState onSelectedPosition(FDPosition position);

        /// <summary>
        ///  Not used in current version
        /// </summary>
        /// <returns></returns>
        public abstract IActionState onUserCancelled();
    }
}