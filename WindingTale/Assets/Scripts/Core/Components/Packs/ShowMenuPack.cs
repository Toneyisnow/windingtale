using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WindingTale.Common;

namespace WindingTale.Core.Components.Packs
{
    public class ShowMenuPack : PackBase
    {
        public MenuItemId[] MenuItems
        {
            get; private set;
        }

        public bool[] Enabled
        {
            get; private set;
        }

        public FDPosition Position
        {
            get; private set;
        }

        public ShowMenuPack(MenuItemId[] menuItems, bool[] enabled, FDPosition position)
        {
            this.Type = PackType.ShowMenu;

            this.MenuItems = menuItems;
            this.Enabled = enabled;
            this.Position = position;
        }
    }
}
