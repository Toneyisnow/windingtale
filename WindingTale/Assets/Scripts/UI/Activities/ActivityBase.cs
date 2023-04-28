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
        public abstract void Start(IGameInterface gameInterface);

        // Update is called once per frame
        public abstract void Update(IGameInterface gameInterface);
    }
}