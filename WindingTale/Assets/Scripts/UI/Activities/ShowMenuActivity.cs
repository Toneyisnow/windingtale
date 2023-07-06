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
            menuObject.name = "Menu";

            menuObject.transform.SetLocalPositionAndRotation(MapCoordinate.ConvertPosToVec3(menu.Position), Quaternion.identity);

            Menu menuComponent = menuObject.GetComponent<Menu>();
            menuComponent.Init(menu);
        }


    }
}