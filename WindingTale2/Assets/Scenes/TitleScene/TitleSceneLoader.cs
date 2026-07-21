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
        private bool initialized = false;

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
            GlobalVariables.Set("ChapterId", 1);

            // The flag is static and survives scene loads, so a New Game after returning to
            // the title from a continued session has to clear it explicitly.
            GameFiledSceneParams.isContinue = false;

            //// SceneManager.LoadScene("GameFieldScene", LoadSceneMode.Single);

            FindFirstObjectByType<SceneTransition>().LoadScene("GameFieldScene");
            FindFirstObjectByType<BackgroundMusic>().StopMusic();
        }

        public void OnLoadGame()
        {

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