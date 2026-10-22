using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace WindingTale.Scenes.GameFreeFieldScene
{
    /// <summary>
    /// A hint in the top-left corner of a free field -- "按方向键操作角色" on Map 32, the
    /// map's "Hint" -- in the skip bar's look. It waits until the map has faded in from
    /// black, fades in, then keeps fading out and back in, once every
    /// <see cref="PulseSeconds"/>, until the player presses an arrow key; then it fades out
    /// for good and removes itself. An arrow key pressed before it shows means it never does.
    /// </summary>
    public class FreeFieldHintBar : MonoBehaviour
    {
        /// <summary>One fade out and back in, in seconds.</summary>
        public const float PulseSeconds = 4f;

        private const float FadeInSeconds = 1f;
        private const float FadeOutSeconds = 0.5f;

        private static readonly Vector2 Size = new Vector2(220f, 36f);
        private static readonly Vector2 Margin = new Vector2(32f, 32f);

        private CanvasGroup group;
        private float delay = 0f;
        private float elapsed = 0f;
        private bool dismissed = false;
        private float dismissFrom = 1f;
        private float dismissElapsed = 0f;

        /// <param name="delay">Seconds to wait before fading in: the scene's own fade-in.</param>
        public static FreeFieldHintBar Create(Transform canvas, string hint, float delay)
        {
            GameObject root = new GameObject("HintBar", typeof(RectTransform), typeof(CanvasGroup));
            root.transform.SetParent(canvas, false);
            RectTransform rect = (RectTransform)root.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
            rect.sizeDelta = Size;
            rect.anchoredPosition = new Vector2(Margin.x, -Margin.y);

            Color background = FreeFieldSkipBar.Blue;
            background.a = FreeFieldSkipBar.BackgroundAlpha;
            FreeFieldSkipBar.Stretch(FreeFieldSkipBar.NewChild(root.transform, "Background", typeof(Image)))
                .GetComponent<Image>().color = background;

            TextMeshProUGUI text = FreeFieldSkipBar.Stretch(FreeFieldSkipBar.NewChild(root.transform, "Label", typeof(TextMeshProUGUI)))
                .GetComponent<TextMeshProUGUI>();
            text.font = ShoppingRecordDialog.GetRecordFont();
            text.text = hint;
            text.fontSize = 20f;
            text.color = Color.white;
            text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false;

            FreeFieldHintBar bar = root.AddComponent<FreeFieldHintBar>();
            bar.group = root.GetComponent<CanvasGroup>();
            bar.group.alpha = 0f;
            bar.group.interactable = false;
            bar.group.blocksRaycasts = false;
            bar.delay = delay;
            return bar;
        }

        void Update()
        {
            if (dismissed)
            {
                dismissElapsed += Time.deltaTime;
                group.alpha = Mathf.Lerp(dismissFrom, 0f, dismissElapsed / FadeOutSeconds);
                if (dismissElapsed >= FadeOutSeconds)
                {
                    Destroy(gameObject);
                }
                return;
            }

            if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.DownArrow)
                || Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.RightArrow))
            {
                dismissed = true;
                dismissFrom = group.alpha;
                return;
            }

            if (delay > 0f)
            {
                delay -= Time.deltaTime;
                return;
            }

            elapsed += Time.deltaTime;
            if (elapsed < FadeInSeconds)
            {
                group.alpha = elapsed / FadeInSeconds;
                return;
            }

            // Full, out to nothing and back to full, once every PulseSeconds.
            float phase = (elapsed - FadeInSeconds) / PulseSeconds;
            group.alpha = 0.5f + 0.5f * Mathf.Cos(2f * Mathf.PI * phase);
        }
    }
}
