using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using WindingTale.Core.Common;
using WindingTale.Core.Definitions;
using WindingTale.Scenes.GameFieldScene;
using WindingTale.Scenes.GameFreeFieldScene;

/// <summary>
/// The bad ending. Chapter 27 is won without the Sky Key, Youni goes up to the Golden City
/// alone, and the field fades out into this scene (GameMain.OnBadEnding).
///
/// First the party's last lines, chapter 27's conversation 6 (lines 1..7), in the talk
/// dialog one per key press as on the field, under the sky (Endings/EndingSky) spanning the
/// top of the screen. The Golden City (Endings/GoldenCity) fades in high in it before line 1, "看！是。。是
/// 黄金城！"; after that line the screen flashes white twice and the city fades away, and the talk goes on. After the
/// last line the sky goes to black. Then the epilogue rolls up the black
/// screen from below (RollEpilogue), and once it is gone the scene fades out into
/// EndingThemeScene by itself -- or sooner, when the skip bar is held full.
/// </summary>
public class GameEndingScene : MonoBehaviour
{
    /// <summary>This scene's name in the build settings.</summary>
    public const string SceneName = "GameEndingScene";

    /// <summary>The talk dialog -- UI/Dialogs/TalkDialog.prefab, as the free field uses it.</summary>
    public GameObject talkDialogPrefab = null;

    /// <summary>The scene's canvas, which the dialog is put up on.</summary>
    public GameObject canvasObject = null;

    /// <summary>Time the scene takes to come up out of black, in seconds.</summary>
    public float fadeInDuration = 1.0f;

    /// <summary>Time the scene takes to go back to black on the way out, in seconds.</summary>
    public float fadeOutDuration = 3.0f;

    private const int ChapterId = 27;
    private const int ConversationId = 6;
    private const int FirstLine = 1;
    private const int LastLine = 7;

    /// <summary>The dialog's scale on the canvas -- the free field's.</summary>
    private const float DialogScale = 0.9f;

    /// <summary>The epilogue, as the original rolls it after the bad ending's talk.</summary>
    private static readonly string[] EpilogueLines =
    {
        "在亚克斯王国宫廷中",
        "盛大的庆祝宴会结束之后，",
        "离别的时刻终于来到了。",
        "昔日的战友们自此将互道珍重，",
        "各奔前程。",
        "在往日的回忆中，",
        "在未来的岁月里，",
        "或许很难有再相见的机会，",
        "但这段冒险的回忆仍会长存，",
        "在每个人的心里……",
    };

    /// <summary>The epilogue's line pitch and font size, in canvas units (the canvas is scaled from 800 x 600).</summary>
    private const float EpilogueLineSpacing = 56f;
    private const float EpilogueFontSize = 34f;

    /// <summary>The original's pale blue lettering.</summary>
    private static readonly Color EpilogueColor = new Color(0.68f, 0.75f, 1.0f);

    /// <summary>
    /// The epilogue fades in over this long as it starts to roll, and out over this long at
    /// the end. The fade-out starts as the last four lines come up to the top, and ends just
    /// as the last line reaches it -- so the roll covers four lines in this time.
    /// </summary>
    private const float EpilogueFadeSeconds = 5f;
    private const int EpilogueFadeOutLines = 4;

    private const string SkyPath = "Endings/EndingSky";
    private const string GoldenCityPath = "Endings/GoldenCity";

    /// <summary>The line the Golden City is seen on.</summary>
    private const int GoldenCityLine = 1;

    /// <summary>The sky's and the city's sizes in the original's pixels.</summary>
    private static readonly Vector2 SkyOriginalSize = new Vector2(320f, 132f);
    private static readonly Vector2 GoldenCityOriginalSize = new Vector2(86f, 81f);

    /// <summary>
    /// The sky spans the screen's width and keeps its shape; of the height it leaves free,
    /// this share is left above it and the rest below.
    /// </summary>
    private const float SkyTopShareOfSpace = 1f / 3f;

    /// <summary>Where the city's centre floats, as a share of the sky's height from its bottom.</summary>
    private const float GoldenCityHeightInSky = 0.6f;

    // Timings, in seconds.
    private const float GoldenCityFadeInSeconds = 2f;
    /// <summary>The white flashes over the whole screen after the line: how many, over how long, and how much of each is the rise to full white.</summary>
    private const int GoldenCityFlashes = 2;
    private const float GoldenCityFlashSeconds = 2f;
    private const float FlashRiseShare = 0.15f;
    private const float GoldenCityFadeOutSeconds = 5f;
    private const float SkyFadeOutSeconds = 1f;

    private ScreenFader fader = null;

    private GameObject sky = null;
    private RawImage goldenCity = null;

    private FreeFieldTalkQueue talkQueue = null;

    /// <summary>Set once the talk is over and the epilogue has started rolling.</summary>
    private bool epilogueStarted = false;

    /// <summary>Set once the way out has been taken, so a second key cannot load twice.</summary>
    private bool isLeaving = false;

    void Start()
    {
        CreateSky();

        GameObject dialog = Instantiate(talkDialogPrefab, canvasObject.transform, false);
        dialog.transform.localScale = Vector3.one * DialogScale;
        dialog.SetActive(false);

        talkQueue = new FreeFieldTalkQueue(dialog, ChapterId);

        // The talk starts once the black has cleared, so a key still held from the
        // battle's last line cannot skip the first one.
        fader = ScreenFader.Create(1.0f);
        fader.FadeTo(0.0f, fadeInDuration, PushLines);
    }

    /// <summary>The lines of the conversation, each with its speaker's portrait, from the bottom like a friend's.</summary>
    private void PushLines()
    {
        ChapterDefinition chapter = DefinitionStore.Instance.LoadChapter(ChapterId);
        for (int line = FirstLine; line <= LastLine; line++)
        {
            int creatureId = chapter.GetConversationCreatureId(
                Conversation.Create(ChapterId, ConversationId, line));
            CreatureDefinition speaker = DefinitionStore.Instance.GetCreatureDefinition(creatureId);
            int animationId = speaker != null ? speaker.AnimationId : 0;

            if (line == GoldenCityLine)
            {
                PushRoutine(ShowGoldenCity);
            }

            talkQueue.Push(ConversationId, line, animationId, GameCanvas.DialogPosition.Bottom);

            if (line == GoldenCityLine)
            {
                PushRoutine(LoseGoldenCity);
            }
        }

        PushRoutine(CloseSky);
    }

    /// <summary>Queues a routine between the lines; the talk waits for it to finish.</summary>
    private void PushRoutine(Func<IEnumerator> routine)
    {
        bool done = false;
        talkQueue.PushAction(() => StartCoroutine(RunThen(routine(), () => done = true)), () => done);
    }

    private static IEnumerator RunThen(IEnumerator routine, Action then)
    {
        yield return routine;
        then();
    }

    /// <summary>
    /// The sky across the screen's width, on black, and the Golden City in it, not yet seen
    /// -- both under the talk dialog. Everything is laid out by anchors, so it holds for any
    /// screen shape: the sky's height follows its width, and the point a third of the way
    /// down it sits a third of the way down the screen, which leaves a third of the free
    /// height above it and two thirds below. The city is the sky's child, sized in the
    /// sky's own pixels.
    /// </summary>
    private void CreateSky()
    {
        sky = new GameObject("Sky", typeof(RectTransform), typeof(RawImage), typeof(AspectRatioFitter));
        sky.transform.SetParent(canvasObject.transform, false);
        sky.transform.SetAsFirstSibling();
        RectTransform skyRect = sky.GetComponent<RectTransform>();
        float anchorY = 1f - SkyTopShareOfSpace;
        skyRect.anchorMin = new Vector2(0f, anchorY);
        skyRect.anchorMax = new Vector2(1f, anchorY);
        skyRect.pivot = new Vector2(0.5f, anchorY);
        skyRect.offsetMin = skyRect.offsetMax = Vector2.zero;

        RawImage skyImage = sky.GetComponent<RawImage>();
        skyImage.texture = Resources.Load<Texture2D>(SkyPath);
        skyImage.raycastTarget = false;
        AspectRatioFitter fitter = sky.GetComponent<AspectRatioFitter>();
        fitter.aspectMode = AspectRatioFitter.AspectMode.WidthControlsHeight;
        fitter.aspectRatio = SkyOriginalSize.x / SkyOriginalSize.y;

        GameObject city = new GameObject("GoldenCity", typeof(RectTransform), typeof(RawImage));
        city.transform.SetParent(sky.transform, false);
        RectTransform cityRect = city.GetComponent<RectTransform>();
        Vector2 half = new Vector2(GoldenCityOriginalSize.x / SkyOriginalSize.x, GoldenCityOriginalSize.y / SkyOriginalSize.y) * 0.5f;
        Vector2 centre = new Vector2(0.5f, GoldenCityHeightInSky);
        cityRect.anchorMin = centre - half;
        cityRect.anchorMax = centre + half;
        cityRect.offsetMin = cityRect.offsetMax = Vector2.zero;

        goldenCity = city.GetComponent<RawImage>();
        goldenCity.texture = Resources.Load<Texture2D>(GoldenCityPath);
        goldenCity.raycastTarget = false;
        goldenCity.color = new Color(1f, 1f, 1f, 0f);
    }

    /// <summary>The Golden City fades in, before the line that sees it.</summary>
    private IEnumerator ShowGoldenCity()
    {
        yield return FadeGoldenCity(1f, GoldenCityFadeInSeconds);
    }

    /// <summary>
    /// After the line, white light fills the screen twice -- each time up to full white
    /// quickly and fading back down -- and then the city fades away.
    /// </summary>
    private IEnumerator LoseGoldenCity()
    {
        GameObject flashObject = new GameObject("WhiteFlash", typeof(RectTransform), typeof(Image));
        flashObject.transform.SetParent(canvasObject.transform, false);
        flashObject.transform.SetAsLastSibling();
        RectTransform flashRect = flashObject.GetComponent<RectTransform>();
        flashRect.anchorMin = Vector2.zero;
        flashRect.anchorMax = Vector2.one;
        flashRect.offsetMin = flashRect.offsetMax = Vector2.zero;
        Image flash = flashObject.GetComponent<Image>();
        flash.raycastTarget = false;

        float flashSeconds = GoldenCityFlashSeconds / GoldenCityFlashes;
        float rise = flashSeconds * FlashRiseShare;
        for (int i = 0; i < GoldenCityFlashes; i++)
        {
            for (float t = 0f; t < flashSeconds; t += Time.deltaTime)
            {
                float alpha = t < rise ? t / rise : 1f - (t - rise) / (flashSeconds - rise);
                flash.color = new Color(1f, 1f, 1f, alpha);
                yield return null;
            }
        }
        Destroy(flashObject);

        yield return FadeGoldenCity(0f, GoldenCityFadeOutSeconds);
    }

    private IEnumerator FadeGoldenCity(float alpha, float seconds)
    {
        float from = goldenCity.color.a;
        for (float t = 0f; t < seconds; t += Time.deltaTime)
        {
            SetGoldenCityAlpha(Mathf.Lerp(from, alpha, t / seconds));
            yield return null;
        }
        SetGoldenCityAlpha(alpha);
    }

    private void SetGoldenCityAlpha(float alpha)
    {
        goldenCity.color = new Color(1f, 1f, 1f, alpha);
    }

    /// <summary>After the last line the sky goes to black, leaving the screen to the epilogue.</summary>
    private IEnumerator CloseSky()
    {
        bool dark = false;
        fader.FadeTo(1f, SkyFadeOutSeconds, () => dark = true);
        while (!dark)
        {
            yield return null;
        }

        Destroy(sky);
        fader.FadeTo(0f, 0f);
    }

    void Update()
    {
        if (isLeaving || talkQueue == null || fader.IsFading)
        {
            return;
        }

        talkQueue.Update();
        if (talkQueue.IsBusy || talkQueue.WasClosedThisFrame())
        {
            return;
        }

        if (!epilogueStarted)
        {
            epilogueStarted = true;
            StartCoroutine(RollEpilogue());
        }
    }

    /// <summary>Fades out into EndingThemeScene -- once the epilogue has rolled off, or when it is skipped.</summary>
    private void Leave()
    {
        if (isLeaving)
        {
            return;
        }

        isLeaving = true;
        fader.FadeTo(1.0f, fadeOutDuration, () =>
        {
            SceneManager.LoadScene(EndingThemeScene.SceneName, LoadSceneMode.Single);
        });
    }

    /// <summary>
    /// Rolls the epilogue up the black screen: the first line comes in at the bottom edge
    /// and the roll stops as the last line reaches the top, the whole of it fading in over
    /// the first EpilogueFadeSeconds and out over the last.
    /// </summary>
    private IEnumerator RollEpilogue()
    {
        // The opening's "按空格跳过" bar, for as long as the epilogue rolls: held full, it
        // ends the scene the same way the epilogue's end does.
        FreeFieldSkipBar skipBar = FreeFieldSkipBar.Create(canvasObject.transform, Leave);

        RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
        float screenHeight = canvasRect.rect.height;

        // The block hangs from its top edge, which is the first line's top; it starts at the
        // bottom of the screen.
        GameObject block = new GameObject("Epilogue", typeof(RectTransform), typeof(CanvasGroup));
        block.transform.SetParent(canvasObject.transform, false);
        RectTransform blockRect = block.GetComponent<RectTransform>();
        blockRect.anchorMin = new Vector2(0.5f, 0f);
        blockRect.anchorMax = new Vector2(0.5f, 0f);
        blockRect.pivot = new Vector2(0.5f, 1f);
        blockRect.sizeDelta = new Vector2(canvasRect.rect.width, EpilogueLines.Length * EpilogueLineSpacing);
        blockRect.anchoredPosition = Vector2.zero;

        CanvasGroup group = block.GetComponent<CanvasGroup>();
        group.alpha = 0f;
        group.blocksRaycasts = false;

        TMP_FontAsset font = ShoppingRecordDialog.GetRecordFont();
        for (int i = 0; i < EpilogueLines.Length; i++)
        {
            GameObject lineObject = new GameObject("Line" + (i + 1), typeof(RectTransform), typeof(TextMeshProUGUI));
            lineObject.transform.SetParent(block.transform, false);

            RectTransform lineRect = lineObject.GetComponent<RectTransform>();
            lineRect.anchorMin = new Vector2(0f, 1f);
            lineRect.anchorMax = new Vector2(1f, 1f);
            lineRect.pivot = new Vector2(0.5f, 1f);
            lineRect.sizeDelta = new Vector2(0f, EpilogueLineSpacing);
            lineRect.anchoredPosition = new Vector2(0f, -i * EpilogueLineSpacing);

            TextMeshProUGUI text = lineObject.GetComponent<TextMeshProUGUI>();
            if (font != null)
            {
                text.font = font;
            }
            text.text = EpilogueLines[i];
            text.fontSize = EpilogueFontSize;
            text.color = EpilogueColor;
            text.alignment = TextAlignmentOptions.Center;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.raycastTarget = false;
        }

        // From the first line's top at the bottom edge to the last line's top at the top edge.
        float distance = screenHeight + (EpilogueLines.Length - 1) * EpilogueLineSpacing;
        float speed = EpilogueFadeOutLines * EpilogueLineSpacing / EpilogueFadeSeconds;
        float duration = distance / speed;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed = Mathf.Min(elapsed + Time.deltaTime, duration);
            blockRect.anchoredPosition = new Vector2(0f, elapsed * speed);

            float fadeIn = Mathf.Clamp01(elapsed / EpilogueFadeSeconds);
            float fadeOut = Mathf.Clamp01((duration - elapsed) / EpilogueFadeSeconds);
            group.alpha = Mathf.Min(fadeIn, fadeOut);

            yield return null;
        }

        Destroy(block);
        if (skipBar != null)
        {
            Destroy(skipBar.gameObject);
        }
        Leave();
    }
}
