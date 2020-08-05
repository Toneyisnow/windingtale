using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WindingTale.Core.Components.Packs
{
    public abstract class PackBase
    {
        public enum PackType
        {
            ComposeCreature = 0,
            MoveCreature = 10,
            ShowRange = 20,
            ShowMenu = 21,

            Talk = 31,
            Prompt = 32,


        }

        public PackType Type
        {
            get; protected set;
        }
    }
}