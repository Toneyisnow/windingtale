using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WindingTale.Scenes.GameFieldScene.Activities
{

    public class SimpleActivity : ActivityBase
    {
        //// private Action<IGameInterface> callbackAction = null;
        private Action<GameMain> callback = null;

        public SimpleActivity(Action<GameMain> action)
        {
            this.callback = action;
        }

        public override void Start(GameMain gameMain)
        {
            if (callback != null && gameMain != null)
            {
                callback(gameMain);
            }

            this.HasFinished = true;
        }

        // Update is called once per frame
        public override void Update(GameMain gameMain)
        {

        }

    }
}