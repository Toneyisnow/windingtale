using System;
using System.Collections.Generic;
using System.Reflection;
using WindingTale.Common;

namespace WindingTale.Core.Common
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

    public class FDMenu
    {
        public FDMenuItem[] Items { get; set; }

        public FDPosition Position { get; set; }

        public FDMenu(FDPosition position)
        {
            this.Position = position;
            this.Items = new FDMenuItem[4];
        }

        public void SetSelected(int index)
        {

        }

        /*
        public static MenuId GetMenuId(MenuItemId menuItemId)
        {
            int menuId = (int)menuItemId / 10;
            return (MenuId)menuId;
        }

        public static List<MenuItemId> GetItems(MenuId menuId)
        {
            List<MenuItemId> items = new List<MenuItemId>();
            for(int index = 0; index < 4; index++)
            {
                int menuItemId = (int)menuId * 10 + index;
                items.Add((MenuItemId)menuItemId);
            }

            return items;
        }
        */

        public static FDPosition[] GetPositions(FDPosition central)
        {
            return new FDPosition[4]
            {
                FDPosition.At(central.X - 1, central.Y),
                FDPosition.At(central.X, central.Y - 1),
                FDPosition.At(central.X + 1, central.Y),
                FDPosition.At(central.X, central.Y + 1),
            };
        }

    }

    public class FDMenuItem
    {
        public MenuItemId Id
        {
            get;
            private set;
        }

        public bool Enabled
        {
            get;
            set;
        }

        public bool Selected
        {
            get;
            set;
        }

        public Action Action { get; set; }

        public FDPosition Position { get; set; }

        public FDMenuItem(MenuItemId menuItemId, bool enabled, Action action)
        {
            this.Id = menuItemId;
            this.Enabled = enabled;
            this.Action = action;

            this.Selected = false;
        }
    }
}