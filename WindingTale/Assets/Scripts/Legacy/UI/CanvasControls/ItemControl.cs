using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using WindingTale.Core.Definitions;
using WindingTale.Core.Definitions.Items;
using WindingTale.UI.Common;

namespace WindingTale.UI.CanvasControls
{
    public class ItemControl : CanvasControl
    {
        private ItemDefinition item = null;

        /// <summary>
        /// Only valid for Attack/Defend items
        /// </summary>
        private bool isEquiped = false;

        private Transform indicator = null;

        public void SetSelected(bool selected)
        {
            Debug.Log("SetSelected:" + selected);
            Image image = this.indicator.GetComponent<Image>();

            if (selected)
            {
                image.color = new Color(0.5f, 0.5f, 0.5f, 0.5f);
            }
            else
            {
                image.color = new Color(0, 0, 0, 0);
            }
        }

        public void Initialize(Canvas canvas, int itemId, bool isEquiped = false, Action onClicked = null)
        {
            this.transform.parent = canvas.transform;
            this.indicator = this.transform.Find("SelectionIndicator");
            SetSelected(false);

            // canvas.worldCamera = camera;
            // this.canvas = canvas;

            this.item = DefinitionStore.Instance.GetItemDefinition(itemId);
            if (this.item == null)
            {
                throw new ArgumentException("Cannot find item definition with ID=" + itemId);
            }

            this.isEquiped = isEquiped;

            if (onClicked != null)
            {
                Clickable clickable = indicator.GetComponent<Clickable>();
                clickable.Initialize(onClicked);
            }

            // Icon
            Transform iconObject = this.transform.Find("Icon");
            Image iconImage = iconObject.GetComponent<Image>();

            string iconKey = string.Empty;
            if (this.item is AttackItemDefinition)
            {
                iconKey = "Attack";
            }
            else if (this.item is DefendItemDefinition)
            {
                iconKey = "Defend";
            }
            else
            {
                iconKey = "Usable";
            }

            int equiped = isEquiped ? 1 : 0;
            string iconName = string.Format(@"OthersLegacy/Item{0}Icon_{1}", iconKey, equiped);
            iconImage.sprite = LoadSprite(iconName, 24, 20);
            
            // Name
            string name = LocalizedStrings.GetItemName(itemId);
            TextMeshPro text = RenderText(name, "Name", FontAssets.FontSizeType.Normal);
            text.transform.localScale = new Vector3(7, 7, 7);

            // Description
            string description = GetItemDescription(this.item);
            TextMeshPro descriptionText = RenderText(description, "Description", FontAssets.FontSizeType.Normal);
            descriptionText.transform.localScale = new Vector3(5, 5, 5);

        }

        // Start is called before the first frame update
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {

        }
    }
}