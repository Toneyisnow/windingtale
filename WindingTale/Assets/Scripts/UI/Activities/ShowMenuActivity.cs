using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WindingTale.Common;
using WindingTale.Core.Common;
using WindingTale.UI.MapObjects;
using WindingTale.UI.Scenes.Game;

namespace WindingTale.UI.Activities
{
    public class ShowMenuActivity : ActivityBase
    {

        private UIMenuItem[] menuItems = null;

        private FDMenu menu = null;

        public ShowMenuActivity(FDMenu menu)
        {
            this.menu = menu;
        }

        public override void Start(IGameInterface gameInterface)
        {
            if (pack == null)
            {
                return;
            }

            menuItems = new UIMenuItem[4];
            FDPosition pos = pack.Position;
            FDPosition[] positions = new FDPosition[4]
            {
                FDPosition.At(pos.X - 1, pos.Y),
                FDPosition.At(pos.X, pos.Y - 1),
                FDPosition.At(pos.X + 1, pos.Y),
                FDPosition.At(pos.X, pos.Y + 1)
            };

            bool hasSelected = false;
            for (int i = 0; i < 4; i++)
            {
                MenuItemId itemId = pack.MenuItems[i];
                bool enabled = pack.Enabled[i];
                bool isSelected = false;
                if (enabled && !hasSelected)
                {
                    isSelected = true;
                    hasSelected = true;
                }

                // Place the Menu on map
                menuItems[i] = gameInterface.PlaceMenuItem(itemId, positions[i], pos, enabled, isSelected);

            }

            for (int i = 0; i < 4; i++)
            {
                menuItems[i].RelatedMenuItems = menuItems;
            }
        }


        public override void Update(IGameInterface gameInterface)
        {
            // Move the menu to target position

            this.HasFinished = true;
        }

    }
}