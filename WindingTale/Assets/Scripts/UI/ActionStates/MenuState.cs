using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WindingTale.Core.Common;
using WindingTale.UI.Activities;
using WindingTale.UI.Scenes.Game;

namespace WindingTale.UI.ActionStates
{
    public abstract class MenuState : ActionState
    {

        public FDMenu Menu { get; private set; }


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

        public Action[] MenuActions
        {
            get; private set;
        }

        public MenuState(GameMain gameMain, IStateResultHandler stateHandler, FDPosition central) : base(gameMain, stateHandler)
        {
            this.Menu = new FDMenu(central);
        }

        public override void OnEnter()
        {
            base.OnEnter();

            // Show Action Menu
            ShowMenuActivity activity = new ShowMenuActivity(this.Menu);
            activityManager.Push(activity);
        }

        public override void OnExit()
        {
            base.OnExit();

            // Close Action Menu
            CloseMenuActivity activity = new CloseMenuActivity();
        }

        protected void SetMenu(int index, MenuItemId menuItemId, bool enabled, Action action)
        {
            if (index < 0 || index >= 4)
            {
                return;
            }

            this.Menu.Items[index] = new FDMenuItem(menuItemId, enabled, action, this.Menu);
        }

        public override void OnSelectPosition(FDPosition position)
        {
            for(int index = 0; index < 4; index++)
            {
                if (this.MenuItemEnabled[index] && this.Menu.Items[index].Position.AreSame(position))
                {
                    // Clicked on menu
                    this.MenuActions[index]();
                }
            }

            stateHandler.HandlePopState();
        }

    }
}