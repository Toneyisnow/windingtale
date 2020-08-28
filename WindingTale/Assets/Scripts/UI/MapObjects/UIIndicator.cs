using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WindingTale.Common;
using WindingTale.UI.Components;
using WindingTale.UI.FieldMap;

namespace WindingTale.UI.MapObjects
{
    public class UIIndicator : UIObject
    {
        private GameObject indicatorGO = null;

        private IGameInterface gameInterface = null;

        public void Initialize(IGameInterface gameInterface, FDPosition position)
        {
            this.gameInterface = gameInterface;

            this.gameObject.name = "indicator";
            this.gameObject.transform.localPosition = FieldTransform.GetGroundPixelPosition(position);

        }

        void Start()
        {
            indicatorGO = AssetManager.Instance().InstantiateIndicatorGO(this.transform);

            var renderer = indicatorGO.GetComponentInChildren<MeshRenderer>();
            var clr = renderer.material.color;
            renderer.material.color = new Color(clr.r, clr.g, clr.b, 0.3f);

        }
    }
}