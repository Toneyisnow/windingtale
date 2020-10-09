using Assets.Scripts.Common;
using Assets.Scripts.UI.Common;
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

        private FontAssets.AssetName fontAssetName;

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

        public void Initialize(Canvas canvas, ConversationId conversation, Action<int> callback = null)
        {
            base.Initialize(canvas);
            this.gameObject.name = "MessageDialog";

            this.chapterId = conversation.ChapterId;
            this.creatureAniId = LocalizedStrings.GetConversationCreatureId(conversation);
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

            if (chapterId >= 0)
            {
                GameObject textAsset = FontAssets.ComposeTextMeshObjectForChapter(chapterId, localizedMessage);
            }
            else
            {
                GameObject textAsset = FontAssets.ComposeTextMeshObject(FontAssets.AssetName.Common, localizedMessage);
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