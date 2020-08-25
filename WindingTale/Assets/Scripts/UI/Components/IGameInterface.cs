using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WindingTale.Common;
using WindingTale.Core.Components;
using WindingTale.Core.ObjectModels;
using WindingTale.UI.MapObjects;

namespace WindingTale.UI.Components
{
    /// <summary>
    /// 
    /// </summary>
    public interface IGameInterface
    {
        IGameHandler GetGameHandler();

        UICreature GetUICreature(int creatureId);

        UICreature PlaceCreature(int creatureId, int animationid, FDPosition position);

        void TouchCreature(int creatureId);

        void TouchPosition(FDPosition position);

        UIMenuItem PlaceMenu(MenuItemId menuItem, FDPosition position, bool enabled, bool selected);

        void ClearCancellableObjects();
    }
}