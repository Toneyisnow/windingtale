using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WindingTale.UI.Scenes;
using WindingTale.UI.Scenes.Game;

namespace WindingTale.UI.Activities
{
    public class ActivityManager : MonoBehaviour
    {
        private List<ActivityBase> activityQueue = null;


        private ActivityBase currentActivity = null;

        public bool IsIdle { get; private set; }

        public ActivityManager()
        {
            this.activityQueue = new List<ActivityBase>();
        }

        public void Push(ActivityBase activity)
        {
            activityQueue.Add(activity);
        }

        public void Insert(ActivityBase activity)
        {
            activityQueue.Insert(0, activity);
        }

        public void Insert(List<ActivityBase> activities)
        {
            activityQueue.InsertRange(0, activities);
        }

        public void Update()
        {
            GameFieldScene gameFieldScene = GetComponent<GameFieldScene>();
            GameCanvas gameCanvas = gameFieldScene.canvas.GetComponent<GameCanvas>();

            if (gameCanvas.HasDialog())
            {
                // Don't update activity if Canvas is active
                return;
            }

            ////Debug.Log("currentActivity: " + currentActivity?.GetType());
            ////Debug.Log("activityQueue: " + activityQueue.Count);
            if (activityQueue.Count > 0)
            {
                //// Debug.Log("activityQueue: " + activityQueue[0].GetType() + " current: " + currentActivity?.GetType());
            }

            if (currentActivity == null && activityQueue.Count == 0)
            {
                // Nothing to do now
                if (!IsIdle)
                {
                    IsIdle = true;
                    /// gameInterface.GetGameHandler().NotifyAI();
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

                if (activityQueue.Count > 0)
                {
                    currentActivity = activityQueue[0];
                    activityQueue.RemoveAt(0);
                    Debug.LogFormat("ActivityManager: Starting activity. type={0}", currentActivity.GetType());

                    currentActivity.Start(gameObject);
                }
                
                return;
            }

            currentActivity.Update(gameObject);
        }

    }
}