using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WindingTale.Scenes.GameFieldScene.Activities
{

    public class SequencedActivity : ActivityBase
    {

        private List<ActivityBase> activities = null;

        private int currentIndex = 0;

        public SequencedActivity(List<ActivityBase> activities)
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

            currentIndex = 0;
            activities[currentIndex].Start(gameMain);
        }

        // Update is called once per frame
        public override void Update(GameMain gameMain)
        {
            activities[currentIndex].Update(gameMain);
            if (activities[currentIndex].HasFinished)
            {
                if (currentIndex < activities.Count - 1)
                {
                    currentIndex++;
                    activities[currentIndex].Start(gameMain);
                }
                else
                {
                    this.HasFinished = true;
                }
            }

        }

    }
}