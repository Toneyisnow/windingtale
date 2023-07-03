using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using WindingTale.Core.Common;
using WindingTale.UI.Common;
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
            Debug.Log("Show Menu Activity");
            
            Transform gameIndicatorRoot = gameInterface.GetComponent<GameInterface>().MapIndicators.transform;
            Debug.Log("gameIndicatorRoot: " + gameIndicatorRoot != null);

            GameObject menuPrefab = Resources.Load<GameObject>("Menu/Menu");
            GameObject menuObject = MonoBehaviour.Instantiate(menuPrefab, gameIndicatorRoot);

            menuObject.transform.SetLocalPositionAndRotation(MapCoordinate.ConvertPosToVec3(this.menu.Position), Quaternion.identity);

            Menu menuComponent = menuObject.GetComponent<Menu>();
            menuComponent.Init(this.menu);

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