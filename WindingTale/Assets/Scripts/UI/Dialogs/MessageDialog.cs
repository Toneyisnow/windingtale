using Assets.Scripts.Common;
using Assets.Scripts.UI.Common;
using Assets.Scripts.UI.Dialogs;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WindingTale.UI.Dialogs
{

    public class MessageDialog : CanvasDialog
    {
        private int creatureAniId;

        private Action<int> onClickCallback = null;

        private bool needsConfirm = false;

        private string localizedMessage = null;

        private int chapterId = -1;

        public void Initialize(Canvas canvas, int animationId, MessageId message, Action<int> callback = null)
        {
            base.Initialize(canvas);
            this.gameObject.name = "MessageDialog";

            this.creatureAniId = animationId;
            this.localizedMessage = LocalizedStrings.GetMessageString(message);

            this.onClickCallback = callback;

            this.transform.localPosition = new Vector3(0, 0, 0);
            this.transform.localScale = new Vector3(1f, 1f, 1f);
        }

        public void Initialize(Canvas canvas, int animationId, ConversationId conversation, Action<int> callback = null)
        {
            base.Initialize(canvas);
            this.gameObject.name = "MessageDialog";

            this.chapterId = conversation.ChapterId;
            this.creatureAniId = animationId;
            this.localizedMessage = LocalizedStrings.GetConversationString(conversation);

            this.onClickCallback = callback;

            this.transform.localPosition = new Vector3(0, 0, 0);
            this.transform.localScale = new Vector3(1f, 1f, 1f);
        }

        // Start is called before the first frame update
        void Start()
        {
            GameObject messageBox = AddSubDialog(@"Others/MessageBox", this.transform, new Vector3(-5, -126, 0), new Vector3(37, 1, 37),
                () => { this.OnClicked(1); });

            // Add Dato to dato
            GameObject dato = new GameObject();
            dato.transform.SetParent(messageBox.transform);
            var datoControl = dato.AddComponent<DatoControl>();
            datoControl.Initialize(this.creatureAniId, true);
            dato.transform.localPosition = new Vector3(9f, 1, 0);
            dato.transform.localScale = new Vector3(8f, 8f, 1);


            Vector3 textPosition = new Vector3(1.3f, 1, -2f);
            Vector3 textScale = new Vector3(0.4f, 0.4f, 1);
            GameObject textObject;
            if (chapterId >= 0)
            {
                textObject = AddText(chapterId, localizedMessage, messageBox.transform, textPosition, textScale);
            }
            else
            {
                textObject = AddText(localizedMessage, messageBox.transform, textPosition, textScale);
            }

        }

        // Update is called once per frame
        void Update()
        {

        }

        private void OnClicked(int index)
        {
            Debug.Log("Clicked MessageDialog.");

            if (onClickCallback != null)
            {
                onClickCallback(index);
            }
        }


    }
}