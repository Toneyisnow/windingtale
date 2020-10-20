using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WindingTale.Core.Components.Packs
{
    public abstract class PackBase
    {
        public enum PackType
        {
            CreatureCompose = 1,
            CreatureMove = 10,
            CreatureRefresh = 11,
            CreatureShowInfo = 12,
            CreatureRefreshAll = 13,
            CreatureDead = 14,
            CreatureDispose = 15,

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