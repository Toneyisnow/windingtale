using System;
using System.Collections.Generic;
using System.Reflection;
using Unity.VisualScripting;

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
            foreach(FDMenuItem item in this.Items)
            {
                item.Selected = false;
            }

            this.Items[index].Selected = true;

        }

        /// <summary>
        /// Menu Item directions: 0-Left, 1-Top, 2-Right, 3-Bottom
        /// </summary>
        public FDPosition GetItemPosition0(int index)
        {
            switch(index)
            {
                case 0:
                    return FDPosition.At(this.Position.X - 1, this.Position.Y);
                 case 1:
                    return FDPosition.At(this.Position.X, this.Position.Y - 1);
                 case 2:
                    return FDPosition.At(this.Position.X + 1, this.Position.Y);
                 case 3:
                    return FDPosition.At(this.Position.X, this.Position.Y + 1);
                default:
                    return this.Position;
            }
        }

        public FDPosition GetItemPosition(int index)
        {
            switch (index)
            {
                case 0:
                    return FDPosition.At( - 1, 0);
                case 1:
                    return FDPosition.At(0, - 1);
                case 2:
                    return FDPosition.At(1, 0);
                case 3:
                    return FDPosition.At(0, 1);
                default:
                    return this.Position;
            }
        }


        public static FDPosition[] GetItemPositions(FDPosition central)
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

        public FDMenu Menu { get; private set; }

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

        public FDMenuItem(MenuItemId menuItemId, bool enabled, Action action, FDPosition position, FDMenu menu)
        {
            this.Id = menuItemId;
            this.Enabled = enabled;
            this.Action = action;
            this.Selected = false;
            this.Position = position;
            this.Menu = menu;
        }
    }
}