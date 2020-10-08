using System;
using TMPro;
using UnityEngine;
using WindingTale.UI.Common;

namespace WindingTale.UI.Dialogs
{
    public abstract class CanvasDialog : MonoBehaviour
    {
        protected Canvas canvas = null;

        public virtual void Initialize(Canvas root)
        {
            this.canvas = root;
            this.transform.parent = canvas.transform;
        }

        protected GameObject AddSubDialog(string resourceName, Transform parent, Vector3 position, Vector3 scale, Action action = null)
        {
            GameObject prefab = Resources.Load<GameObject>(resourceName);
            GameObject control = GameObject.Instantiate(prefab);
            control.transform.parent = parent;
            control.layer = 5; //UI

            GameObject defaultObject = control.transform.Find(@"default").gameObject;
            defaultObject.layer = 5;
            control.transform.localPosition = position;
            control.transform.localScale = scale;
            control.transform.localRotation = Quaternion.Euler(90, 0, 180);

            Renderer renderer = defaultObject.GetComponent<Renderer>();
            BoxCollider collider = control.AddComponent<BoxCollider>();

            float scaleFactor = 0.15f * scale.x;
            collider.size = new Vector3((float)(renderer.bounds.size.x / scaleFactor), 1, (float)(renderer.bounds.size.y / scaleFactor));

            if (action != null)
            {
                Clickable clickable = control.AddComponent<Clickable>();
                clickable.Initialize(() => { action(); } );
            }

            return control;
        }

        protected GameObject AddControl(string resourceName, Transform subDialog, Vector3 position, Vector3 scale, Action action = null)
        {
            GameObject prefab = Resources.Load<GameObject>(resourceName);
            GameObject control = GameObject.Instantiate(prefab);
            control.transform.parent = subDialog;
            control.layer = 5; //UI

            GameObject defaultObject = control.transform.Find(@"default").gameObject;
            defaultObject.layer = 5;
            control.transform.localPosition = position;
            control.transform.localScale = scale;
            control.transform.localRotation = Quaternion.Euler(0, 0, 0);

            Renderer renderer = defaultObject.GetComponent<Renderer>();
            BoxCollider collider = control.AddComponent<BoxCollider>();

            //float scaleFactor = 1.8f;
            //collider.size = new Vector3((float)(renderer.bounds.size.x / scaleFactor), 1, (float)(renderer.bounds.size.y / scaleFactor));
            collider.size = new Vector3(2f, 1, 2f);



            if (action != null)
            {
                Clickable clickable = control.AddComponent<Clickable>();
                clickable.Initialize(() => { action(); });
            }

            return control;
        }

        protected GameObject AddText(string textString, Transform subDialog, Vector3 position, Vector3 scale)
        {

            GameObject textPro = new GameObject();
            textPro.transform.parent = subDialog;
            textPro.layer = 5; //UI
            textPro.transform.localPosition = position;
            textPro.transform.localScale = scale;

            TextMeshPro textComp = textPro.AddComponent<TextMeshPro>();
            textComp.text = textString;

            ///Material material = Resources.Load<Material>("Fonts/FontAssets/FZB_Item");
            ///textComp.fontSharedMaterial = material;
            
            TMP_FontAsset fontAssetA = Resources.Load<TMP_FontAsset>("Fonts/FontAssets/FZB_Item");
            textComp.font = fontAssetA;
            

            return textPro;
        }
    }
}