using UnityEngine;

namespace WindingTale.MapObjects.GameMap
{
    /// <summary>
    /// Shared fade behaviour for map props that stand on the board (obstacles,
    /// treasure chests). Swaps every renderer between the opaque clip shader and
    /// its fading variant so whatever is behind the prop -- a creature, the
    /// cursor, a menu item -- stays readable.
    ///
    /// Both shaders clip geometry to the board rectangle (see
    /// ObstaclesLayer.SetMapClipBounds), so fading never un-clips a prop that
    /// overhangs the map edge.
    /// </summary>
    public abstract class MapObjectFade : MonoBehaviour
    {
        private bool faded = false;

        // Alpha currently applied, so a re-fade at a different alpha still takes.
        private float fadedAlpha = 1f;

        private static Shader opaqueShader = null;
        private static Shader fadeShader = null;

        /// <summary>
        /// Fades the whole object to the given alpha. Call ResetTransparency() to
        /// restore. Re-applying the same alpha is a no-op.
        /// </summary>
        public void SetTransparency(float alpha)
        {
            if (faded && Mathf.Approximately(fadedAlpha, alpha))
            {
                return;
            }

            ApplyMaterials(GetFadeShader(), alpha);
            faded = true;
            fadedAlpha = alpha;
        }

        /// <summary>
        /// Restores full opacity after SetTransparency().
        /// </summary>
        public void ResetTransparency()
        {
            if (!faded)
            {
                return;
            }

            ApplyMaterials(GetOpaqueShader(), 1f);
            faded = false;
            fadedAlpha = 1f;
        }

        /// <summary>
        /// Drops the cached fade state without touching materials. Call after
        /// replacing the object's renderers (e.g. swapping a chest's model), whose
        /// fresh materials start opaque no matter what this object was showing.
        /// </summary>
        protected void ForgetFadeState()
        {
            faded = false;
            fadedAlpha = 1f;
        }

        private void ApplyMaterials(Shader shader, float alpha)
        {
            foreach (Renderer renderer in GetComponentsInChildren<Renderer>())
            {
                Material[] mats = renderer.materials; // per-instance copies
                foreach (Material m in mats)
                {
                    if (m == null)
                    {
                        continue;
                    }

                    if (shader != null)
                    {
                        m.shader = shader;
                    }

                    Color c = m.color;
                    c.a = alpha;
                    m.color = c;
                }
                renderer.materials = mats;
            }
        }

        private static Shader GetOpaqueShader()
        {
            if (opaqueShader == null)
            {
                opaqueShader = Shader.Find("Custom/MapClip");
            }
            return opaqueShader;
        }

        private static Shader GetFadeShader()
        {
            if (fadeShader == null)
            {
                fadeShader = Shader.Find("Custom/MapClipFade");
            }
            return fadeShader;
        }
    }
}
