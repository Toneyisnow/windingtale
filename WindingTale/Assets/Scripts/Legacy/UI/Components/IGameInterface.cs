using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WindingTale.Common;
using WindingTale.Core.Components;
using WindingTale.Core.Components.Algorithms;
using WindingTale.Core.ObjectModels;
using WindingTale.UI.CanvasControls;
using WindingTale.UI.Dialogs;
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

        void RefreshCreature(FDCreature creature);

        void RefreshAllCreatures();

        void DisposeCreature(int creatureId);

        void TouchCreature(int creatureId);

        void TouchShape(FDPosition position);

        void TouchCursor();

        void TouchMenu(FDPosition position);

        UIMenuItem PlaceMenuItem(MenuItemId menuItem, FDPosition position, FDPosition showUpPosition, bool enabled, bool selected);

        void ClearCancellableObjects();

        void PlaceIndicators(FDRange range);

        void ShowCreatureDialog(FDCreature creature, CreatureDialog.ShowType showType);

        void ShowConversationDialog(FDCreature creature, ConversationId conversation);

        void ShowMessageDialog(FDCreature creature, MessageId message);


        void BattleFight(FDCreature subject, FDCreature target, FightInformation fightInfo);

        void BattleMagic(FDCreature subject, FDCreature target, FightInformation magicInfo);

        bool IsBusy();
    }
}