using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WindingTale.Core.Components;
using WindingTale.UI.Scenes.Game;

namespace WindingTale.UI.Activities
{
    public abstract class ActivityBase 
    {
        public bool HasFinished
        {
            get; protected set;
        }

        public ActivityBase()
        {
            this.HasFinished = false;
        }

        // Start is called before the first frame update
        public virtual void Start(GameObject gameInterface)
        {

        }

        // Update is called once per frame
        public virtual void Update(GameObject gameInterface)
        {
            this.HasFinished = true;
        }
    }
}