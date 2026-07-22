using UnityEngine;
using UnityEngine.SceneManagement;

namespace WindingTale.Scenes.GameOverScene
{
    /// <summary>
    /// The game-over screen. It is handed over on black by the field scene's quitting
    /// animation, so it opens by fading the "Game Over" art up out of that black,
    /// waits for the player to press anything at all, then fades back down and returns
    /// to the title.
    /// </summary>
    public class GameOverSceneLoader : MonoBehaviour
    {
        /// <summary>Time for the screen to come up out of black, in seconds.</summary>
        public float fadeInDuration = 1.0f;

        /// <summary>Time for the screen to fade back to black on the way out, in seconds.</summary>
        public float fadeOutDuration = 3.0f;

        private ScreenFader fader = null;

        /// <summary>
        /// Set as soon as the player has pressed something, so a second press during
        /// the three second fade cannot start the title load twice.
        /// </summary>
        private bool isLeaving = false;

        void Start()
        {
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
    }
}
