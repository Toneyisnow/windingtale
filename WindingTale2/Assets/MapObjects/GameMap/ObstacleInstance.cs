using UnityEngine;
using WindingTale.Core.Common;

namespace WindingTale.MapObjects.GameMap
{
    /// <summary>
    /// Runtime data for one instantiated obstacle: which tiles of the board it
    /// covers, plus the fade used when the cursor or a menu item lands on one of
    /// those tiles (the same treatment creatures get, see Creature.SetTransparency).
    ///
    /// The footprint is the rectangle
    ///   [TileX, TileX + Width - 1] x [TileY, TileY + Height - 1]
    /// with (TileX, TileY) being the obstacle's Position from the chapter JSON,
    /// i.e. its top-left tile; the model extends into the map from there.
    /// </summary>
    public class ObstacleInstance : MonoBehaviour
    {
        public int TileX { get; private set; }
        public int TileY { get; private set; }
        public int Width { get; private set; }
        public int Height { get; private set; }

        private bool faded = false;

        private static Shader opaqueShader = null;
        private static Shader fadeShader = null;

        public void SetFootprint(int tileX, int tileY, int width, int height)
        {
            this.TileX = tileX;
            this.TileY = tileY;
            this.Width = Mathf.Max(1, width);
            this.Height = Mathf.Max(1, height);
        }

        /// <summary>
        /// True when the given tile falls inside this obstacle's footprint.
        /// </summary>
        public bool Covers(FDPosition position)
        {
            if (position == null)
            {
                return false;
            }

            return position.X >= TileX && position.X <= TileX + Width - 1
                && position.Y >= TileY && position.Y <= TileY + Height - 1;
        }

        /// <summary>
        /// Fades the whole obstacle to the given alpha. Call ResetTransparency() to
        /// restore. Re-applying while already faded is a no-op.
        /// </summary>
        public void SetTransparency(float alpha)
        {
            if (faded)
            {
                return;
            }

            Shader shader = GetFadeShader();
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

            faded = true;
        }

        /// <summary>
        /// Restores the obstacle to full opacity after SetTransparency().
        /// </summary>
        public void ResetTransparency()
        {
            if (!faded)
            {
                return;
            }

            Shader shader = GetOpaqueShader();
            foreach (Renderer renderer in GetComponentsInChildren<Renderer>())
            {
                Material[] mats = renderer.materials;
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
                    c.a = 1f;
                    m.color = c;
                }
                renderer.materials = mats;
            }

            faded = false;
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
