using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The name plate in the bottom-right corner of the village: it says what the cursor is
/// standing on -- the way out, or which shop -- read from CommonStrings "VillageShop-NN"
/// (NN = the spot index) so it localizes with everything else.
///
/// Built entirely in code on its own overlay canvas, so the scene needs no edit; VillageScene
/// creates one and calls SetSpot as the cursor moves. It sits under the confirm dialog and the
/// fade curtain, so both draw over it.
/// </summary>
public class VillageInfoBar : MonoBehaviour
{
    private const string FrameResource = "Dialogs/VillageLabel";

    // Above the village picture, below the confirm dialog (100) and the fade curtain.
    private const int SortingOrder = 50;

    // Layout in canvas units (scaled from 800 x 600). The frame bitmap is 60 x 24.
    private const float BarWidth = 200f;
    private const float BarHeight = 80f;
    private const float Margin = 8f;
    private const float FontSize = 32f;

    // The drop shadow under the text: a dark copy, this many canvas units down and right.
    private const float ShadowOffset = 3f;
    private static readonly Color ShadowColor = new Color(0f, 0f, 0f, 0.8f);

    private TextMeshProUGUI label = null;
    private TextMeshProUGUI shadow = null;

    // The spot the label currently names, so the string is only resolved when it changes.
    private int shownSpot = -1;

    /// <summary>Builds a bar on a new object in the active scene, empty until SetSpot is called.</summary>
    public static VillageInfoBar Create()
    {
        GameObject canvasObject = new GameObject("VillageInfoBar");

        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = SortingOrder;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(800f, 600f);
        scaler.matchWidthOrHeight = 0.5f;

        VillageInfoBar bar = canvasObject.AddComponent<VillageInfoBar>();
        bar.Build();
        return bar;
    }

    private void Build()
    {
        GameObject frameObject = new GameObject("Frame", typeof(RectTransform), typeof(Image));
        frameObject.transform.SetParent(transform, false);

        RectTransform frameRect = frameObject.GetComponent<RectTransform>();
        frameRect.anchorMin = Vector2.right;
        frameRect.anchorMax = Vector2.right;
        frameRect.pivot = Vector2.right;
        frameRect.anchoredPosition = new Vector2(-Margin, Margin);
        frameRect.sizeDelta = new Vector2(BarWidth, BarHeight);

        Image frame = frameObject.GetComponent<Image>();
        frame.sprite = LoadFrame();
        frame.raycastTarget = false;

        TMP_FontAsset font = ShoppingRecordDialog.GetRecordFont();
        shadow = CreateLabel("Shadow", frameRect, font, ShadowColor, ShadowOffset);
        label = CreateLabel("Label", frameRect, font, Color.white, 0f);
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
    /// The frame bitmap (the original game's village label). It ships imported as a plain
    /// texture rather than a sprite, so it is wrapped in one here if need be.
    /// </summary>
    private static Sprite LoadFrame()
    {
        Sprite sprite = Resources.Load<Sprite>(FrameResource);
        if (sprite != null)
        {
            return sprite;
        }

        Texture2D texture = Resources.Load<Texture2D>(FrameResource);
        if (texture == null)
        {
            Debug.LogWarning("VillageInfoBar: frame image not found: Resources/" + FrameResource);
            return null;
        }

        texture.filterMode = FilterMode.Point;
        return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
    }

    /// <summary>Names the given cursor spot on the bar: 0 the way out, 1-5 the shops.</summary>
    public void SetSpot(int spotIndex)
    {
        if (spotIndex == shownSpot || label == null)
        {
            return;
        }

        string text = LocalizationManager.GetVillageShopString(spotIndex).GetLocalizedString();
        label.text = text;
        shadow.text = text;
        shownSpot = spotIndex;
    }
}
