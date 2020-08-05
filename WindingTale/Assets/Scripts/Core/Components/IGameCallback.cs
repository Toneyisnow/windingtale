using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WindingTale.Core.Components.Packs;

namespace WindingTale.Core.Components
{
    public interface IGameCallback
    {
        void OnReceivePack(PackBase pack);

        void ComposeCreatureAt(int creatureId, int posX, int posY);



    }
}