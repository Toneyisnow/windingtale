using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WindingTale.Common;
using WindingTale.UI.Components;
using static WindingTale.UI.Common.Constants;

namespace WindingTale.UI.MapObjects
{
    public class UIMenuItem : UIObject
    {
        private GameObject menuFrame1 = null;
        private GameObject menuFrame2 = null;
        private GameObject menuDisabled = null;

        private static Material defaultMaterial = Resources.Load<Material>(@"common-mat");


        public void Initialize(IGameInterface gameInterface, MenuItemId menuItemId)
        {
            this.gameObject.name = string.Format(@"menu_{0}", menuItemId.GetHashCode());

            menuFrame1 = AssetManager.Instance().InstantiateMenuItemGO(this.transform, menuItemId, 1);
            /// menuFrame2 = AssetManager.Instance().InstantiateMenuItem(this.transform, menuItemId, 2);
            /// menuDisabled = AssetManager.Instance().InstantiateMenuItem(this.transform, menuItemId, 3);

            var box = this.gameObject.AddComponent<BoxCollider>();
            box.size = new Vector3(2.0f, 2.0f, 2.0f);
            box.center = new Vector3(0f, 1f, 0f);
        }


    }
}