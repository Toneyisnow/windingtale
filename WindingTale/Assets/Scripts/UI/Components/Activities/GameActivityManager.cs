using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WindingTale.Core.Components;
using WindingTale.Core.Components.Packs;

namespace WindingTale.UI.Components.Activities
{
    public class GameActivityManager
    {
        private Queue<ActivityBase> activityQueue = null;

        private IGameCallback gameCallback = null;

        private ActivityBase currentActivity = null;

        public GameActivityManager(IGameCallback gameCallback)
        {
            this.gameCallback = gameCallback;
            this.activityQueue = new Queue<ActivityBase>();
        }

        public void PushActivity(ActivityBase activity)
        {
            activityQueue.Enqueue(activity);
        }

        public void PushPack(PackBase pack)
        {
            switch(pack.Type)
            {
                case PackBase.PackType.ComposeCreature:

                    CallbackActivity act = new CallbackActivity(
                        (callback) => { callback.ComposeCreatureAt(3, 10, 20); });
                    PushActivity(act);
                    break;
                default:
                    break;
            }
        }

        public void Update()
        {
            if (currentActivity == null && (activityQueue == null || activityQueue.Count == 0))
            {
                return;
            }

            if (currentActivity == null || currentActivity.HasFinished)
            {
                currentActivity = activityQueue.Dequeue();
                currentActivity.Start(gameCallback);
                return;
            }

            currentActivity.Update(gameCallback);

        }


    }
}