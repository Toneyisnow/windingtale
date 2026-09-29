using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The title plate at the top centre of the village: the name of the chapter the party is
/// on its way to, e.g. "第11关 幻之森林", read from CommonStrings "ChapterTitle-NN" so it
/// localizes with everything else.
///
/// It wears the same frame as the name plate in the bottom-right corner (VillageInfoBar),
/// drawn lower and longer. The frame bitmap is a flat blue field with a one-pixel light
/// edge down its right side and along its bottom; it is nine-sliced so that edge keeps the
/// name plate's thickness instead of being smeared by the longer stretch.
///
/// Built entirely in code on its own overlay canvas, like VillageInfoBar; VillageScene
/// creates it once the record says which chapter is next.
/// </summary>
public class VillageTitleBar : MonoBehaviour
{
    private const string FrameResource = "Dialogs/VillageLabel";

    // Same layer as the name plate: above the village picture, below the confirm dialog
    // (100) and the fade curtain.
    private const int SortingOrder = 50;

    // Layout in canvas units (scaled from 800 x 600). Lower and longer than the name
    // plate (160 x 64): a chapter name runs to seven or eight characters.
    private const float BarWidth = 360f;
    private const float BarHeight = 48f;
    private const float Margin = 8f;
    private const float FontSize = 24f;

    // How thick the frame's light edge is drawn, in canvas units: what the name plate's
    // 60 x 24 bitmap comes to at its 160 x 64.
    private const float EdgeThickness = 64f / 24f;

    private const float ShadowOffset = 2.5f;
    private static readonly Color ShadowColor = new Color(0f, 0f, 0f, 0.8f);

    /// <summary>Builds the bar on a new object in the active scene, showing the given chapter.</summary>
    public static VillageTitleBar Create(int chapterId)
    {
        GameObject canvasObject = new GameObject("VillageTitleBar");

        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = SortingOrder;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(800f, 600f);
        scaler.matchWidthOrHeight = 0.5f;

        VillageTitleBar bar = canvasObject.AddComponent<VillageTitleBar>();
        bar.Build(LocalizationManager.GetChapterTitleString(chapterId).GetLocalizedString());
        return bar;
    }

    private void Build(string title)
    {
        AddTitlePlate(transform, title);
    }

    /// <summary>
    /// Adds the framed title plate, top centre, to a canvas built from 800 x 600 like this
    /// one. Also used by the battlefield's ChapterInfoDialog, so the chapter name looks the
    /// same there as in the village. Returns the plate's frame.
    /// </summary>
    public static RectTransform AddTitlePlate(Transform canvas, string title)
    {
        GameObject frameObject = new GameObject("TitlePlate", typeof(RectTransform), typeof(Image));
        frameObject.transform.SetParent(canvas, false);

        RectTransform frameRect = frameObject.GetComponent<RectTransform>();
        frameRect.anchorMin = new Vector2(0.5f, 1f);
        frameRect.anchorMax = new Vector2(0.5f, 1f);
        frameRect.pivot = new Vector2(0.5f, 1f);
        frameRect.anchoredPosition = new Vector2(0f, -Margin);
        frameRect.sizeDelta = new Vector2(BarWidth, BarHeight);

        Image frame = frameObject.GetComponent<Image>();
        frame.sprite = LoadSlicedFrame();
        frame.type = Image.Type.Sliced;
        frame.fillCenter = true;
        // The sprite is 100 pixels per unit against the canvas's 100, so one bitmap pixel
        // is one canvas unit; this multiplier draws the one-pixel edge EdgeThickness wide.
        frame.pixelsPerUnitMultiplier = 1f / EdgeThickness;
        frame.raycastTarget = false;

        TMP_FontAsset font = ShoppingRecordDialog.GetRecordFont();
        CreateLabel("Shadow", frameRect, font, ShadowColor, ShadowOffset).text = title;
        CreateLabel("Label", frameRect, font, Color.white, 0f).text = title;
        return frameRect;
    }

    private static TextMeshProUGUI CreateLabel(string objectName, RectTransform parent, TMP_FontAsset font, Color color, float offset)
    {
        GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);

        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(offset, -offset);
        rect.offsetMax = new Vector2(offset, -offset);

        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        if (font != null)
        {
            text.font = font;
        }

        text.fontSize = FontSize;
        text.fontStyle = FontStyles.Bold;
        text.alignment = TextAlignmentOptions.Center;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.overflowMode = TextOverflowModes.Overflow;
        text.color = color;
        text.raycastTarget = false;
        return text;
    }

    /// <summary>
    /// The village label bitmap as a nine-sliced sprite: the light edge is its last column
    /// and its bottom row, so the border is one pixel on the right and one at the bottom
    /// (Unity's border order is left, bottom, right, top). Built from the texture every
    /// time -- a sprite the importer made would carry no border.
    /// </summary>
    private static Sprite LoadSlicedFrame()
    {
        Texture2D texture = Resources.Load<Texture2D>(FrameResource);
        if (texture == null)
        {
            Debug.LogWarning("VillageTitleBar: frame image not found: Resources/" + FrameResource);
            return null;
        }

        texture.filterMode = FilterMode.Point;
        return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f),
            100f, 0, SpriteMeshType.FullRect, new Vector4(0f, 1f, 1f, 0f));
    }
}
