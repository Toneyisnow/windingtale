using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WindingTale.Scenes.GameFieldScene.Activities
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
        public virtual void Start(GameMain gameMain)
        {

        }

        // Update is called once per frame
        public virtual void Update(GameMain gameMain)
        {
            this.HasFinished = true;
        }
    }
}