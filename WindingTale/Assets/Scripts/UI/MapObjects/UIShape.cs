using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WindingTale.Common;
using WindingTale.UI.Components;
using WindingTale.UI.FieldMap;

namespace WindingTale.UI.MapObjects
{
    public class UIShape : UIObject
    {
        private IGameInterface gameInterface = null;

        private FDPosition position = null;

        private int chapterId = 0;

        private int shapeIndex = 0;

        private Transform fieldMapRoot = null;

        public UIShape()
        { 
            
        }

        public void Initialize(IGameInterface gameInterface, Transform field, int chapterId, int shapeIndex, FDPosition position)
        {
            this.transform.parent = field;
            this.gameInterface = gameInterface;
            this.fieldMapRoot = field;

            this.chapterId = chapterId;
            this.shapeIndex = shapeIndex;
            this.position = position;

            this.transform.localPosition = FieldTransform.GetShapePixelPosition(position.X, position.Y);
        }

        // Start is called before the first frame update
        void Start()
        {
            GameObject shapeGO = AssetManager.Instance().InstantiateShapeGO(this.transform, chapterId, shapeIndex);

            shapeGO.transform.parent = this.transform;
            shapeGO.transform.localPosition = new Vector3(0, 0, 0);

            var box = this.gameObject.AddComponent<BoxCollider>();
            box.size = new Vector3(2.0f, 0.5f, 2.0f);
            box.center = new Vector3(1.2f, 2.4f, 1.2f);

        }

        protected override void OnTouched()
        {
            Debug.LogFormat("UIShape Clicked: {0},{1}", position.X, position.Y);
            gameInterface.TouchPosition(this.position);
        }
    }
}