using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace WindingTale.Scenes.GameOverScene
{
    /// <summary>
    /// The game-over screen. It is handed over on black by the field scene's quitting
    /// animation, so it opens by fading the "Game Over" art up out of that black,
    /// waits for the player to press anything at all, then fades back down and returns
    /// to the title. The "GAME OVER" lettering (Dialogs/GameOver_lable) stands at the
    /// bottom centre of the screen, under the picture.
    /// </summary>
    public class GameOverSceneLoader : MonoBehaviour
    {
        /// <summary>Time for the screen to come up out of black, in seconds.</summary>
        public float fadeInDuration = 1.0f;

        /// <summary>Time for the screen to fade back to black on the way out, in seconds.</summary>
        public float fadeOutDuration = 3.0f;

        private const string LabelResource = "Dialogs/GameOver_lable";

        // The lettering is a 55 x 8 bitmap, drawn at this many canvas units per pixel (the
        // canvas is scaled from 800 x 600), this far up from the bottom edge.
        private const float LabelPixelScale = 5f;
        private const float LabelBottomMargin = 60f;

        // Over the scene's own canvas, under the fade curtain, so it fades with the picture.
        private const int LabelSortingOrder = 10;

        private ScreenFader fader = null;

        /// <summary>
        /// Set as soon as the player has pressed something, so a second press during
        /// the three second fade cannot start the title load twice.
        /// </summary>
        private bool isLeaving = false;

        void Start()
        {
            ShowLabel();

            fader = ScreenFader.Create(1.0f);
            fader.FadeTo(0.0f, fadeInDuration);
        }

        void Update()
        {
            //// Nothing to do until the picture is fully up: a key pressed during the
            //// fade in is the tail of whatever ended the battle, not an answer to this
            //// screen.
            if (isLeaving || fader == null || fader.IsFading)
            {
                return;
            }

            //// Any keyboard or mouse button goes back to the title.
            if (!Input.anyKeyDown)
            {
                return;
            }

            isLeaving = true;
            fader.FadeTo(1.0f, fadeOutDuration, () =>
            {
                SceneManager.LoadScene("TitleScene", LoadSceneMode.Single);
            });
        }

        /// <summary>
        /// Puts the "GAME OVER" lettering up at the bottom centre, on an overlay canvas of
        /// its own so the scene needs no edit.
        /// </summary>
        private void ShowLabel()
        {
            Texture2D texture = Resources.Load<Texture2D>(LabelResource);
            if (texture == null)
            {
                Debug.LogWarning("GameOverSceneLoader: label not found: Resources/" + LabelResource);
                return;
            }

            texture.filterMode = FilterMode.Point;

            GameObject canvasObject = new GameObject("GameOverLabel");
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = LabelSortingOrder;

            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(800f, 600f);
            scaler.matchWidthOrHeight = 0.5f;

            GameObject labelObject = new GameObject("Label", typeof(RectTransform), typeof(RawImage));
            labelObject.transform.SetParent(canvasObject.transform, false);

            RectTransform rect = labelObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(0f, LabelBottomMargin);
            rect.sizeDelta = new Vector2(texture.width * LabelPixelScale, texture.height * LabelPixelScale);

            RawImage image = labelObject.GetComponent<RawImage>();
            image.texture = texture;
            image.raycastTarget = false;
        }
    }
}
