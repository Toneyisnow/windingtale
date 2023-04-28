using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WindingTale.Common;
using WindingTale.Core.Components.Packs;
using WindingTale.Core.Map;
using WindingTale.UI.Activities;
using WindingTale.UI.Scenes.Game;

namespace WindingTale.UI.ActionStates
{
    public abstract class ActionState
    {
        protected GameMain gameMain = null;

        protected GameMap gameMap = null;

        protected ActivityManager activityManager = null;

        public ActionState(GameMain gameMain)
        {
            this.gameMain = gameMain;
            this.gameMap = gameMain.GameMap;
            this.activityManager = gameMain.ActivityManager;
        }

        public virtual void OnEnter()
        {
            Debug.LogFormat("OnEnter State: {0}", this.GetType().Name);
        }

        public virtual void OnExit()
        {
            Debug.LogFormat("OnExit State: {0}", this.GetType().Name);
        }

        #region State Operations

        public abstract StateResult OnSelectPosition(FDPosition position);

        public virtual StateResult OnSelectCallback(int index)
        {
            return null;
        }

        #endregion
    }
}