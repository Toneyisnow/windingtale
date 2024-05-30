using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using WindingTale.Core.Objects;
using WindingTale.UI.Dialogs;

namespace WindingTale.Scenes.GameFieldScene
{

    public class GameCanvas : MonoBehaviour
    {
        public GameObject dialog = null;

        public bool isOpened = false;

        // Start is called before the first frame update
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {

        }

        public void ShowDialog(FDCreature creature, CreatureInfoType infoType, Action<int> onSelected)
        {
            dialog = this.transform.Find("sampleDialog").gameObject;
            dialog.SetActive(true);
            SampleDialog sDialog = dialog.GetComponent<SampleDialog>();
            sDialog.Init(onSelected);

            isOpened = true;
        }

        public void CloseDialog()
        {
            dialog.SetActive(false);
            isOpened = false;
        }

        public bool IsDialogOpened()
        {
            return isOpened;
        }


    }
}