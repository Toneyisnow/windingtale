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

        protected FDMap map;


        public IActionState(GameMain game)
        {
            this.gameMain = game;

            this.map = this.gameMain.gameMap.Map;
        }

        public abstract void onEnter();

        public abstract void onExit();

        public abstract IActionState onSelectedPosition(FDPosition position);

        public abstract IActionState onUserCancelled();
    }
}