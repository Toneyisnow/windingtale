using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WindingTale.Scenes.GameFieldScene.Activities
{

    public class DurationActivity : ActivityBase
    {
        //// private Action<IGameInterface> callbackAction = null;
        private Action<GameMain> startAction = null;

        private Action<GameMain> endAction = null;


        private Func<GameMain, bool> checkEnd = null;

        public DurationActivity(Action<GameMain> startAction, Func<GameMain, bool> checkEnd, Action<GameMain> endAction = null)
        {
            this.startAction = startAction;
            this.checkEnd = checkEnd;
            this.endAction = endAction;
        }

        public override void Start(GameMain gameMain)
        {
            if (startAction != null && gameMain != null)
            {
                startAction(gameMain);
            }

            this.HasFinished = false;
        }

        // Update is called once per frame
        public override void Update(GameMain gameMain)
        {
            if (this.HasFinished)
            {
                return;
            }

            bool hasFinished = checkEnd(gameMain);
            if (hasFinished)
            {
                this.HasFinished = true;

                if (endAction != null)
                {
                    endAction(gameMain);
                }
            }
        }

    }
}