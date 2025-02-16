using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Localization;
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


        public void ShowTalkDialog(int creatureAnimationid, LocalizedString rawText, bool needConfirm, DialogPosition dialogPosition, Action<int> onSelected, int chapterId)
        {
            dialog = this.transform.Find("TalkDialog").gameObject;
            dialog.SetActive(true);
            TalkDialog sDialog = dialog.GetComponent<TalkDialog>();

            if (chapterId >= 0)
            {
                sDialog.InitConversation(chapterId, creatureAnimationid, rawText, onSelected);
            }
            else
            {
                sDialog.InitMessage(creatureAnimationid, rawText, onSelected);
            }
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