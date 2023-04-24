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
        private GameObject messageBoxBase;

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
                return new Vector2(230, 0);
            }
            else
            {
                return new Vector2(-230, 0);
            }
        }

        private static Vector3 GetTextPosition(MessageDialogPosition pos)
        {
            if (pos == MessageDialogPosition.UP)
            {
                return new Vector3(-80, -5, -1);
            }
            else
            {
                return new Vector3(60, -5, -1);
            }
        }

        public void Initialize(Camera camera, Vector2 popupPosition, int animationId, string message, Action<int> callback, int forChapterId = 0)
        {
            this.gameObject.name = "MessageDialog";

            canvas.worldCamera = camera;
            this.onClickCallback = callback;

            if (animationId > 500)
            {
                dialogPosition = MessageDialogPosition.UP;
            }

            messageBoxBase = this.transform.Find("Canvas/MessageBase").gameObject;
            messageBoxBase.transform.localPosition = GetBasePosition(dialogPosition);
            Clickable clickable = messageBoxBase.GetComponent<Clickable>();
            clickable.Initialize(() => { this.OnClicked(); });

            PopUp popUp = messageBoxBase.GetComponent<PopUp>();
            popUp.Initialize(popupPosition);

            localizedMessage = message;

            localizedMessage = localizedMessage.Replace("#", "\r\n");
            datoControl = GameObjectExtension.CreateFromPrefab<DatoControl>("Prefabs/DatoControl");
            datoControl.Initialize(messageBoxBase.transform, animationId, GetDatoPosition(dialogPosition), (dialogPosition == MessageDialogPosition.DOWN));

            if (forChapterId > 0)
            {
                textObj = FontAssets.ComposeTextMeshObjectForChapter(forChapterId, localizedMessage);
            }
            else
            {
                textObj = FontAssets.ComposeTextMeshObject(localizedMessage);
            }

            Transform textAnchor = this.transform.Find("Canvas/MessageBase/TextAnchor");
            textAnchor.localPosition = GetTextPosition(dialogPosition);

            textObj.transform.parent = textAnchor;
            //textObj.transform.localPosition = new Vector2(190, -50);
            textObj.transform.localPosition = new Vector3(0, 0, 0);
            textObj.transform.localScale = new Vector3(5, 5, 1);
            textObj.gameObject.layer = 5;
            // textObj.fontSize = 20;

            displayLength = 1;
            textObj.text = localizedMessage.Substring(0, 1);


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
                PopUp popUp = messageBoxBase.GetComponent<PopUp>();
                popUp.Close(() =>
                {
                    //// Debug.Log("Clicked on MessageDialog!");
                    if (this.onClickCallback != null)
                    {
                        this.onClickCallback(1);
                    }
                });
                
            }
        }
    }
}
