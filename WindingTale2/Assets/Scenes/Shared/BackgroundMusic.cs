using UnityEngine;
using System.Collections;

/// <summary>
/// The scene's background music, played on a single looping AudioSource. Every start
/// fades the track up and every stop fades it down, over <see cref="fadeDuration"/>
/// seconds (2 by default); switching tracks fades the old one out and the new one in as
/// one move. Asking for the track that is already playing is ignored, so a turn that
/// re-asserts its music does not restart it.
/// </summary>
public class BackgroundMusic : MonoBehaviour
{
    public AudioClip backgroundMusic;

    /// <summary>Fade in / out time in seconds, applied to every play, stop and switch.</summary>
    public float fadeDuration = 2.0f;

    /// <summary>The volume a faded-in track settles at.</summary>
    private const float PlayVolume = 0.5f;

    private AudioSource audioSource;

    /// <summary>Resource path of the clip currently playing, so a repeat request is a no-op.</summary>
    private string currentClipName = null;

    /// <summary>The running fade, kept so a new request can cut the previous one short.</summary>
    private Coroutine fadeRoutine = null;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        audioSource.clip = backgroundMusic;
        audioSource.loop = true;
        audioSource.playOnAwake = false;
        audioSource.volume = 0f;
    }

    /// <summary>
    /// The background music player in the current scene, created if the scene has none.
    /// The title and battlefield scenes lay one out in the editor; the village and
    /// shopping scenes do not, so this gives them one on demand.
    /// </summary>
    public static BackgroundMusic GetOrCreate()
    {
        BackgroundMusic player = FindFirstObjectByType<BackgroundMusic>();
        if (player == null)
        {
            GameObject holder = new GameObject("BackgroundMusic");
            player = holder.AddComponent<BackgroundMusic>();
        }

        return player;
    }

    public void SetAudioClipName(string clipName)
    {
        backgroundMusic = Resources.Load<AudioClip>(clipName);
        audioSource.clip = backgroundMusic;
        currentClipName = clipName;
    }

    /// <summary>The Resources path of the clip currently playing, or null if none is.</summary>
    public string CurrentClipName
    {
        get { return currentClipName; }
    }

    /// <summary>
    /// How far into the current clip playback has reached, in seconds. Used to save a
    /// track's progress across a scene change so it can be resumed where it left off.
    /// </summary>
    public float CurrentTime
    {
        get { return audioSource != null ? audioSource.time : 0f; }
    }

    /// <summary>
    /// Plays the clip at the given Resources path, fading it in from <paramref name="startTime"/>
    /// seconds in (0 for the beginning). If that clip is already playing it is left alone; an
    /// empty or missing path fades the music out instead.
    /// </summary>
    public void PlayClipByName(string clipName, float startTime = 0f)
    {
        if (string.IsNullOrEmpty(clipName))
        {
            StopMusic();
            return;
        }

        if (audioSource.isPlaying && clipName == currentClipName)
        {
            return;
        }

        AudioClip clip = Resources.Load<AudioClip>(clipName);
        if (clip == null)
        {
            Debug.LogWarning("Cannot load background music clip: " + clipName);
            return;
        }

        currentClipName = clipName;
        StartFade(SwitchTo(clip, startTime));
    }

    /// <summary>Plays the currently assigned clip (see <see cref="backgroundMusic"/>), fading it in.</summary>
    public void PlayMusic(bool fadeIn = true)
    {
        if (audioSource.clip == null || audioSource.isPlaying)
        {
            return;
        }

        audioSource.Play();
        if (fadeIn)
        {
            StartFade(FadeVolume(0f, PlayVolume, fadeDuration));
        }
        else
        {
            audioSource.volume = PlayVolume;
        }
    }

    public void StopMusic()
    {
        if (!audioSource.isPlaying)
        {
            return;
        }

        currentClipName = null;
        StartFade(FadeOutAndStop());
    }

    private void StartFade(IEnumerator fade)
    {
        if (fadeRoutine != null)
        {
            StopCoroutine(fadeRoutine);
        }

        fadeRoutine = StartCoroutine(fade);
    }

    /// <summary>
    /// Fades the current track out (if one is playing), swaps in the new clip, then fades
    /// that in -- one coroutine so the two halves never overlap or race a second switch.
    /// </summary>
    private IEnumerator SwitchTo(AudioClip clip, float startTime)
    {
        if (audioSource.isPlaying)
        {
            yield return FadeVolume(audioSource.volume, 0f, fadeDuration);
            audioSource.Stop();
        }

        backgroundMusic = clip;
        audioSource.clip = clip;
        audioSource.volume = 0f;

        // Resume partway in when asked (a track carried across a scene change); clamp
        // inside the clip so a stale saved time never lands past its end.
        if (startTime > 0f && clip != null && startTime < clip.length)
        {
            audioSource.time = startTime;
        }

        audioSource.Play();
        yield return FadeVolume(0f, PlayVolume, fadeDuration);

        fadeRoutine = null;
    }

    private IEnumerator FadeOutAndStop()
    {
        yield return FadeVolume(audioSource.volume, 0f, fadeDuration);
        audioSource.Stop();
        fadeRoutine = null;
    }

    private IEnumerator FadeVolume(float from, float to, float duration)
    {
        float elapsedTime = 0f;
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            audioSource.volume = Mathf.Lerp(from, to, elapsedTime / duration);
            yield return null;
        }

        audioSource.volume = to;
    }
}
