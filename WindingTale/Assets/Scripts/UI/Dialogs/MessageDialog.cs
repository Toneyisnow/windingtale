using Assets.Scripts.Common;
using Assets.Scripts.UI.Common;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using WindingTale.Common;
using WindingTale.UI.Common;

namespace WindingTale.UI.CanvasControls
{
    public class MessageDialog : MonoBehaviour
    {
        public enum MessageDialogPosition
        {
            UP = 1,
            DOWN = 2,
        }

        public Canvas canvas;
        private DatoControl datoControl = null;
        private Action<int> onClickCallback = null;


        private float displayInterval = 0.08f;

        private int displayLength = 0;
        private string localizedMessage = string.Empty;
        private TextMeshPro textObj = null;

        private float lastTime;
        private bool isTalking = false;

        private MessageDialogPosition dialogPosition = MessageDialogPosition.DOWN;


        private static Vector2 GetBasePosition(MessageDialogPosition pos)
        {
            if (pos == MessageDialogPosition.UP)
            {
                return new Vector2(0, 150);
            }
            else
            {
                return new Vector2(0, -150);
            }
        }

        private static Vector2 GetDatoPosition(MessageDialogPosition pos)
        {
            if (pos == MessageDialogPosition.UP)
            {
                return new Vector2(230, 150);
            }
            else
            {
                return new Vector2(-230, -150);
            }
        }

        private static Vector3 GetTextPosition(MessageDialogPosition pos)
        {
            if (pos == MessageDialogPosition.UP)
            {
                return new Vector3(-60, 130, -50);
            }
            else
            {
                return new Vector3(60, -160, 0);
            }
        }

        public void Initialize(Camera camera, int animationId, string message, Action<int> callback, int forChapterId = 0)
        {
            this.gameObject.name = "MessageDialog";

            canvas.worldCamera = camera;
            this.onClickCallback = callback;

            if (animationId > 500)
            {
                dialogPosition = MessageDialogPosition.UP;
            }

            GameObject messageBoxBase = this.transform.Find("Canvas/MessageBase").gameObject;
            messageBoxBase.transform.localPosition = GetBasePosition(dialogPosition);
            Clickable clickable = messageBoxBase.AddComponent<Clickable>();
            clickable.Initialize(() => { this.OnClicked(); });

            localizedMessage = message;

            localizedMessage = localizedMessage.Replace("#", "\r\n");
            datoControl = GameObjectExtension.CreateFromPrefab<DatoControl>("Prefabs/DatoControl");
            datoControl.Initialize(canvas, animationId, GetDatoPosition(dialogPosition), (dialogPosition == MessageDialogPosition.DOWN));

            if (forChapterId > 0)
            {
                textObj = FontAssets.ComposeTextMeshObjectForChapter(forChapterId, localizedMessage);
            }
            else
            {
                textObj = FontAssets.ComposeTextMeshObject(localizedMessage);
            }

            Transform textAnchor = this.transform.Find("Canvas/TextAnchor");
            textAnchor.localPosition = GetTextPosition(dialogPosition);

            textObj.transform.parent = textAnchor;
            //textObj.transform.localPosition = new Vector2(190, -50);
            textObj.transform.localPosition = new Vector3(0, 0, 0);
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

        private void OnClicked()
        {
            if (isTalking)
            {
                displayLength = localizedMessage.Length;
                textObj.text = localizedMessage;
            }
            else
            {
                //// Debug.Log("Clicked on MessageDialog!");
                if (this.onClickCallback != null)
                {
                    this.onClickCallback(1);
                }
            }
        }
    }
}
