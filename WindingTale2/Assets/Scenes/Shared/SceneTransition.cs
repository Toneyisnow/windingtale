using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class SceneTransition : MonoBehaviour
{
    public Image fadeImage; // UI Image ���ڹ���Ч��
    public float fadeDuration = 0.8f; // �������ʱ��

    private void Awake()
    {
        // The fade overlay lives on its own Canvas. Force that Canvas to render above every
        // other Canvas in the scene, otherwise a same-sorting-order Canvas (e.g. the title UI)
        // draws on top and the black fade only darkens the area behind it -- leaving the title
        // image and buttons fully bright. Rendering on top makes the whole scene fade out.
        if (fadeImage != null && fadeImage.canvas != null)
        {
            Canvas fadeCanvas = fadeImage.canvas;
            fadeCanvas.overrideSorting = true;
            fadeCanvas.sortingOrder = short.MaxValue;
        }
    }

    private void Start()
    {
        StartCoroutine(FadeIn()); // ������ʼʱ����
    }

    public void LoadScene(string sceneName)
    {
        StartCoroutine(FadeOut(sceneName));
    }

    private IEnumerator FadeIn()
    {
        float elapsedTime = 0f;
        Color color = fadeImage.color;

        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            color.a = Mathf.Lerp(1f, 0f, elapsedTime / fadeDuration);
            fadeImage.color = color;
            yield return null;
        }

        fadeImage.gameObject.SetActive(false);
    }

    private IEnumerator FadeOut(string sceneName)
    {
        float elapsedTime = 0f;
        Color color = fadeImage.color;
        fadeImage.gameObject.SetActive(true);

        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            color.a = Mathf.Lerp(0f, 1f, elapsedTime / fadeDuration);
            fadeImage.color = color;
            yield return null;
        }

        SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
    }
}
