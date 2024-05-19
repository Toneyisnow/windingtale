using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WindingTale.Scenes.GameFieldScene.Activities
{

    public class ParallelActivity : ActivityBase
    {
        //// private Action<IGameInterface> callbackAction = null;
        private List<ActivityBase> activities = null;


        public ParallelActivity(List<ActivityBase> activities)
        {
            this.activities = activities;
        }

        public override void Start(GameMain gameMain)
        {
            if (activities == null || gameMain == null || activities.Count == 0)
            {
                this.HasFinished = true;
                return;
            }

            foreach(ActivityBase activity in activities)
            {
                if (activity != null)
                {
                    activity.Start(gameMain);
                }
            }
        }

        // Update is called once per frame
        public override void Update(GameMain gameMain)
        {
            bool allFinished = true;
            foreach (ActivityBase activity in activities)
            {
                if (!activity.HasFinished)
                {
                    allFinished = false;
                    activity.Update(gameMain);
                }
            }

            if (allFinished)
            {
                this.HasFinished = true;
            }

        }

    }
}