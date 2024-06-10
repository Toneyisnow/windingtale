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
            dialog = this.transform.Find("creatureInfoDialog").gameObject;
            dialog.SetActive(true);
            CreatureInfoDialog sDialog = dialog.GetComponent<CreatureInfoDialog>();
            sDialog.Init(onSelected);

        }

        public void CloseDialog()
        {
            dialog.SetActive(false);
        }

        public bool IsDialogOpened()
        {
            return dialog.activeSelf;
        }


    }
}