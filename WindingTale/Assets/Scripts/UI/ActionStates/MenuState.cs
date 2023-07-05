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


        public FDPosition[] MenuItemPositions
        {
            get; private set;
        }

        public MenuItemId[] MenuItemIds
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

            Debug.Log("Enter Menu State");

            if (GameInterface.Instance.MapIndicators.transform.Find("Menu") != null)
            {
                Debug.Log("Already have menu, skip this");
                return;
            }


            // Show Action Menu
            ShowMenuActivity activity = new ShowMenuActivity(this.Menu);
            activityManager.Push(activity);
        }

        public override void OnExit()
        {
         
            // Close Action Menu
            CloseMenuActivity activity = new CloseMenuActivity();
            activityManager.Push(activity);
            base.OnExit();
        }

        protected void SetMenu(int index, MenuItemId menuItemId, bool enabled, Action action)
        {
            if (index < 0 || index >= 4)
            {
                return;
            }

            FDPosition central = this.Menu.Position;
            FDPosition itemPosition = central;
            switch(index)
            {
                case 0:
                    itemPosition = FDPosition.At(central.X, central.Y - 1);
                    break;
                case 1:
                    itemPosition = FDPosition.At(central.X + 1, central.Y);
                    break;
                case 2:
                    itemPosition = FDPosition.At(central.X, central.Y + 1);
                    break;
                case 3:
                    itemPosition = FDPosition.At(central.X - 1, central.Y);
                    break;
                default:
                    break;
            }
            this.Menu.Items[index] = new FDMenuItem(menuItemId, enabled, action, itemPosition, this.Menu);
        }

        public override void OnSelectPosition(FDPosition position)
        {
            for(int index = 0; index < 4; index++)
            {
                FDMenuItem item = this.Menu.Items[index];
                if (item.Position != null && item.Position.AreSame(position))
                {
                    // Clicked on menu
                    item.Action();
                    return;
                    //// this.MenuActions[index]();
                }
            }

            stateHandler.HandlePopState();
        }

    }
}