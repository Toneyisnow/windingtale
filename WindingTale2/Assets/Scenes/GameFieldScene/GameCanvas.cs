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
        public enum DialogPosition
        {
            Top = 0,
            Bottom = 1,
        }

        public GameObject dialog = null;

        // Start is called before the first frame update
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {

        }

        public void ShowCreatureDialog(FDCreature creature, CreatureInfoType infoType, Action<int> onSelected)
        {
            dialog = this.transform.Find("CreatureInfoDialog").gameObject;
            dialog.SetActive(true);
            CreatureInfoDialog sDialog = dialog.GetComponent<CreatureInfoDialog>();

            sDialog.Init(creature, infoType, onSelected);

        }

        /// <summary>
        /// Show talk dialog, if creatureAnimationid is zero, then it is a system dialog from narrator
        /// </summary>
        /// <param name="creatureAnimationid"></param>
        /// <param name="rawText"></param>
        /// <param name="needConfirm"></param>
        /// <param name="dialogPosition"></param>
        /// <param name="onSelected"></param>
        public void ShowTalkDialog(int creatureAnimationid, string rawText, bool needConfirm, DialogPosition dialogPosition, Action<int> onSelected)
        {
            dialog = this.transform.Find("TalkDialog").gameObject;
            dialog.SetActive(true);
            TalkDialog sDialog = dialog.GetComponent<TalkDialog>();
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