using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WindingTale.Core.Components.Packs;
using WindingTale.Core.ObjectModels;

namespace WindingTale.Legacy.Core.Components
{
    public interface IGameCallback
    {
        /// <summary>
        /// The Handler send a pack back to Interface
        /// </summary>
        /// <param name="pack"></param>
        void OnHandlePack(PackBase pack);


    }
}