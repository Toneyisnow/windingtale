using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WindingTale.Core.Components;
using WindingTale.UI.Scenes.Game;

namespace WindingTale.UI.Activities
{
    public class BatchActivity : ActivityBase
    {
        private List<ActivityBase> Activities
        {
            get; set;
        }

        public BatchActivity()
        {
            this.Activities = new List<ActivityBase>();
        }

        public void Add(ActivityBase a)
        {
            this.Activities.Add(a);
        }

        public override void Start(IGameInterface gameInterface)
        {
            foreach(ActivityBase activity in this.Activities)
            {
                activity.Start(gameInterface);
            }
        }

        public override void Update(IGameInterface gameInterface)
        {
            bool hasFinished = true;
            foreach (ActivityBase activity in this.Activities)
            {
                if (!activity.HasFinished)
                {
                    activity.Update(gameInterface);
                    hasFinished = false;
                }
            }

            this.HasFinished = hasFinished;
        }

    }
}