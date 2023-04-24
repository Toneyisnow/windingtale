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

        private IGameInterface gameInterface = null;

        private ActivityBase currentActivity = null;

        private bool isIdling = false;

        public GameActivityManager(IGameInterface gameInterface)
        {
            this.gameInterface = gameInterface;
            this.activityQueue = new Queue<ActivityBase>();
        }

        public void PushActivity(ActivityBase activity)
        {
            activityQueue.Enqueue(activity);
        }

        public void PushPack(PackBase pack)
        {
            Debug.LogFormat("PushPack: type={0}", pack.Type);
            var activity = ActivityBuilder.BuildFromPack(pack);
            if (activity != null)
            {
                this.PushActivity(activity);
            }
        }

        public void Update()
        {
            if (currentActivity == null && (activityQueue == null || activityQueue.Count == 0))
            {
                // Nothing to do now
                if (!isIdling)
                {
                    isIdling = true;
                    gameInterface.GetGameHandler().NotifyAI();
                }

                return;
            }

            isIdling = false;
            if (currentActivity == null || currentActivity.HasFinished)
            {
                if (currentActivity != null)
                {
                    Debug.LogFormat("ActivityManager: Finished activity. type={0}", currentActivity.GetType());
                    currentActivity = null;
                }

                if (activityQueue != null && activityQueue.Count > 0)
                {
                    currentActivity = activityQueue.Dequeue();
                    Debug.LogFormat("ActivityManager: Starting activity. type={0}", currentActivity.GetType());

                    currentActivity.Start(gameInterface);
                }
                
                return;
            }

            currentActivity.Update(gameInterface);
        }

        public bool IsIdling()
        {
            return isIdling;
        }
    }
}