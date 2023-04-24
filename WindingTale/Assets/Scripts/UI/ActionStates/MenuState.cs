using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WindingTale.Common;
using WindingTale.Core.Components.Packs;

namespace WindingTale.Core.Components.ActionStates
{
    public enum MenuId
    {
        ActionMenu = 11,
        ItemsMenu = 12,

        SystemMenu = 21,
        SettingsMenu = 22,
        RecordMenu = 23,

    }

    public enum MenuItemId
    {
        ActionMagic = 110,
        ActionAttack = 111,
        ActionItems = 112,
        ActionRest = 113,

        ItemExchange = 120,
        ItemUse = 121,
        ItemEquip = 122,
        ItemDiscard = 123,

        SystemMatching = 210,
        SystemRecord = 211,
        SystemSettings = 212,
        SystemRestAll = 213,

        SettingsSound = 220,
        SettingsMusic = 221,
        SettingsFight = 222,
        SettingsInfo = 223,

        RecordSave = 230,
        RecordInfo = 231,
        RecordLoad = 232,
        RecordQuit = 233,
    }

    public abstract class MenuState : ActionState
    {
        public FDPosition Central
        {
            get; private set;
        }

        public FDPosition[] MenuItemPositions
        {
            get; private set;
        }

        public MenuItemId[] MenuItemIds
        {
            get; private set;
        }

        public bool[] MenuItemEnabled
        {
            get; private set;
        }

        public Func<StateOperationResult>[] MenuActions
        {
            get; private set;
        }

        public MenuState(IGameAction gameAction, FDPosition central) : base(gameAction)
        {
            this.Central = central;

            this.MenuItemPositions = new FDPosition[4]
            {
                FDPosition.At(Central.X - 1, Central.Y),
                FDPosition.At(Central.X, Central.Y - 1),
                FDPosition.At(Central.X + 1, Central.Y),
                FDPosition.At(Central.X, Central.Y + 1),
            };

            this.MenuItemIds = new MenuItemId[4];
            this.MenuActions = new Func<StateOperationResult>[4];
            this.MenuItemEnabled = new bool[4];
        }

        public override void OnEnter()
        {
            base.OnEnter();

            // Show Action Menu
            ShowMenuPack pack = new ShowMenuPack(this.MenuItemIds, this.MenuItemEnabled, this.Central);
            SendPack(pack);
        }

        public override void OnExit()
        {
            base.OnExit();

            // Close Action Menu
            CloseMenuPack pack = new CloseMenuPack();
            SendPack(pack);
        }

        protected void SetMenu(int index, MenuItemId menuItemId, bool enabled, Func<StateOperationResult> action)
        {
            if (index < 0 || index >= 4)
            {
                return;
            }

            this.MenuItemIds[index] = menuItemId;
            this.MenuActions[index] = action;
            this.MenuItemEnabled[index] = enabled;
        }

        public override StateOperationResult OnSelectPosition(FDPosition position)
        {
            for(int index = 0; index < 4; index++)
            {
                if (this.MenuItemEnabled[index] && this.MenuItemPositions[index].AreSame(position))
                {
                    // Clicked on menu
                    return this.MenuActions[index]();
                }
            }

            // Cancell the menu
            return StateOperationResult.Pop();
        }

    }
}