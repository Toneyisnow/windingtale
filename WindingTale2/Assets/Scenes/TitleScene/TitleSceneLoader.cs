using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using WindingTale.UI.Utils;

namespace WindingTale.Scenes.TitleScene
{
    public class TitleSceneLoader : MonoBehaviour
    {
        // Start is called before the first frame update
        void Start()
        {
            

        }

        // Update is called once per frame
        void Update()
        {

        }


        public void OnStartNewGame()
        {
            GlobalVariables.Set("ChapterId", 1);
            SceneManager.LoadScene("GameFieldScene", LoadSceneMode.Single);
        }

        public void OnLoadGame()
        {

        }


        public void OnContinueGame()
        {

        }



    }
}