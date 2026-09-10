using System.Collections.Generic;
using UnityEngine;

namespace WindingTale.MapObjects.GameMap
{
    /// <summary>
    /// Makes an obstacle glow -- chapter 10's fire pillars. Two things together:
    /// the model's own materials get an emission term (the clip shaders' _Emission,
    /// so the fire keeps its colours whatever the scene lighting does), and a point
    /// light is hung at the flame so the glow falls on the cave floor and on
    /// whoever stands next to it.
    ///
    /// Whether any obstacle glows at all is one switch, <see cref="Enabled"/>,
    /// applied to every instance at once: ObstaclesLayer exposes it as an Inspector
    /// checkbox and a hotkey so the two looks can be compared in Play mode.
    /// </summary>
    public class ObstacleGlow : MonoBehaviour
    {
        /// <summary>How one obstacle glows; see ObstaclesLayer.GetGlow.</summary>
        public sealed class Spec
        {
            /// <summary>Emission strength, 0..1: how much of the albedo is self-lit.</summary>
            public float Emission;
            public Color LightColor;
            /// <summary>Light range in world units; one tile is 2.</summary>
            public float LightRange;
            public float LightIntensity;
            /// <summary>Where the light hangs, as a fraction of the model's height above its base.</summary>
            public float LightHeight;
        }

        private const string EmissionProperty = "_Emission";
        private const string EmissionColorProperty = "_EmissionColor";

        private static bool enabledForAll = true;
        private static readonly List<ObstacleGlow> instances = new List<ObstacleGlow>();

        /// <summary>
        /// The one switch. Setting it re-applies to every glowing obstacle on the board.
        /// </summary>
        public static bool Enabled
        {
            get { return enabledForAll; }
            set
            {
                enabledForAll = value;
                foreach (ObstacleGlow glow in instances)
                {
                    if (glow != null)
                    {
                        glow.Apply();
                    }
                }
            }
        }

        private Spec spec;
        private readonly List<Material> materials = new List<Material>();
        private Light pointLight;

        /// <summary>
        /// Wires the glow up. Call after the obstacle's materials have been switched to
        /// the clip shader (so the per-object instances exist) and after it has been
        /// anchored (so the bounds are final); <paramref name="bounds"/> is used to
        /// place the pointLight.
        /// </summary>
        public void Init(Spec spec, Bounds bounds)
        {
            this.spec = spec;

            materials.Clear();
            foreach (Renderer renderer in GetComponentsInChildren<Renderer>(true))
            {
                foreach (Material material in renderer.materials)
                {
                    if (material != null)
                    {
                        materials.Add(material);
                    }
                }
            }

            // A spec with no light is emission only -- chapter 25's lava sheets, of
            // which there are a couple of hundred, far too many for a light each.
            bool wantsLight = spec.LightIntensity > 0f && spec.LightRange > 0f;
            if (!wantsLight && pointLight != null)
            {
                Destroy(pointLight.gameObject);
                pointLight = null;
            }

            if (wantsLight && pointLight == null)
            {
                GameObject holder = new GameObject("glow");
                holder.transform.SetParent(transform, false);
                pointLight = holder.AddComponent<Light>();
                pointLight.type = LightType.Point;
                pointLight.shadows = LightShadows.None;
                // Regardless of the quality level's pixel light count, so the glow is
                // the same everywhere it is looked at.
                pointLight.renderMode = LightRenderMode.ForcePixel;
            }

            if (pointLight != null)
            {
                pointLight.color = spec.LightColor;
                pointLight.range = spec.LightRange;
                pointLight.intensity = spec.LightIntensity;
                pointLight.transform.position = new Vector3(
                    bounds.center.x,
                    bounds.min.y + bounds.size.y * spec.LightHeight,
                    bounds.center.z);
            }

            Apply();
        }

        private void Apply()
        {
            bool on = enabledForAll && spec != null;
            foreach (Material material in materials)
            {
                if (material == null)
                {
                    continue;
                }
                material.SetColor(EmissionColorProperty, Color.white);
                material.SetFloat(EmissionProperty, on ? spec.Emission : 0f);
            }
            if (pointLight != null)
            {
                pointLight.enabled = on;
            }
        }

        void OnEnable()
        {
            instances.Add(this);
        }

        void OnDisable()
        {
            instances.Remove(this);
        }
    }
}
