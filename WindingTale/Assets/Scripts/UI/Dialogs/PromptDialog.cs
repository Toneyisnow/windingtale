using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WindingTale.UI.Dialogs
{
    public class PromptDialog : CanvasDialog
    {
        public void Initialize(Canvas canvas, int animationId, string content, Action<int> callback = null)
        {
            base.Initialize(canvas);
            this.gameObject.name = "PromptDialog";

            this.transform.localPosition = new Vector3(0, 0, 0);
            this.transform.localScale = new Vector3(1f, 1f, 1f);

            GameObject messageBox = AddSubDialog(@"Others/MessageBox", this.transform, new Vector3(-5, -126, 0), new Vector3(37, 1, 37));
            GameObject buttonYes = AddControl(@"Others/ConfirmButtonYes", messageBox.transform, new Vector3(-6.6f, 0, 1), new Vector3(1, 1, 1),
                () => { Debug.Log("Clicked Yes Button in PromptDialog."); callback(1); });
            GameObject buttonNo = AddControl(@"Others/ConfirmButtonNo", messageBox.transform, new Vector3(-10.2f, 0, 1), new Vector3(1, 1, 1),
                () => { Debug.Log("Clicked No Button in PromptDialog."); callback(0); });

        }

        // Start is called before the first frame update
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {

        }
    }
}
