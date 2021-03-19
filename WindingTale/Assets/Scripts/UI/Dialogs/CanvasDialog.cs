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

            if (action != null)
            {
                BoxCollider collider = control.AddComponent<BoxCollider>();

                //float scaleFactor = 1.8f;
                //collider.size = new Vector3((float)(renderer.bounds.size.x / scaleFactor), 1, (float)(renderer.bounds.size.y / scaleFactor));
                collider.size = new Vector3(2f, 1, 2f);

                Clickable clickable = control.AddComponent<Clickable>();
                clickable.Initialize(() => { action(); });
            }

            return control;
        }

        protected GameObject AddText(int chapterId, string textString, Transform subDialog, Vector3 position, Vector3 scale, Action action = null)
        {
            TextMeshPro textObj = FontAssets.ComposeTextMeshObjectForChapter(chapterId, textString);
            IntialText(textObj.gameObject, subDialog, position, scale, action);

            return textObj.gameObject;
        }

        protected GameObject AddText(string textString, Transform subDialog, Vector3 position, Vector3 scale, Action action = null)
        {
            GameObject textObj = FontAssets.ComposeTextMeshObject(textString).gameObject;
            IntialText(textObj, subDialog, position, scale, action);

            return textObj;
        }

        protected void IntialText(GameObject textObj, Transform subDialog, Vector3 position, Vector3 scale, Action action = null)
        {
            textObj.transform.SetParent(subDialog);
            textObj.layer = 5; //UI
            textObj.transform.localPosition = position;
            textObj.transform.localScale = scale;

            TextMeshProUGUI container = textObj.GetComponent<TextMeshProUGUI>();
            //// container.bounds.size = new Vector3(50, 5, 1);

            RectTransform container2 = textObj.GetComponent<RectTransform>();
            //// container2.

            // container = 120;

            if (action != null)
            {
                BoxCollider collider = textObj.AddComponent<BoxCollider>();

                //float scaleFactor = 1.8f;
                //collider.size = new Vector3((float)(renderer.bounds.size.x / scaleFactor), 1, (float)(renderer.bounds.size.y / scaleFactor));
                collider.size = new Vector3(2f, 1, 2f);

                Clickable clickable = textObj.AddComponent<Clickable>();
                clickable.Initialize(() => { action(); });
            }
        }
    }
}