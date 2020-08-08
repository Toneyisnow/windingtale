using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WindingTale.Core.Components;

namespace WindingTale.UI.Components.Activities
{
    public class SequenceActivity : ActivityBase
    {

        private Queue<ActivityBase> Activities
        {
            get; set;
        }

        public SequenceActivity()
        {
            this.Activities = new Queue<ActivityBase>();
        }

        public void Add(ActivityBase a)
        {
            this.Activities.Enqueue(a);
        }

        public override void Start(IGameInterface gameInterface)
        {
            throw new System.NotImplementedException();
        }

        public override void Update(IGameInterface gameInterface)
        {
            throw new System.NotImplementedException();
        }
    }
}