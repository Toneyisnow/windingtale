using UnityEngine;

namespace WindingTale.Scenes.GameBattleScene
{
    /// <summary>
    /// Plays a quick slide-in when the battle scene opens: the camera starts offset to
    /// the right of its authored position and eases left into place (decelerating, then
    /// settling). Attach to the battle camera. The authored scene position is the final
    /// resting position, so designers tune the shot in the editor as usual.
    /// </summary>
    public class BattleCameraIntro : MonoBehaviour
    {
        [Tooltip("Seconds for the slide-in.")]
        public float duration = 0.7f;

        [Tooltip("How far to the camera's right (world units) the slide starts.")]
        public float slideDistance = 60f;

        private Vector3 targetPos;
        private Vector3 startPos;
        private float elapsed;
        private bool animating;

        void Start()
        {
            targetPos = transform.position;
            // Start offset to the camera's right; the slide moves right -> left.
            startPos = targetPos + transform.right * slideDistance;
            transform.position = startPos;
            elapsed = 0f;
            animating = duration > 0f;

            if (!animating)
            {
                transform.position = targetPos;
            }
        }

        void LateUpdate()
        {
            if (!animating)
            {
                return;
            }

            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            // Ease-out cubic: fast in, decelerating to a stop.
            float eased = 1f - Mathf.Pow(1f - t, 3f);
            transform.position = Vector3.LerpUnclamped(startPos, targetPos, eased);

            if (t >= 1f)
            {
                transform.position = targetPos;
                animating = false;
            }
        }
    }
}
