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

            GameMain gameMain = GameObject.FindFirstObjectByType<GameMain>();
            sDialog.Init(creature, infoType, onSelected, gameMain);

        }


        /// <summary>
        /// resolvedText overrides rawText when the line was assembled in code from several
        /// table entries rather than looked up as one (see TalkActivity's multi-message
        /// constructor). Only meaningful for messages, not conversations.
        /// </summary>
        public void ShowTalkDialog(int creatureAnimationid, LocalizedString rawText, bool needConfirm, DialogPosition dialogPosition, Action<int> onSelected, int chapterId = -1, string resolvedText = null)
        {
            dialog = this.transform.Find("TalkDialog").gameObject;
            dialog.SetActive(true);
            TalkDialog sDialog = dialog.GetComponent<TalkDialog>();

            if (chapterId >= 0)
            {
                sDialog.InitConversation(chapterId, creatureAnimationid, rawText, dialogPosition, onSelected);
            }
            else
            {
                sDialog.InitMessage(creatureAnimationid, rawText, needConfirm, dialogPosition, onSelected, resolvedText);
            }
        }

        // Frame on which the last dialog was dismissed. A dialog that closes itself on a
        // key press does so from its own Update, and PlayerInterface's Update may run
        // later in the same frame -- by which time the dialog is gone and the very same
        // key press would be handled a second time as a field input. See
        // WasDialogClosedThisFrame.
        private int lastDialogClosedFrame = -1;

        public void CloseDialog()
        {
            dialog.SetActive(false);
            lastDialogClosedFrame = Time.frameCount;
        }

        public bool IsDialogOpened()
        {
            return dialog.activeSelf;
        }

        /// <summary>
        /// True during the frame a dialog was dismissed. Callers that read raw key state
        /// should stay quiet on that frame so the key that closed the dialog is not also
        /// consumed by whatever is behind it.
        /// </summary>
        public bool WasDialogClosedThisFrame()
        {
            return lastDialogClosedFrame == Time.frameCount;
        }


    }
}