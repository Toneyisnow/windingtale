using UnityEngine;
using System.Collections;

public class BackgroundMusic : MonoBehaviour
{
    public AudioClip backgroundMusic;
    private AudioSource audioSource;
    public float fadeDuration = 1.5f; // ���ֵ���/����ʱ��

    void Awake()
    {
        // ȷ�����ֹ������ڳ����л�ʱ��������
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        audioSource.clip = backgroundMusic;
        audioSource.loop = true;
        audioSource.playOnAwake = true;
        audioSource.volume = 0f; // ��ʼ������Ϊ 0

        //// PlayMusic(true);
    }

    public void SetAudioClipName(string clipName)
    {
        backgroundMusic = Resources.Load<AudioClip>(clipName);
        audioSource.clip = backgroundMusic;
    }

    public void PlayMusic(bool fadeIn = false)
    {
        if (!audioSource.isPlaying)
        {
            audioSource.Play();
            if (fadeIn)
            {
                StartCoroutine(FadeIn());
            }
            else
            {
                audioSource.volume = 0.5f; // ֱ����ΪĬ������
            }
        }
    }

    public void StopMusic()
    {
        StartCoroutine(FadeOutAndStop());
    }

    private IEnumerator FadeIn()
    {
        float elapsedTime = 0f;
        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            audioSource.volume = Mathf.Lerp(0f, 0.5f, elapsedTime / fadeDuration);
            yield return null;
        }
    }

    private IEnumerator FadeOutAndStop()
    {
        float startVolume = audioSource.volume;
        float elapsedTime = 0f;

        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            audioSource.volume = Mathf.Lerp(startVolume, 0f, elapsedTime / fadeDuration);
            yield return null;
        }

        audioSource.Stop();
        audioSource.volume = startVolume; // ��λ�����Ա��´β���
    }
}
