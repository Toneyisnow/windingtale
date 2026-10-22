using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace WindingTale.Scenes.GameFreeFieldScene
{
    /// <summary>
    /// The "按空格跳过" bar in the bottom-right corner of a free field. It is invisible when
    /// the map opens and fades in after <see cref="AppearDelay"/> seconds. Holding Space or
    /// Enter fills it from the left, in the same blue but opaque and under the label; let go
    /// and it empties. Full -- <see cref="HoldSeconds"/> of holding -- it calls
    /// <see cref="OnSkip"/> once, and the scene leaves for whatever comes after the map.
    /// Built in code under the scene's canvas (800 x 600 reference, Scale With Screen Size).
    /// </summary>
    public class FreeFieldSkipBar : MonoBehaviour
    {
        /// <summary>Seconds of holding to fill the bar.</summary>
        public const float HoldSeconds = 3f;

        /// <summary>Seconds after the map opens before the bar starts to fade in.</summary>
        public const float AppearDelay = 5f;

        private const float FadeInSeconds = 1f;

        private const string Label = "按空格跳过";

        private static readonly Vector2 Size = new Vector2(180f, 36f);
        private static readonly Vector2 Margin = new Vector2(32f, 32f);
        internal static readonly Color Blue = new Color(0.16f, 0.36f, 0.85f, 1f);
        internal const float BackgroundAlpha = 0.3f;

        /// <summary>Called once, when the bar fills.</summary>
        public Action OnSkip;

        private CanvasGroup group;
        private RectTransform fill;
        private float elapsed = 0f;
        private float progress = 0f;
        private bool skipped = false;

        public static FreeFieldSkipBar Create(Transform canvas, Action onSkip)
        {
            GameObject root = new GameObject("SkipBar", typeof(RectTransform), typeof(CanvasGroup));
            root.transform.SetParent(canvas, false);
            RectTransform rect = (RectTransform)root.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(1f, 0f);
            rect.sizeDelta = Size;
            rect.anchoredPosition = new Vector2(-Margin.x, Margin.y);

            Color background = Blue;
            background.a = BackgroundAlpha;
            Stretch(NewChild(root.transform, "Background", typeof(Image))).GetComponent<Image>().color = background;

            // The fill grows from the left edge by its right anchor, so no sprite is needed.
            RectTransform fillRect = NewChild(root.transform, "Fill", typeof(Image));
            fillRect.anchorMin = new Vector2(0f, 0f);
            fillRect.anchorMax = new Vector2(0f, 1f);
            fillRect.offsetMin = fillRect.offsetMax = Vector2.zero;
            fillRect.GetComponent<Image>().color = Blue;

            // The label last, so the fill passes under it.
            TextMeshProUGUI text = Stretch(NewChild(root.transform, "Label", typeof(TextMeshProUGUI))).GetComponent<TextMeshProUGUI>();
            text.font = ShoppingRecordDialog.GetRecordFont();
            text.text = Label;
            text.fontSize = 20f;
            text.color = Color.white;
            text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false;

            FreeFieldSkipBar bar = root.AddComponent<FreeFieldSkipBar>();
            bar.group = root.GetComponent<CanvasGroup>();
            bar.group.alpha = 0f;
            bar.group.interactable = false;
            bar.group.blocksRaycasts = false;
            bar.fill = fillRect;
            bar.OnSkip = onSkip;
            return bar;
        }

        void Update()
        {
            if (skipped)
            {
                return;
            }

            elapsed += Time.deltaTime;
            group.alpha = Mathf.Clamp01((elapsed - AppearDelay) / FadeInSeconds);

            // Only once it is showing: a bar no one can see cannot be held down.
            bool holding = elapsed >= AppearDelay
                && (Input.GetKey(KeyCode.Space) || Input.GetKey(KeyCode.Return) || Input.GetKey(KeyCode.KeypadEnter));
            progress = holding ? Mathf.Min(1f, progress + Time.deltaTime / HoldSeconds) : 0f;
            fill.anchorMax = new Vector2(progress, 1f);

            if (progress >= 1f)
            {
                skipped = true;
                OnSkip?.Invoke();
            }
        }

        internal static RectTransform NewChild(Transform parent, string name, Type component)
        {
            GameObject child = new GameObject(name, typeof(RectTransform), component);
            child.transform.SetParent(parent, false);
            return (RectTransform)child.transform;
        }

        internal static RectTransform Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            return rect;
        }
    }
}
