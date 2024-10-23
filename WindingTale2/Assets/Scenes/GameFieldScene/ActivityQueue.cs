
using WindingTale.Scenes.GameFieldScene.Activities;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WindingTale.Scenes.GameFieldScene
{

        public class ActivityQueue: MonoBehaviour
        {
            private List<ActivityBase> activityList = null;


            private ActivityBase currentActivity = null;

            private GameMain gameMain = null;


            public bool IsIdle { get; private set; }

            public ActivityQueue()
            {
                
            }

            public void Start()
            {
                this.gameMain = this.gameObject.GetComponent<GameMain>();
                this.activityList = new List<ActivityBase>();
            }

            public void Push(ActivityBase activity)
            {
                if (activityList != null && activity != null)
                    activityList.Add(activity);
            }

            public void Insert(ActivityBase activity)
            {
                activityList.Insert(0, activity);
            }

            public void Insert(List<ActivityBase> activities)
            {
                activityList.InsertRange(0, activities);
            }

            public void Update()
            {
                //GameFieldScene gameFieldScene = GetComponent<GameFieldScene>();
                //GameCanvas gameCanvas = gameFieldScene.canvas.GetComponent<GameCanvas>();

                //if (gameCanvas.HasDialog())
                //{
                //    // Don't update activity if Canvas is active
                //    return;
                //}

                ////Debug.Log("currentActivity: " + currentActivity?.GetType());
                ////Debug.Log("activityQueue: " + activityQueue.Count);
                if (activityList.Count > 0)
                {
                    //// Debug.Log("activityQueue: " + activityQueue[0].GetType() + " current: " + currentActivity?.GetType());
                }

                if (currentActivity == null && activityList.Count == 0)
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

                    if (activityList.Count > 0)
                    {
                        currentActivity = activityList[0];
                        activityList.RemoveAt(0);
                        Debug.LogFormat("ActivityManager: Starting activity. type={0}", currentActivity.GetType());

                        currentActivity.Start(gameMain);
                    }

                    return;
                }

                currentActivity.Update(gameMain);
            }

        }
}