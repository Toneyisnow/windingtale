using System;
using System.Collections;
using System.Collections.Generic;

using TMPro;
using UnityEngine;
using WindingTale.UI.Common;

namespace WindingTale.UI.CanvasControls
{
    public class PromptDialog : MonoBehaviour
    {
        public Canvas canvas;
        private DatoControl datoControl = null;
        private Action<int> onClickCallback = null;

        private float displayInterval = 0.08f;

        private int displayLength = 0;
        private string localizedMessage = string.Empty;
        private TextMeshPro textObj = null;

        private float lastTime;
        private bool isTalking = false;

        public void Initialize(Camera camera, int animationId, string message, Action<int> callback)
        {
            this.gameObject.name = "PromptDialog";

            canvas.worldCamera = camera;
            this.onClickCallback = callback;


            GameObject messageBoxBase = this.transform.Find("Canvas/MessageBase").gameObject;
            messageBoxBase.transform.localPosition = new Vector2(0, -150);
            Clickable clickable = messageBoxBase.AddComponent<Clickable>();
            clickable.Initialize(() => { this.OnClicked(); });

            localizedMessage = message;

            localizedMessage = localizedMessage.Replace("#", "\r\n");
            datoControl = GameObjectExtension.CreateFromPrefab<DatoControl>("Prefabs/DatoControl");
            datoControl.Initialize(messageBoxBase.transform, animationId, new Vector2(-230, -150), true);

            textObj = FontAssets.ComposeTextMeshObject(localizedMessage);
            

            Transform textAnchor = this.transform.Find("Canvas/TextAnchor");
            textObj.transform.parent = textAnchor;
            textObj.transform.localPosition = new Vector3(0, 0, 0);
            textObj.transform.localScale = new Vector3(5, 5, 1);
            textObj.gameObject.layer = 5;
            // textObj.fontSize = 20;

            displayLength = 4;
            textObj.text = localizedMessage.Substring(0, 4);


        }

        // Start is called before the first frame update
        void Start()
        {
            datoControl.SetTalking(true);
            isTalking = true;
            lastTime = Time.time;
        }

        // Update is called once per frame
        void Update()
        {

        }

        private void OnClicked()
        {
            if (isTalking)
            {
                displayLength = localizedMessage.Length;
                textObj.text = localizedMessage;
            }
        }

        /// <summary>
        /// Result: 0 - No   1 - Yes
        /// </summary>
        /// <param name="result"></param>
        public void OnButtonClicked(int result)
        {
            // If it's still talking, just output the full message
            if (isTalking)
            {
                displayLength = localizedMessage.Length;
                textObj.text = localizedMessage;
                return;
            }

            if (this.onClickCallback != null)
            {
                this.onClickCallback(result);
            }
        }

        private void OnGUI()
        {
            if (!isTalking)
            {
                return;
            }

            if (Time.time - lastTime >= displayInterval)
            {
                OnShowingText();
                lastTime = Time.time;
            }
        }

        private void OnShowingText()
        {
            //// Debug.Log("OnShowingText:" + displayLength);
            if (displayLength++ < localizedMessage.Length)
            {
                textObj.text = localizedMessage.Substring(0, displayLength);
            }
            else
            {
                datoControl.SetTalking(false);
                isTalking = false;
            }
        }

    }
}