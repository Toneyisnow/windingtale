using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using WindingTale.Core.Common;
using WindingTale.Core.Definitions;
using WindingTale.Core.Files;
using WindingTale.FightObjects;
using WindingTale.UI.Utils;

/// <summary>
/// The ending theme and the companions' epilogues, after either ending: the good one
/// (chapter 31, GameMain.OnGameFinished) or the bad one (GameEndingScene). Ending_Theme_Final
/// plays throughout.
///
/// Each companion the party still has gets a page, in the order of Resources/Endings/Endings.txt
/// (missing ones are skipped), laid out as the original's: the smoke picture, the companion's
/// fight figure standing on the right of it, the portrait (blinking) top-left with 姓名 / 职业
/// beside it, and the epilogue text across the bottom. A page fades up out of black with the
/// figure idle; the figure plays its attack as in a battle and goes back to idle; the portrait
/// and labels come up, the text fades in over a second and stays twelve; then the page fades
/// out over a second (the last one over six). Youni has two texts, twelve seconds each, and
/// the first differs between the endings. The trade and the figure are the ones the companion
/// holds in the saved party, so a transferred class shows as its new self.
///
/// After the last page, a dedication fades in on black in the middle of the screen
/// (ShowDedication); once it is gone, the game goes back to the title by itself.
/// </summary>
public class EndingThemeScene : MonoBehaviour
{
    /// <summary>This scene's name in the build settings.</summary>
    public const string SceneName = "EndingThemeScene";

    /// <summary>The GlobalVariables key the ending hands the party over on.</summary>
    private const string EndingVariableName = "EndingTheme";

    private class EndingInfo
    {
        public GameRecord Party;
        public bool IsGoodEnding;
    }

    /// <summary>One companion's epilogue, as Endings.txt lists them.</summary>
    public class EndingEntry
    {
        /// <summary>The companion's friend id (its first definition id).</summary>
        public int Id;

        /// <summary>The texts, one page each; the good ending's, or both endings' when BadPages is empty.</summary>
        public List<string> Pages;

        /// <summary>The bad ending's texts, where they differ.</summary>
        public List<string> BadPages;
    }

    public class EndingTable
    {
        public List<EndingEntry> Endings;
    }

    /// <summary>
    /// Called by the field as it ends the story: the party as it stands, and which ending it
    /// reached. The bad ending passes through GameEndingScene first; this is read here.
    /// </summary>
    public static void SetEnding(GameRecord party, bool isGoodEnding)
    {
        GlobalVariables.Set(EndingVariableName, new EndingInfo() { Party = party, IsGoodEnding = isGoodEnding });
    }

    /// <summary>TEMP test: show only these companions' pages (Sol and Youni). Empty it to show everyone.</summary>
    private static readonly int[] TestOnlyIds = { 1, 2 };

    private const string MusicPath = "Audios/Musics/Ending_Theme_Final";
    private const string EndingsPath = "Endings/Endings";
    private const string BackgroundPath = "Endings/EndingBackground";

    // Timings, in seconds.
    private const float PageFadeInSeconds = 1f;
    private const float PageFadeOutSeconds = 1f;
    private const float LastPageFadeOutSeconds = 6f;
    private const float TextFadeInSeconds = 1f;
    private const float TextHoldSeconds = 12f;
    /// <summary>The pause before the attack and after it, as a battle has (AttackRunner).</summary>
    private const float BeforeAttackSeconds = 0.5f;
    private const float AfterAttackSeconds = 0.5f;
    /// <summary>The longest an attack is waited for, should its finish event never come.</summary>
    private const float AttackTimeoutSeconds = 6f;

    /// <summary>
    /// The fight figure, placed against the camera exactly as the battle scene places the
    /// player's fighter (LocalBody) against its camera -- so the figure, and the 3D model
    /// FightModel3D builds over it, look as they do in a battle -- then moved right by this
    /// share of the screen width, onto the smoke.
    /// </summary>
    private static readonly Vector3 FighterPosition = new Vector3(0.733f, -0.235f, 41.613f);
    private static readonly Quaternion FighterRotation = new Quaternion(-0.20782f, 0.02970f, -0.00631f, 0.97770f);
    /// <summary>The battle's own scale (4, 4, 1) made half as big again; the sprite's pivot is its centre, so it grows from there.</summary>
    private static readonly Vector3 FighterScale = new Vector3(6f, 6f, 1f);
    private const float FighterScreenShift = 0.12f;
    private const float CameraFieldOfView = 60f;
    private static readonly Vector3 CameraPosition = new Vector3(8f, -9f, 0f);
    private static readonly Vector3 CameraEulerAngles = new Vector3(-15f, 0f, 0f);

    // Layout on the 800 x 600 canvas.
    private const float Margin = 24f;
    private const float PortraitSize = 180f;
    private const float PortraitBorder = 4f;
    private const float LabelFontSize = 32f;
    private const float LabelLineHeight = 52f;
    private const float TextFontSize = 28f;
    private const float TextTop = 250f;

    /// <summary>The dedication: the first line at DedicationFontSize, the signature under it two sizes smaller.</summary>
    private const string DedicationLine = "致所有喜爱经典游戏的玩家们";
    private const string DedicationSignature = "2026, Toney Games";
    private const float DedicationFontSize = 22f;
    private const float DedicationSignatureFontSize = 18f;
    private const float DedicationFadeSeconds = 5f;
    private const float DedicationHoldSeconds = 10f;
    private const float MusicFadeOutSeconds = 3f;

    private static readonly Color TextColor = new Color(0.42f, 0.52f, 0.92f);
    private static readonly Color PortraitBorderColor = new Color(0.62f, 0.66f, 0.68f);

    private ScreenFader fader = null;
    private Camera sceneCamera = null;
    private RectTransform uiRoot = null;
    private TMP_FontAsset font = null;

    /// <summary>The smoke picture's canvas, taken down for the dedication on plain black.</summary>
    private GameObject backgroundCanvas = null;


    void Start()
    {
        EndingInfo info = GlobalVariables.Take<EndingInfo>(EndingVariableName);
        if (info == null)
        {
            // Opened on its own from the editor: everyone, as they first joined.
            Debug.LogWarning("No ending handed over; showing every companion in the good ending.");
            info = new EndingInfo() { Party = new GameRecord() { Friends = new List<CreatureMapRecord>() }, IsGoodEnding = true };
            for (int id = 1; id <= 32; id++)
            {
                info.Party.Friends.Add(new CreatureMapRecord() { Id = id, DefinitionId = id });
            }
        }

        font = ShoppingRecordDialog.GetRecordFont();
        SetUpCamera();
        CreateBackground();
        uiRoot = CreateOverlayCanvas();

        BackgroundMusic.GetOrCreate().PlayClipByName(MusicPath);

        fader = ScreenFader.Create(1.0f);
        StartCoroutine(PlayEpilogues(info));
    }


    private IEnumerator PlayEpilogues(EndingInfo info)
    {
        EndingTable table = ResourceJsonFile.Load<EndingTable>(EndingsPath);
        List<KeyValuePair<CreatureMapRecord, List<string>>> pages = new List<KeyValuePair<CreatureMapRecord, List<string>>>();
        if (table != null && table.Endings != null && info.Party != null && info.Party.Friends != null)
        {
            foreach (EndingEntry entry in table.Endings)
            {
                // TEMP test: only the companions in TestOnlyIds. Empty the array to show everyone.
                if (TestOnlyIds.Length > 0 && Array.IndexOf(TestOnlyIds, entry.Id) < 0)
                {
                    continue;
                }

                CreatureMapRecord member = info.Party.Friends.Find(friend => friend.Id == entry.Id);
                if (member == null)
                {
                    continue;
                }

                List<string> texts = !info.IsGoodEnding && entry.BadPages != null && entry.BadPages.Count > 0
                    ? entry.BadPages : entry.Pages;
                pages.Add(new KeyValuePair<CreatureMapRecord, List<string>>(member, texts ?? new List<string>()));
            }
        }

        for (int i = 0; i < pages.Count; i++)
        {
            yield return ShowCompanion(pages[i].Key, pages[i].Value, i == pages.Count - 1);
        }

        yield return FadeOutMusic();
        yield return ShowDedication();

        SceneManager.LoadScene("TitleScene", LoadSceneMode.Single);
    }

    /// <summary>One companion's page, from black to black.</summary>
    private IEnumerator ShowCompanion(CreatureMapRecord member, List<string> texts, bool isLast)
    {
        CreatureDefinition definition = DefinitionStore.Instance.GetCreatureDefinition(member.DefinitionId);
        if (definition == null)
        {
            yield break;
        }

        // The figure, idle, as the page comes up.
        bool attackFinished = false;
        GameObject fighter = CreateFighter(definition.AnimationId, () => attackFinished = true);
        Animator animator = fighter.GetComponent<Animator>();
        bool canAttack = DefinitionStore.Instance.GetFightAnimation(definition.AnimationId) != null
            && animator.runtimeAnimatorController != null;

        GameObject page = new GameObject("Page", typeof(RectTransform));
        page.transform.SetParent(uiRoot, false);
        Stretch(page.GetComponent<RectTransform>());

        yield return Fade(0f, PageFadeInSeconds);

        // Its attack, timed as in a battle, and back to idle.
        if (canAttack)
        {
            yield return new WaitForSeconds(BeforeAttackSeconds);
            animator.SetInteger("actionState", 1);
            float waited = 0f;
            while (!attackFinished && waited < AttackTimeoutSeconds)
            {
                waited += Time.deltaTime;
                yield return null;
            }
            if (!attackFinished)
            {
                animator.SetInteger("actionState", 0);
            }
            yield return new WaitForSeconds(AfterAttackSeconds);
        }

        // The portrait and who it is.
        Image portrait = CreatePortrait(page.transform, PortraitAnimationId(member, definition));
        Coroutine blink = portrait != null ? StartCoroutine(Blink(portrait, PortraitAnimationId(member, definition))) : null;

        CreatureDefinition firstDefinition = DefinitionStore.Instance.GetCreatureDefinition(member.Id);
        string name = firstDefinition != null && !string.IsNullOrEmpty(firstDefinition.Name) ? firstDefinition.Name : definition.Name;
        OccupationDefinition occupation = DefinitionStore.Instance.GetOccupationDefinition(definition.Occupation);
        float labelLeft = Margin + PortraitSize + 2 * PortraitBorder + 40f;
        CreateLabel(page.transform, "姓名：" + name, labelLeft, Margin + 30f);
        CreateLabel(page.transform, "职业：" + (occupation != null ? occupation.Name : string.Empty), labelLeft, Margin + 30f + LabelLineHeight);

        // The epilogue, a page of text at a time.
        CanvasGroup textGroup;
        TextMeshProUGUI text = CreateEpilogueText(page.transform, out textGroup);
        foreach (string pageText in texts)
        {
            text.text = pageText;
            textGroup.alpha = 0f;
            for (float t = 0f; t < TextFadeInSeconds; t += Time.deltaTime)
            {
                textGroup.alpha = t / TextFadeInSeconds;
                yield return null;
            }
            textGroup.alpha = 1f;
            yield return new WaitForSeconds(TextHoldSeconds);
        }

        yield return Fade(1f, isLast ? LastPageFadeOutSeconds : PageFadeOutSeconds);

        if (blink != null)
        {
            StopCoroutine(blink);
        }
        Destroy(page);
        Destroy(fighter);
    }

    /// <summary>
    /// The closing dedication: two small white lines in the middle of a black screen,
    /// fading in over five seconds, staying ten and fading out over five.
    /// </summary>
    private IEnumerator ShowDedication()
    {
        // Black from here on without the curtain, so the lines can show in front of it.
        Destroy(backgroundCanvas);
        fader.FadeTo(0f, 0f);

        GameObject textObject = new GameObject("Dedication", typeof(RectTransform), typeof(CanvasGroup), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(uiRoot, false);
        Stretch(textObject.GetComponent<RectTransform>());

        CanvasGroup group = textObject.GetComponent<CanvasGroup>();
        group.alpha = 0f;

        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        ApplyTextStyle(text, DedicationFontSize);
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.Center;
        text.lineSpacing = 30f;
        text.text = DedicationLine + "\n<size=" + DedicationSignatureFontSize + ">" + DedicationSignature + "</size>";

        yield return FadeGroup(group, 1f, DedicationFadeSeconds);
        yield return new WaitForSeconds(DedicationHoldSeconds);
        yield return FadeGroup(group, 0f, DedicationFadeSeconds);

        Destroy(textObject);
    }

    /// <summary>
    /// The theme fades out over MusicFadeOutSeconds before the dedication. The player
    /// outlives the scene, so its own fade time is put back afterwards.
    /// </summary>
    private static IEnumerator FadeOutMusic()
    {
        BackgroundMusic music = BackgroundMusic.GetOrCreate();
        float fadeDuration = music.fadeDuration;
        music.fadeDuration = MusicFadeOutSeconds;
        music.StopMusic();
        yield return new WaitForSeconds(MusicFadeOutSeconds);
        music.fadeDuration = fadeDuration;
    }

    private static IEnumerator FadeGroup(CanvasGroup group, float alpha, float seconds)
    {
        float from = group.alpha;
        for (float t = 0f; t < seconds; t += Time.deltaTime)
        {
            group.alpha = Mathf.Lerp(from, alpha, t / seconds);
            yield return null;
        }
        group.alpha = alpha;
    }

    private IEnumerator Fade(float alpha, float seconds)
    {
        bool done = false;
        fader.FadeTo(alpha, seconds, () => done = true);
        while (!done)
        {
            yield return null;
        }
    }

    /// <summary>The portrait to show: the trade's own, or the companion's first one where the trade has none.</summary>
    private static int PortraitAnimationId(CreatureMapRecord member, CreatureDefinition definition)
    {
        string id = StringUtils.Digit3(definition.AnimationId);
        return Resources.Load<Sprite>(string.Format(@"Datos/{0}/Dato_{0}_0", id)) != null ? definition.AnimationId : member.Id;
    }

    #region Building the page

    /// <summary>
    /// The scene's camera. The fighter's placement is measured from a camera at the origin
    /// looking down +Z; this moves the camera from there to frame the figure on the page.
    /// </summary>
    private void SetUpCamera()
    {
        sceneCamera = FindFirstObjectByType<Camera>();
        sceneCamera.transform.SetPositionAndRotation(CameraPosition, Quaternion.Euler(CameraEulerAngles));
        sceneCamera.orthographic = false;
        sceneCamera.fieldOfView = CameraFieldOfView;
        sceneCamera.nearClipPlane = 0.3f;
        sceneCamera.farClipPlane = 1000f;
        sceneCamera.clearFlags = CameraClearFlags.SolidColor;
        sceneCamera.backgroundColor = Color.black;
    }

    /// <summary>The smoke picture, drawn by the camera far behind the fighter so the figure stands in front of it.</summary>
    private void CreateBackground()
    {
        GameObject canvasObject = new GameObject("BackgroundCanvas");
        backgroundCanvas = canvasObject;
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = sceneCamera;
        canvas.planeDistance = 500f;

        GameObject imageObject = new GameObject("Background", typeof(RectTransform), typeof(RawImage), typeof(AspectRatioFitter));
        imageObject.transform.SetParent(canvasObject.transform, false);
        RawImage image = imageObject.GetComponent<RawImage>();
        image.texture = Resources.Load<Texture2D>(BackgroundPath);
        image.raycastTarget = false;

        AspectRatioFitter fitter = imageObject.GetComponent<AspectRatioFitter>();
        fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
        fitter.aspectRatio = image.texture != null ? (float)image.texture.width / image.texture.height : 16f / 9f;
    }

    /// <summary>The portrait, labels and text, over everything but the fade curtain.</summary>
    private static RectTransform CreateOverlayCanvas()
    {
        GameObject canvasObject = new GameObject("EpilogueCanvas");
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(800f, 600f);
        scaler.matchWidthOrHeight = 0.5f;

        return canvasObject.GetComponent<RectTransform>();
    }

    /// <summary>
    /// The companion's fight figure: a sprite body driven by its battle animator, with
    /// FightBody for the attack events and the 3D frames (FightModel3D) when it has them.
    /// </summary>
    private GameObject CreateFighter(int animationId, Action onAttackFinish)
    {
        GameObject fighter = new GameObject("Fighter");
        float depth = FighterPosition.z;
        float screenWidth = 2f * depth * Mathf.Tan(CameraFieldOfView * 0.5f * Mathf.Deg2Rad) * sceneCamera.aspect;
        fighter.transform.SetPositionAndRotation(FighterPosition + Vector3.right * (FighterScreenShift * screenWidth), FighterRotation);
        fighter.transform.localScale = FighterScale;

        fighter.AddComponent<SpriteRenderer>();
        Animator animator = fighter.AddComponent<Animator>();
        string id = StringUtils.Digit3(animationId);
        animator.runtimeAnimatorController = Resources.Load<RuntimeAnimatorController>(
            string.Format("Fights/{0}/animator_{0}", id));

        FightBody body = fighter.AddComponent<FightBody>();
        if (animator.runtimeAnimatorController != null && DefinitionStore.Instance.GetFightAnimation(animationId) != null)
        {
            body.Initialize(animationId, null, onAttackFinish);
        }

        return fighter;
    }

    private Image CreatePortrait(Transform parent, int animationId)
    {
        Sprite sprite = LoadDato(animationId, 0);
        if (sprite == null)
        {
            return null;
        }

        GameObject frameObject = new GameObject("PortraitFrame", typeof(RectTransform), typeof(Image));
        frameObject.transform.SetParent(parent, false);
        float framed = PortraitSize + 2 * PortraitBorder;
        PlaceTopLeft(frameObject.GetComponent<RectTransform>(), Margin, Margin, framed, framed);
        Image frame = frameObject.GetComponent<Image>();
        frame.color = PortraitBorderColor;
        frame.raycastTarget = false;

        GameObject portraitObject = new GameObject("Portrait", typeof(RectTransform), typeof(Image));
        portraitObject.transform.SetParent(frameObject.transform, false);
        PlaceTopLeft(portraitObject.GetComponent<RectTransform>(), PortraitBorder, PortraitBorder, PortraitSize, PortraitSize);
        Image portrait = portraitObject.GetComponent<Image>();
        portrait.sprite = sprite;
        portrait.raycastTarget = false;
        return portrait;
    }

    /// <summary>The portrait's idle blink, as the talk dialog does it: frame 0 held, frame 3 for a moment.</summary>
    private static IEnumerator Blink(Image portrait, int animationId)
    {
        Sprite open = LoadDato(animationId, 0);
        Sprite closed = LoadDato(animationId, 3) ?? open;
        while (true)
        {
            portrait.sprite = open;
            yield return new WaitForSeconds(2.7f);
            portrait.sprite = closed;
            yield return new WaitForSeconds(0.3f);
        }
    }

    private static Sprite LoadDato(int animationId, int frame)
    {
        string id = StringUtils.Digit3(animationId);
        return Resources.Load<Sprite>(string.Format(@"Datos/{0}/Dato_{0}_{1}", id, frame));
    }

    private void CreateLabel(Transform parent, string value, float left, float top)
    {
        GameObject labelObject = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        labelObject.transform.SetParent(parent, false);
        PlaceTopLeft(labelObject.GetComponent<RectTransform>(), left, top, 400f, LabelLineHeight);

        TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>();
        ApplyTextStyle(label, LabelFontSize);
        label.text = value;
        label.alignment = TextAlignmentOptions.MidlineLeft;
        label.textWrappingMode = TextWrappingModes.NoWrap;
    }

    private TextMeshProUGUI CreateEpilogueText(Transform parent, out CanvasGroup group)
    {
        GameObject textObject = new GameObject("Epilogue", typeof(RectTransform), typeof(CanvasGroup), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);
        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.offsetMin = new Vector2(Margin, Margin);
        rect.offsetMax = new Vector2(-Margin, -TextTop);

        group = textObject.GetComponent<CanvasGroup>();
        group.alpha = 0f;

        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        ApplyTextStyle(text, TextFontSize);
        text.alignment = TextAlignmentOptions.TopLeft;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.lineSpacing = 20f;
        return text;
    }

    private void ApplyTextStyle(TextMeshProUGUI text, float size)
    {
        if (font != null)
        {
            text.font = font;
        }
        text.fontSize = size;
        text.color = TextColor;
        text.raycastTarget = false;
    }

    private static void PlaceTopLeft(RectTransform rect, float left, float top, float width, float height)
    {
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(left, -top);
        rect.sizeDelta = new Vector2(width, height);
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    #endregion
}
