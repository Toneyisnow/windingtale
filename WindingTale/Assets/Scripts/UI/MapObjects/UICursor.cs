using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WindingTale.UI.Common;
using WindingTale.UI.Components;
using static WindingTale.UI.Common.Constants;

namespace WindingTale.UI.MapObjects
{
    public class UICursor : UIObject
    {
        private GameObject cursorDefault = null;
        private GameObject cursorRange0 = null;
        private GameObject cursorRange1 = null;
        private GameObject cursorRange2 = null;

        private IGameInterface gameInterface = null;
        
        public void Initialize(IGameInterface gameInterface)
        {
            this.gameInterface = gameInterface;

            cursorDefault = AssetManager.Instance().InstantiateCursorGO(this.transform, CursorType.Default);
            //cursorRange0 = GameObjectExtension.LoadCursor(CursorType.Range0, this.transform);
            //cursorRange1 = GameObjectExtension.LoadCursor(CursorType.Range1, this.transform);
            //cursorRange2 = GameObjectExtension.LoadCursor(CursorType.Range2, this.transform);

            //cursorRange0.SetActive(false);

            var box = this.gameObject.AddComponent<BoxCollider>();
            box.size = new Vector3(2.0f, 0.3f, 2.0f);
            box.center = new Vector3(0f, 1f, 0f);
        }

        protected override void OnTouched()
        {
            gameInterface.TouchCursor();

        }
    }
}