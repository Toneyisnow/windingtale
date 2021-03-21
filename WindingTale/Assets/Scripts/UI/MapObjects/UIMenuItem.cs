using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WindingTale.Common;
using WindingTale.UI.Components;
using WindingTale.UI.Controls;
using WindingTale.UI.FieldMap;
using static WindingTale.UI.Common.Constants;

namespace WindingTale.UI.MapObjects
{
    public class UIMenuItem : UIObject
    {
        private GameObject menuFrame1 = null;
        private GameObject menuFrame2 = null;
        private GameObject menuDisabled = null;

        private FDPosition position = null;

        private bool isEnabled = false;
        private bool isSelected = false;

        private int tick = 0;
        private int tickCount = 80;

        private MenuItemId menuItemId;
        private IGameInterface gameInterface = null;

        public void Initialize(IGameInterface gameInterface, MenuItemId menuItemId, FDPosition position, FDPosition showUpPosition, bool enabled, bool selected)
        {
            this.gameInterface = gameInterface;
            
            this.gameObject.name = string.Format(@"menuitem_{0}", menuItemId.GetHashCode());

            this.gameObject.transform.localPosition = FieldTransform.GetGroundPixelPosition(showUpPosition);
            Vector3 menuLocalPosition = FieldTransform.GetGroundPixelPosition(position);
            MenuSliding sliding = this.gameObject.AddComponent<MenuSliding>();
            sliding.Initialize(menuLocalPosition);

            this.isEnabled = enabled;
            this.isSelected = selected;
            this.position = position;

            this.menuItemId = menuItemId;

            
            var box = this.gameObject.AddComponent<BoxCollider>();
            box.size = new Vector3(2.4f, 0.2f, 2.4f);
            box.center = new Vector3(0f, 0.2f, 0f);
        }

        public UIMenuItem[] RelatedMenuItems
        {
            private get; set;
        }

        void Start()
        {
            if (isEnabled)
            {
                menuFrame1 = AssetManager.Instance().InstantiateMenuItemGO(this.transform, menuItemId, 1);
                menuFrame2 = AssetManager.Instance().InstantiateMenuItemGO(this.transform, menuItemId, 2);
            }
            else
            {
                menuDisabled = AssetManager.Instance().InstantiateMenuItemGO(this.transform, menuItemId, 3);
            }
        }

        // Update is called once per frame
        void Update()
        {
            // Animation for the available menu
            if (this.isEnabled && this.isSelected)
            {
                this.tick = (this.tick + 1) % this.tickCount;
                if (this.tick == 0)
                {
                    menuFrame1.SetActive(true);
                    menuFrame2.SetActive(false);
                }
                else if (this.tick == this.tickCount / 2)
                {
                    menuFrame2.SetActive(true);
                    menuFrame1.SetActive(false);
                }
            }
        }

        public void SetSelected(bool value)
        {
            this.isSelected = value;
        }

        protected override void OnTouched()
        {
            Debug.Log("UIMenuItem Touched.");

            if (!this.isEnabled)
            {
                return;
            }

            if (!this.isSelected)
            {
                foreach(UIMenuItem menuItem in RelatedMenuItems)
                {
                    menuItem.SetSelected(false);
                }

                this.isSelected = true;
                return;
            }

            // Do the actual work
            gameInterface.TouchMenu(this.position);
        }
    }
}