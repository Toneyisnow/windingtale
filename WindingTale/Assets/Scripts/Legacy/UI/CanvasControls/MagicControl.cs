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
    public class MagicControl : CanvasControl
    {
        private MagicDefinition magic = null;

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

        public void Initialize(Canvas canvas, int magicId, Action onClicked = null)
        {
            this.transform.parent = canvas.transform;
            this.indicator = this.transform.Find("SelectionIndicator");
            SetSelected(false);

            // canvas.worldCamera = camera;
            // this.canvas = canvas;

            this.magic = DefinitionStore.Instance.GetMagicDefinition(magicId);
            if (this.magic == null)
            {
                throw new ArgumentException("Cannot find magic definition with ID=" + magicId);
            }

            if (onClicked != null)
            {
                Clickable clickable = indicator.GetComponent<Clickable>();
                clickable.Initialize(onClicked);
            }

            // Name
            string name = LocalizedStrings.GetMagicName(magicId);
            TextMeshPro text = RenderText(name, "Name", FontAssets.FontSizeType.Normal);
            text.transform.localScale = new Vector3(7, 7, 7);

            // Description
            string description = GetMagicDescription(this.magic);
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