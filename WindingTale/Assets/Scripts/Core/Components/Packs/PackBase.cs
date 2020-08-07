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
            ClearRange = 22,
            CloseMenu = 23,

            Talk = 31,
            Prompt = 32,

            Batch = 90,
            Sequence = 91,

        }

        public PackType Type
        {
            get; protected set;
        }
    }
}