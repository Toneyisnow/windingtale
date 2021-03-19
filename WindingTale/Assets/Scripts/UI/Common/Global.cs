using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WindingTale.UI.Common
{
    public class Global
    {
        private static Global instance = null;

        public Int32 CurrentTick
        {
            get; private set;
        }

        private Global()
        {

        }

        public static Global Instance()
        {
            if (instance == null)
            {
                instance = new Global();
            }

            return instance;
        }

        public void Tick()
        {
            if (this.CurrentTick++ > Int32.MaxValue - 10)
            {
                this.CurrentTick = 0;
            }

        }


    }
}