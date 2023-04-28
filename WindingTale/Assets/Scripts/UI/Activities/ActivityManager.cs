using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WindingTale.Core.Components;
using WindingTale.Core.Components.Packs;
using WindingTale.UI.Scenes.Game;

namespace WindingTale.UI.Activities
{
    public class ActivityManager
    {
        private Queue<ActivityBase> activityQueue = null;

        private IGameInterface gameInterface = null;

        private ActivityBase currentActivity = null;

        public bool IsIdle { get; private set; }


        public ActivityManager(IGameInterface gameInterface)
        {
            this.gameInterface = gameInterface;
            this.activityQueue = new Queue<ActivityBase>();
        }

        public void Push(ActivityBase activity)
        {
            activityQueue.Enqueue(activity);
        }


        public void Update()
        {
            if (currentActivity == null && (activityQueue == null || activityQueue.Count == 0))
            {
                // Nothing to do now
                if (!IsIdle)
                {
                    IsIdle = true;
                    gameInterface.GetGameHandler().NotifyAI();
                }

                return;
            }

            IsIdle = false;
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

    }
}