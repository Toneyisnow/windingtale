using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WindingTale.Core.Components;
using WindingTale.Core.Components.Packs;

namespace WindingTale.UI.Components.Activities
{
    public class GameActivityManager
    {
        private Queue<ActivityBase> activityQueue = null;

        private IGameCallback gameCallback = null;

        public GameActivityManager(IGameCallback gameCallback)
        {
            this.gameCallback = gameCallback;
            this.activityQueue = new Queue<ActivityBase>();
        }

        public void PushActivity(ActivityBase activity)
        {

        }

        public void PushPack(PackBase pack)
        {

        }

        public void Update()
        {

        }


    }
}