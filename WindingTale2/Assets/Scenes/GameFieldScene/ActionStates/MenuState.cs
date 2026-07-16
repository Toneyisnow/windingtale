using System;
using System.Collections;
using System.Collections.Generic;
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
        Debug.Log("MenuState: onEnter");
        gameMain.gameMap.ShowMenu(fdMenu);

        // Auto-select the first enabled item, in Left / Up / Right / Bottom order.
        SelectDefaultItem();
    }

    /// <summary>
    /// Selects (and animates) the first enabled item scanning index 0..3
    /// (Left, Up, Right, Bottom). No-op if the menu has no enabled item.
    /// </summary>
    private void SelectDefaultItem()
    {
        for (int index = 0; index < 4; index++)
        {
            FDMenuItem item = this.fdMenu.Items[index];
            if (item != null && item.Enabled)
            {
                this.fdMenu.SetSelected(index);
                gameMain.gameMap.SetMenuActiveItem(index);
                return;
            }
        }
    }

    public override void onExit()
    {
        Debug.Log("MenuState: onExit");
        // Close Action Menu
        gameMain.gameMap.CloseMenu(fdMenu);

    }

    public override IActionState onSelectedPosition(FDPosition position)
    {
        Debug.Log("MenuState: onSelectedPosition" + position.ToString());

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

                // if menu item not selected, select it (and animate it)
                if (!item.Selected)
                {
                    item.Menu.SetSelected(index);
                    gameMain.gameMap.SetMenuActiveItem(index);
                    return this;
                }

                // Clicked on menu
                return item.Action();
            }
        }

        return backState;
    }

    public override IActionState onUserCancelled()
    {
        return backState;
    }

    // The map cursor is hidden in every menu; the selected item animates instead.
    public override bool HidesCursor => true;

    #region Keyboard input

    // A held arrow key must not repeatedly re-select the same menu item.
    public override bool SupportsDirectionRepeat => false;

    /// <summary>
    /// Arrow keys move the active item to the one in that direction (Left/Top/Right/
    /// Bottom map to item index 0/1/2/3). Disabled items cannot become active.
    /// </summary>
    public override void OnDirectionKey(InputDirection direction)
    {
        int index = DirectionToItemIndex(direction);
        FDMenuItem item = this.fdMenu.Items[index];
        if (item == null || !item.Enabled)
        {
            // Not selectable in that direction: no reaction.
            return;
        }

        this.fdMenu.SetSelected(index);

        // The selected item animates as the highlight (the map cursor stays hidden).
        gameMain.gameMap.SetMenuActiveItem(index);
    }

    /// <summary>
    /// Enter / Space activates the currently active item, exactly as clicking it does.
    /// </summary>
    public override void OnConfirmKey()
    {
        int index = GetSelectedIndex();
        if (index < 0)
        {
            // Nothing active yet.
            return;
        }

        FDMenuItem item = this.fdMenu.Items[index];
        if (item == null || !item.Enabled)
        {
            return;
        }

        IActionState nextState = item.Action();
        playerInterface.onUpdateState(nextState);
    }

    /// <summary>
    /// Backspace / ESC rolls back to the previous state.
    /// </summary>
    public override void OnCancelKey()
    {
        playerInterface.onUserCancelled();
    }

    private static int DirectionToItemIndex(InputDirection direction)
    {
        switch (direction)
        {
            case InputDirection.Left: return 0;
            case InputDirection.Up: return 1;
            case InputDirection.Right: return 2;
            case InputDirection.Down: return 3;
            default: return 0;
        }
    }

    private int GetSelectedIndex()
    {
        for (int i = 0; i < this.fdMenu.Items.Length; i++)
        {
            FDMenuItem item = this.fdMenu.Items[i];
            if (item != null && item.Selected)
            {
                return i;
            }
        }

        return -1;
    }

    #endregion


    protected void SetMenu(int index, MenuItemId menuItemId, bool enabled, Func<IActionState> action)
    {
        if (index < 0 || index >= 4)
        {
            return;
        }

        this.fdMenu.Items[index] = new FDMenuItem(menuItemId, enabled, action, this.fdMenu.GetItemPosition(index), this.fdMenu);
    }


}
