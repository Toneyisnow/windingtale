using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WindingTale.UI.Scenes.Game;

namespace WindingTale.UI.Activities
{
    public class SequenceActivity : ActivityBase
    {
        private Queue<ActivityBase> activityQueue = null;

        private ActivityBase currentActivity = null;

        public SequenceActivity()
        {
            this.activityQueue = new Queue<ActivityBase>();
        }

        public void Add(ActivityBase a)
        {
            this.activityQueue.Enqueue(a);
        }

        public override void Start(IGameInterface gameInterface)
        {
            if (currentActivity == null && (activityQueue == null || activityQueue.Count == 0))
            {
                this.HasFinished = true;
                return;
            }

            currentActivity = activityQueue.Dequeue();
            currentActivity.Start(gameInterface);
        }


        public override void Update(IGameInterface gameInterface)
        {
            if (currentActivity == null && (activityQueue == null || activityQueue.Count == 0))
            {
                this.HasFinished = true;
                return;
            }

            if (currentActivity == null || currentActivity.HasFinished)
            {
                if (currentActivity != null)
                {
                    Debug.LogFormat("SequenceActivity: Finished activity. type={0}", currentActivity.GetType());
                    currentActivity = null;

                    this.HasFinished = true;
                }

                if (activityQueue != null && activityQueue.Count > 0)
                {
                    currentActivity = activityQueue.Dequeue();
                    Debug.LogFormat("SequenceActivity: Starting activity. type={0}", currentActivity.GetType());

                    currentActivity.Start(gameInterface);
                }

                return;
            }

            currentActivity.Update(gameInterface);
        }
    }
}