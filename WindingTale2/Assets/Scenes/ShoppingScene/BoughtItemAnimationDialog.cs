using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using WindingTale.Core.Definitions;

/// <summary>
/// The short flourish shown right after a purchase is made -- the item drops into the
/// creature's pack. A placeholder for now: it puts the bought item's icon and name up on a
/// dimmed backdrop with a "购买成功！" line, holds for a couple of seconds, then reports done
/// so the shop can carry on. No input; it dismisses itself on a timer.
///
/// Unlike the other shop dialogs it carries no authored prefab -- the shop adds this component
/// to a bare GameObject and it builds its own overlay canvas in code, so nothing has to be
/// wired in the inspector for a placeholder. Swap it for a real animation later behind the
/// same Init / onDone contract.
/// </summary>
public class BoughtItemAnimationDialog : MonoBehaviour
{
    private const string MessageFontPath = @"Fonts/FontAssets/zh/FZB_Message";

    private float duration = 2f;
    private float elapsed = 0f;
    private Action onDone = null;
    private bool running = false;

    /// <summary>
    /// Shows the bought item's icon and name for <paramref name="duration"/> seconds, then
    /// raises <paramref name="onDone"/> once. A null item still shows the banner, just without
    /// an icon or name.
    /// </summary>
    public void Init(ItemDefinition item, float duration, Action onDone)
    {
        this.duration = Mathf.Max(0f, duration);
        this.onDone = onDone;
        this.elapsed = 0f;

        BuildOverlay(item);

        running = true;
    }

    void Update()
    {
        if (!running)
        {
            return;
        }

        elapsed += Time.deltaTime;
        if (elapsed >= duration)
        {
            running = false; // fire once, even if the callback does not tear us down at once
            onDone?.Invoke();
        }
    }

    /// <summary>
    /// Builds the whole placeholder in code: an overlay canvas, a dimmed full-screen backdrop,
    /// and a centred column of the item's icon, its name and a "购买成功！" line.
    /// </summary>
    private void BuildOverlay(ItemDefinition item)
    {
        Canvas canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000; // over the picker and the shop picture beneath it
        gameObject.AddComponent<GraphicRaycaster>();

        CreateBackdrop(transform);

        Sprite iconSprite = item != null ? ItemIconHelper.LoadIcon(item) : null;
        if (iconSprite != null)
        {
            CreateIcon(transform, iconSprite);
        }

        string itemName = item != null && item.Name != null ? item.Name : string.Empty;
        if (!string.IsNullOrEmpty(itemName))
        {
            CreateLabel(transform, "ItemName", itemName, 40f, new Vector2(0f, -40f));
        }

        CreateLabel(transform, "BoughtBanner", "购买成功！", 28f, new Vector2(0f, -100f));
    }

    private static void CreateBackdrop(Transform parent)
    {
        GameObject backdropObject = new GameObject("Backdrop", typeof(RectTransform), typeof(Image));
        RectTransform rect = backdropObject.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Image image = backdropObject.GetComponent<Image>();
        image.color = new Color(0f, 0f, 0f, 0.5f);
        image.raycastTarget = false;
    }

    private void CreateIcon(Transform parent, Sprite sprite)
    {
        GameObject iconObject = new GameObject("Icon", typeof(RectTransform), typeof(Image));
        RectTransform rect = iconObject.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(96f, 96f);
        rect.anchoredPosition = new Vector2(0f, 60f);

        Image image = iconObject.GetComponent<Image>();
        image.sprite = sprite;
        image.preserveAspect = true;
        image.raycastTarget = false;
    }

    private void CreateLabel(Transform parent, string name, string text, float fontSize, Vector2 anchoredPosition)
    {
        GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(600f, fontSize + 16f);
        rect.anchoredPosition = anchoredPosition;

        TextMeshProUGUI label = textObject.GetComponent<TextMeshProUGUI>();
        label.alignment = TextAlignmentOptions.Center;
        label.enableAutoSizing = false;
        label.fontSize = fontSize;
        label.color = Color.white;
        label.raycastTarget = false;
        label.text = text;

        // The bare TMP default (LiberationSans) carries no Chinese glyphs, so assign the whole
        // FZB_Message asset -- the same fix the other shop dialogs use, or the line is tofu.
        TMP_FontAsset messageFont = Resources.Load<TMP_FontAsset>(MessageFontPath);
        if (messageFont != null)
        {
            label.font = messageFont;
        }

        label.ForceMeshUpdate();
    }
}
