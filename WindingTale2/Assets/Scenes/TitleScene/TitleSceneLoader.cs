using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using WindingTale.Core.Files;
using WindingTale.Scenes.GameFieldScene;
using WindingTale.UI.Utils;

namespace WindingTale.Scenes.TitleScene
{
    public class TitleSceneLoader : MonoBehaviour
    {
        /// <summary>
        /// The save slot picker shown when Load is pressed. Assigned in the inspector -- the
        /// same ShoppingRecordDialog prefab the shop's bar uses. See ShoppingRecordDialog.
        /// </summary>
        public GameObject recordDialogPrefab = null;

        private bool initialized = false;

        /// <summary>The open slot picker, or null when none is up. Guards against a second one.</summary>
        private GameObject recordDialog = null;

        // Start is called before the first frame update
        void Start()
        {
            
        }

        // Update is called once per frame
        void Update()
        {
            if (!initialized)
            {
                Init();
                initialized = true;
            }
        }

        void Init()
        {
            FindFirstObjectByType<BackgroundMusic>().PlayMusic();

        }


        public void OnStartNewGame()
        {
            GlobalVariables.Set(GameFiledSceneParams.ChapterIdVariableName, 1);

            // The flag is static and survives scene loads, so a New Game after returning to
            // the title from a continued session has to clear it explicitly.
            GameFiledSceneParams.isContinue = false;

            //// SceneManager.LoadScene("GameFieldScene", LoadSceneMode.Single);

            FindFirstObjectByType<SceneTransition>().LoadScene("GameFieldScene");
            FindFirstObjectByType<BackgroundMusic>().StopMusic();
        }

        /// <summary>
        /// Puts up the save slot picker in the lower half of the title screen. Picking a
        /// slot reads that save and carries the party into the village it left off in;
        /// backing out (Esc / Backspace) just closes the picker and leaves the title up.
        /// </summary>
        public void OnLoadGame()
        {
            if (recordDialog != null)
            {
                // Already open -- ignore a second Load click.
                return;
            }

            if (recordDialogPrefab == null)
            {
                Debug.LogWarning("Title scene has no record dialog prefab to show.");
                return;
            }

            GameObject dialogObject = Instantiate(recordDialogPrefab);

            // Draw it above the title's own buttons, whatever their sorting turns out to be.
            Canvas canvas = dialogObject.GetComponentInChildren<Canvas>();
            if (canvas != null)
            {
                canvas.sortingOrder = 100;
            }

            ShoppingRecordDialog dialog = dialogObject.GetComponent<ShoppingRecordDialog>();
            if (dialog == null)
            {
                Debug.LogWarning("Record dialog prefab has no ShoppingRecordDialog component.");
                Destroy(dialogObject);
                return;
            }

            recordDialog = dialogObject;

            // Load has nothing to read from an empty slot, so confirming one does nothing.
            dialog.Init(OnLoadSlotSelected, allowEmpty: false);
        }

        /// <summary>
        /// The slot picker has reported a choice. A negative index is a cancel -- the picker
        /// closes and nothing else happens. An empty slot has nothing to read, so the picker
        /// is left up to try another. A real save is read and followed into the village.
        /// </summary>
        private void OnLoadSlotSelected(int recordIndex)
        {
            if (recordIndex < 0)
            {
                CloseRecordDialog();
                return;
            }

            GameRecord record = GameRecordManager.LoadFromFile(recordIndex);
            if (record == null)
            {
                Debug.LogWarning("Load slot " + recordIndex + " is empty; nothing to read.");
                return;
            }

            CloseRecordDialog();

            // The village picks the party up out of GlobalVariables on the way in, the same
            // handover the shop and the field scene use.
            GlobalVariables.Set(VillageScene.RecordVariableName, record);

            FindFirstObjectByType<SceneTransition>().LoadScene("VillageScene");
            FindFirstObjectByType<BackgroundMusic>().StopMusic();
        }

        private void CloseRecordDialog()
        {
            if (recordDialog != null)
            {
                Destroy(recordDialog);
                recordDialog = null;
            }
        }


        /// <summary>
        /// 读取战场战况: resumes the saved battle. Identical to the in-battle record menu's
        /// Load -- both end up in GameMain.ContinueFromRecord, which loads the saved
        /// chapter's static data and then lays the saved battle state over it.
        /// </summary>
        public void OnContinueGame()
        {
            if (!GameMapRecordManager.HasSavedGame(GameMain.SaveRecordName))
            {
                Debug.LogWarning("No saved game to continue.");
                return;
            }

            // GameMain reads this on Start to load the .sav instead of beginning a chapter.
            GameFiledSceneParams.isContinue = true;

            FindFirstObjectByType<SceneTransition>().LoadScene("GameFieldScene");
            FindFirstObjectByType<BackgroundMusic>().StopMusic();
        }



    }
}