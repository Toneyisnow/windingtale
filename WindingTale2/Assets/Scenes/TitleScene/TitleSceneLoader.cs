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

        /// <summary>
        /// The yes / no question asked before a new game starts -- the shop's
        /// ShoppingConfirmDialog prefab, as the quick village uses it.
        /// </summary>
        public GameObject confirmDialogPrefab = null;

        /// <summary>
        /// Asked when Start is pressed. Drawn in the record font (InitLiteral): the baked
        /// message atlas is not guaranteed to carry its glyphs.
        /// </summary>
        private const string NewGameConfirmText = "确认要开始新游戏吗？";

        /// <summary>The free-field map a new game opens on: the throne hall (Map 32).</summary>
        private const int NewGameFreeFieldMapId = 32;

        private bool initialized = false;

        /// <summary>The open slot picker, or null when none is up. Guards against a second one.</summary>
        private GameObject recordDialog = null;

        /// <summary>The open new-game question, or null. Guards against a second one.</summary>
        private GameObject confirmDialog = null;

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


        /// <summary>
        /// Start: asks "确认要开始新游戏吗？" first. Yes opens the new game on the throne hall
        /// (GameFreeFieldScene, Map 32); No closes the question and leaves the title up.
        /// </summary>
        public void OnStartNewGame()
        {
            if (confirmDialog != null || recordDialog != null)
            {
                // A question or the slot picker is already up.
                return;
            }

            if (confirmDialogPrefab == null)
            {
                Debug.LogWarning("Title scene has no confirm dialog prefab to show.");
                return;
            }

            GameObject dialogObject = Instantiate(confirmDialogPrefab);

            // Draw it above the title's own buttons, as the slot picker is.
            Canvas canvas = dialogObject.GetComponentInChildren<Canvas>();
            if (canvas != null)
            {
                canvas.sortingOrder = 100;
            }

            ShoppingConfirmDialog dialog = dialogObject.GetComponent<ShoppingConfirmDialog>();
            if (dialog == null)
            {
                Debug.LogWarning("Confirm dialog prefab has no ShoppingConfirmDialog component.");
                Destroy(dialogObject);
                return;
            }

            confirmDialog = dialogObject;
            dialog.InitLiteral(NewGameConfirmText, OnNewGameConfirmed);
        }

        private void OnNewGameConfirmed(bool yes)
        {
            if (confirmDialog != null)
            {
                Destroy(confirmDialog);
                confirmDialog = null;
            }

            if (!yes)
            {
                return;
            }

            GlobalVariables.Set(GameFiledSceneParams.ChapterIdVariableName, 1);
            GlobalVariables.Set(GameFreeFieldScene.GameFreeFieldScene.MapIdVariableName, NewGameFreeFieldMapId);

            // The flag is static and survives scene loads, so a New Game after returning to
            // the title from a continued session has to clear it explicitly.
            GameFiledSceneParams.isContinue = false;

            FindFirstObjectByType<SceneTransition>().LoadScene(GameFreeFieldScene.GameFreeFieldScene.SceneName);
            FindFirstObjectByType<BackgroundMusic>().StopMusic();
        }

        /// <summary>
        /// Puts up the save slot picker in the lower half of the title screen. Picking a
        /// slot reads that save and carries the party into the village it left off in;
        /// backing out (Esc / Backspace) just closes the picker and leaves the title up.
        /// </summary>
        public void OnLoadGame()
        {
            if (recordDialog != null || confirmDialog != null)
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

            FindFirstObjectByType<SceneTransition>().LoadScene(QuickVillageScene.VillageSceneNameFor(record.ChapterId));
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