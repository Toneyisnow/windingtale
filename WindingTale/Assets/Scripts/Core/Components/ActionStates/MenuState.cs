using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WindingTale.Common;
using WindingTale.Core.Components.Packs;

namespace WindingTale.Core.Components.ActionStates
{
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
            // Show Action Menu
            ShowMenuPack pack = new ShowMenuPack(this.MenuItemIds, this.MenuItemEnabled, this.Central);
            SendPack(pack);
        }

        public override void OnExit()
        {
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
                if (this.MenuItemEnabled[index] && this.MenuItemPositions[index] == position)
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