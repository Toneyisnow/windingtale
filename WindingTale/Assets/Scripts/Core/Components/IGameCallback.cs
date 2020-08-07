using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WindingTale.Core.Components.Packs;
using WindingTale.Core.ObjectModels;

namespace WindingTale.Core.Components
{
    public interface IGameCallback
    {
        void OnReceivePack(PackBase pack);

        #region Activity Called Functions

        void PlaceCreature(FDCreature creature);


        #endregion
    }
}