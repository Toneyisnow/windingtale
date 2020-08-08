using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WindingTale.Core.ObjectModels;
using WindingTale.UI.MapObjects;

namespace WindingTale.UI.Components
{
    public interface IGameInterface
    {
        UICreature GetUICreature(int creatureId);

        void PlaceCreature(FDCreature creature);

    }
}