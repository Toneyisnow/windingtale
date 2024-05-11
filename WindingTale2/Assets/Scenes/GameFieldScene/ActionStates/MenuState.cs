using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WindingTale.Core.Common;
using WindingTale.Scenes.GameFieldScene;
using WindingTale.Scenes.GameFieldScene.ActionStates;

public class MenuState : IActionState
{
    // private FDPosition[] menuItemPositions;

    public FDMenu Menu
    {
        get; private set;
    }

    public MenuState(GameMain game, FDPosition central) : base(game)
    {
        this.Menu = new FDMenu(central);

        //this.menuItemPositions = new FDPosition[4];
        //this.menuItemPositions[0] = FDPosition.At(central.X - 1, central.Y);
        //this.menuItemPositions[1] = FDPosition.At(central.X, central.Y - 1);
        //this.menuItemPositions[2] = FDPosition.At(central.X + 1, central.Y);
        //this.menuItemPositions[3] = FDPosition.At(central.X, central.Y + 1);
    }

    public override void onEnter()
    {
        // Show Action Menu

        // Set default active
    }

    public override void onExit()
    {
        // Close Action Menu

    }

    public override IActionState onSelectedPosition(FDPosition position)
    {
        return this;
    }

    public override IActionState onUserCancelled()
        {
        return this;
    }


    protected void SetMenu(int index, MenuItemId menuItemId, bool enabled, Action action)
    {
        if (index < 0 || index >= 4)
        {
            return;
        }

        this.Menu.Items[index] = new FDMenuItem(menuItemId, enabled, action, this.Menu.GetItemPosition(index), this.Menu);
    }


}
