using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using WindingTale.Core.Common;
using WindingTale.MapObjects.GameMap;
using WindingTale.Scenes.GameFieldScene;
using WindingTale.Scenes.GameFieldScene.ActionStates;

public class MenuState : IActionState
{
    // private FDPosition[] menuItemPositions;

    public FDMenu fdMenu
    {
        get; private set;
    }

    protected IActionState nextState = null;

    protected IActionState backState = null;


    public MenuState(GameMain game, FDPosition central, IActionState backState) : base(game)
    {
        this.fdMenu = new FDMenu(central);
        this.backState = backState;

        //this.menuItemPositions = new FDPosition[4];
        //this.menuItemPositions[0] = FDPosition.At(central.X - 1, central.Y);
        //this.menuItemPositions[1] = FDPosition.At(central.X, central.Y - 1);
        //this.menuItemPositions[2] = FDPosition.At(central.X + 1, central.Y);
        //this.menuItemPositions[3] = FDPosition.At(central.X, central.Y + 1);
    }

    public override void onEnter()
    {
        gameMain.gameMap.ShowMenu(fdMenu);

        // Set default active
    }

    public override void onExit()
    {
        // Close Action Menu
        gameMain.gameMap.CloseMenu(fdMenu);

    }

    public override IActionState onSelectedPosition(FDPosition position)
    {
        for (int index = 0; index < 4; index++)
        {
            FDMenuItem item = this.fdMenu.Items[index];
            if (item.Position != null && item.Position.AreSame(position))
            {
                // if menu item not enabled, no op
                if (!item.Enabled)
                {
                    return this;
                }

                // if menu item not selected, select it
                if (!item.Selected)
                {
                    item.Menu.SetSelected(index);
                    return this;
                }

                // Clicked on menu
                item.Action();
                return nextState;
            }
        }

        return backState;
    }

    public override IActionState onUserCancelled()
    {
        return backState;
    }


    protected void SetMenu(int index, MenuItemId menuItemId, bool enabled, Action action)
    {
        if (index < 0 || index >= 4)
        {
            return;
        }

        this.fdMenu.Items[index] = new FDMenuItem(menuItemId, enabled, action, this.fdMenu.GetItemPosition(index), this.fdMenu);
    }


}
