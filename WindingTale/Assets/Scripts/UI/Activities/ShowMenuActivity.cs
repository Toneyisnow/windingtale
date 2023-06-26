using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WindingTale.Core.Common;
using WindingTale.UI.Scenes.Game;

namespace WindingTale.UI.Activities
{
    public class ShowMenuActivity : ActivityBase
    {

        /// <summary>
        /// / private UIMenuItem[] menuItems = null;
        /// </summary>

        private FDMenu menu = null;


        public ShowMenuActivity(FDMenu menu)
        {
            this.menu = menu;
        }

        public override void Start(GameObject gameInterface)
        {
            /*
            menuItems = new UIMenuItem[4];
            bool hasSelected = false;
            for (int i = 0; i < 4; i++)
            {
                FDMenuItem item = menu.Items[i];
                bool isSelected = false;
                if (item.Enabled && !item.Selected)
                {
                    isSelected = true;
                    hasSelected = true;
                }

                // Place the Menu on map
                FDPosition position = item.Position;
                
                ////menuItems[i] = gameInterface.PlaceMenuItem(itemId, , pos, enabled, isSelected);

            }

            for (int i = 0; i < 4; i++)
            {
                menuItems[i].RelatedMenuItems = menuItems;
            }
            */
        }


        public override void Update(GameObject gameInterface)
        {
            // Move the menu to target position

            this.HasFinished = true;
        }

    }
}