using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameTitleScene : MonoBehaviour
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
        SceneManager.LoadScene("GameFieldScene");
    }

    public void OnLoadGame()
    {
        SceneManager.LoadScene("GameLoadScene");
    }

    public void OnContinueGame()
    {
        SceneManager.LoadScene("GameFieldScene");
    }

}
