using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WindingTale.UI.Dialogs
{

    public class MessageDialog : CanvasDialog
    {

        public void Initialize(Canvas canvas, int animationId, string content, Action<int> callback = null)
        {
            base.Initialize(canvas);
            this.gameObject.name = "MessageDialog";

            this.transform.localPosition = new Vector3(0, 0, 0);
            this.transform.localScale = new Vector3(1f, 1f, 1f);

            GameObject messageBox = AddSubDialog(@"Others/MessageBox", this.transform, new Vector3(-5, -126, 0), new Vector3(37, 1, 37),
                () => { Debug.Log("Clicked MessageDialog."); callback(1); });

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