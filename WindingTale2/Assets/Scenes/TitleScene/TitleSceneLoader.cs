using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
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
            //// SceneManager.LoadScene("GameFieldScene", LoadSceneMode.Single);

            FindFirstObjectByType<SceneTransition>().LoadScene("GameFieldScene");
            FindFirstObjectByType<BackgroundMusic>().StopMusic();
        }

        public void OnLoadGame()
        {

        }


        public void OnContinueGame()
        {

        }



    }
}