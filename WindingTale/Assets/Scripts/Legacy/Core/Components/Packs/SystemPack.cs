using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WindingTale.Core.Components.Packs
{
    public class SystemPack : PackBase
    {
        public enum Command
        {
            Quit = 1,
        }

        private Command command;

        public SystemPack(Command c)
        {
            this.command = c;
        }



    }
}
