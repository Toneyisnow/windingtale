using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WindingTale.Scenes.GameFieldScene.Activities
{

    public class SimpleActivity : ActivityBase
    {
        //// private Action<IGameInterface> callbackAction = null;
        private Action<GameMain> takeAction = null;

        public SimpleActivity(Action<GameMain> action)
        {
            this.takeAction = action;
        }

        public override void Start(GameMain gameMain)
        {
            if (takeAction != null && gameMain != null)
            {
                takeAction(gameMain);
            }

            this.HasFinished = true;
        }

        // Update is called once per frame
        public override void Update(GameMain gameMain)
        {

        }

    }
}