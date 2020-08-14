using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WindingTale.Common;

namespace WindingTale.Core.Components.Packs
{
    public class ShowMenuPack : PackBase
    {
        public MenuId MenuId
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

        public ShowMenuPack(MenuId menuId, bool[] enabled, FDPosition position)
        {
            this.MenuId = menuId;
            this.Enabled = enabled;
            this.Position = position;
        }
    }
}
