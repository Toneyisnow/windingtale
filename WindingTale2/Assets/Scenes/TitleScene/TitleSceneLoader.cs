using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using WindingTale.Core.Files;
using WindingTale.Scenes.GameFieldScene;
using WindingTale.UI.Audio;
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

        /// <summary>The title's three buttons, left to right on screen: Start, Load, Continue.</summary>
        private List<TitleButton> buttons = new List<TitleButton>();

        /// <summary>The button the keyboard is on, an index into <see cref="buttons"/>.</summary>
        private int selectedIndex = 0;

        /// <summary>
        /// The frame a dialog last closed on. The key that closed it must not also press the
        /// button under it -- the dialog may close before or after this script's Update.
        /// </summary>
        private int dialogClosedFrame = -1;

        /// <summary>Set once a button has sent the game on to another scene; the title takes no more keys.</summary>
        private bool leaving = false;

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
                return;
            }

            HandleKeys();
        }

        void Init()
        {
            FindFirstObjectByType<BackgroundMusic>().PlayMusic();

            InitButtons();
        }

        /// <summary>
        /// Finds the three buttons and puts the keyboard on the first that has something
        /// behind it: Continue when there is a battle to resume, else Load when there is any
        /// save, else Start.
        /// </summary>
        private void InitButtons()
        {
            buttons = new List<TitleButton>(FindObjectsByType<TitleButton>(FindObjectsSortMode.None));
            buttons.Sort((a, b) => a.GetComponent<RectTransform>().anchoredPosition.x
                .CompareTo(b.GetComponent<RectTransform>().anchoredPosition.x));
            if (buttons.Count == 0)
            {
                return;
            }

            foreach (TitleButton button in buttons)
            {
                button.Hovered = OnButtonHovered;
            }

            int startIndex = 0;
            int loadIndex = Mathf.Min(1, buttons.Count - 1);
            int continueIndex = Mathf.Min(2, buttons.Count - 1);
            if (GameMapRecordManager.HasSavedGame(GameMain.SaveRecordName))
            {
                Select(continueIndex);
            }
            else if (GameRecordManager.GetAllFiles().Count > 0)
            {
                Select(loadIndex);
            }
            else
            {
                Select(startIndex);
            }
        }

        /// <summary>
        /// Left / Right move between the buttons, round from either end; Enter or Space presses
        /// the selected one, as a click would. Nothing while a dialog is up -- it has the keys.
        /// </summary>
        private void HandleKeys()
        {
            if (leaving || buttons.Count == 0 || recordDialog != null || confirmDialog != null
                || Time.frameCount - dialogClosedFrame <= 1)
            {
                return;
            }

            // A clicked button stays the EventSystem's selection, and the EventSystem would
            // turn Enter / Space into a second click on it: the keys are this script's alone.
            if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject != null)
            {
                EventSystem.current.SetSelectedGameObject(null);
            }

            if (Input.GetKeyDown(KeyCode.LeftArrow))
            {
                SoundEffects.Play(SoundEffect.DialogCursorMove);
                Select((selectedIndex + buttons.Count - 1) % buttons.Count);
            }
            else if (Input.GetKeyDown(KeyCode.RightArrow))
            {
                SoundEffects.Play(SoundEffect.DialogCursorMove);
                Select((selectedIndex + 1) % buttons.Count);
            }
            else if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)
                || Input.GetKeyDown(KeyCode.Space))
            {
                buttons[selectedIndex].Press();
            }
        }

        private void Select(int index)
        {
            selectedIndex = index;
            for (int i = 0; i < buttons.Count; i++)
            {
                buttons[i].SetSelected(i == index);
            }
        }

        /// <summary>The mouse coming onto a button moves the keyboard selection there too.</summary>
        private void OnButtonHovered(TitleButton button)
        {
            int index = buttons.IndexOf(button);
            if (index >= 0 && index != selectedIndex)
            {
                Select(index);
            }
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
                dialogClosedFrame = Time.frameCount;
            }

            if (!yes)
            {
                return;
            }

            leaving = true;

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
            leaving = true;

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
                dialogClosedFrame = Time.frameCount;
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
            leaving = true;

            FindFirstObjectByType<SceneTransition>().LoadScene("GameFieldScene");
            FindFirstObjectByType<BackgroundMusic>().StopMusic();
        }



    }
}