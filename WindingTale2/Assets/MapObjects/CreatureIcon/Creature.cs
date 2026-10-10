using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;
using WindingTale.Core.Objects;
using WindingTale.Core.Definitions;
using WindingTale.Core.Algorithms;
using WindingTale.Scenes.GameFieldScene;
using UnityEngine.EventSystems;

namespace WindingTale.MapObjects.CreatureIcon
{
    public class Creature : MonoBehaviour, IPointerClickHandler
    {
        private bool isInitialized = false;

        private int moveCount = 0;
        private bool isMoving = false;

        private FDMovePath path = null;

        private Material[] originalMaterials1 = null;
        private Material[] originalMaterials2 = null;
        private Material[] originalMaterials3 = null;

        public FDCreature creature
        {
            get; private set;
        }

        public void SetCreature(FDCreature creature)
        {
            this.creature = creature;
            isInitialized = true;
        }

        /// <summary>
        /// Reset to initial state when a new turn starts
        /// </summary>
        public void ResetTurnState()
        {
            this.SetActioned(false);
            this.creature.PrePosition = null;

            if (this.creature is FDAICreature aiCreature)
            {
                // A deferred turn only ever defers within the turn it was deferred in.
                aiCreature.PendingAction = false;
            }
        }

        public void SetGreyout(bool greyout)
        {
            // Some icons are deliberately empty (the hidden swamp creatures 758..761 have
            // fully transparent originals), so their OBJ imports without a "default" child.
            MeshRenderer r1 = GetClipRenderer("Clip_01");
            MeshRenderer r2 = GetClipRenderer("Clip_02");
            MeshRenderer r3 = GetClipRenderer("Clip_03");

            if (greyout)
            {
                // Save shared material references before greying out; guard against double-call overwriting originals
                if (originalMaterials1 == null && r1 != null) originalMaterials1 = r1.sharedMaterials;
                if (originalMaterials2 == null && r2 != null) originalMaterials2 = r2.sharedMaterials;
                if (originalMaterials3 == null && r3 != null) originalMaterials3 = r3.sharedMaterials;
                if (r1 != null) GameRenderer.Instance.ApplyDefaultGreyMaterial(r1.gameObject);
                if (r2 != null) GameRenderer.Instance.ApplyDefaultGreyMaterial(r2.gameObject);
                if (r3 != null) GameRenderer.Instance.ApplyDefaultGreyMaterial(r3.gameObject);
            }
            else
            {
                if (originalMaterials1 != null && r1 != null) { r1.sharedMaterials = originalMaterials1; originalMaterials1 = null; }
                if (originalMaterials2 != null && r2 != null) { r2.sharedMaterials = originalMaterials2; originalMaterials2 = null; }
                if (originalMaterials3 != null && r3 != null) { r3.sharedMaterials = originalMaterials3; originalMaterials3 = null; }

                // The saved "originals" may be the faded copies a menu put on while it covered
                // this creature (greyed out under an open menu, e.g. by 结束回合): bring the
                // restored materials back to the opacity the creature should have now.
                if (transparencyAlpha < 1f)
                {
                    SetTransparency(transparencyAlpha);
                }
                else if (HasFadedMaterial())
                {
                    ResetTransparency();
                }
            }
        }

        /// <summary>Whether any clip still renders with a see-through material.</summary>
        private bool HasFadedMaterial()
        {
            foreach (string clip in ClipNames)
            {
                MeshRenderer renderer = GetClipRenderer(clip);
                if (renderer == null)
                {
                    continue;
                }

                foreach (Material m in renderer.sharedMaterials)
                {
                    if (m != null && m.HasProperty("_Color") && m.color.a < 1f)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static readonly string[] ClipNames = { "Clip_01", "Clip_02", "Clip_03" };

        // The opacity SetTransparency last asked for (1 = opaque), so a material swap
        // (greyout) can put it back on whatever materials it restores.
        private float transparencyAlpha = 1f;

        // Materials saved while the white recovering flash is up. Kept separate from the
        // greyout originals: a creature flashes first and is greyed out afterwards, and
        // the two must not overwrite each other's saved state.
        private Material[][] preFlashMaterials = null;

        /// <summary>
        /// Paints the creature flat white (the "recovering" flash), or restores the
        /// materials it had before. See CreatureRecovering for the timing.
        /// </summary>
        public void SetWhiteFlash(bool flashing)
        {
            if (flashing)
            {
                if (preFlashMaterials != null)
                {
                    // Already flashing: don't overwrite the saved materials with white ones.
                    return;
                }

                preFlashMaterials = new Material[ClipNames.Length][];
                for (int i = 0; i < ClipNames.Length; i++)
                {
                    MeshRenderer renderer = GetClipRenderer(ClipNames[i]);
                    if (renderer == null)
                    {
                        continue;
                    }

                    preFlashMaterials[i] = renderer.sharedMaterials;
                    GameRenderer.Instance.ApplyWhiteFlashMaterial(renderer.gameObject);
                }
            }
            else
            {
                if (preFlashMaterials == null)
                {
                    return;
                }

                for (int i = 0; i < ClipNames.Length; i++)
                {
                    MeshRenderer renderer = GetClipRenderer(ClipNames[i]);
                    if (renderer != null && preFlashMaterials[i] != null)
                    {
                        renderer.sharedMaterials = preFlashMaterials[i];
                    }
                }
                preFlashMaterials = null;
            }
        }

        /// <summary>
        /// Fades the whole creature to the given alpha (e.g. while a menu covers its
        /// tile, so the menu reads clearly). Call ResetTransparency() to restore.
        /// </summary>
        public void SetTransparency(float alpha)
        {
            transparencyAlpha = alpha;

            foreach (string clip in ClipNames)
            {
                MeshRenderer renderer = GetClipRenderer(clip);
                if (renderer == null)
                {
                    continue;
                }

                Material[] mats = renderer.materials; // per-instance copies
                foreach (Material m in mats)
                {
                    SetMaterialFade(m);
                    Color c = m.color;
                    c.a = alpha;
                    m.color = c;
                }
                renderer.materials = mats;
            }
        }

        /// <summary>
        /// Restores the creature to full opacity after SetTransparency().
        /// </summary>
        public void ResetTransparency()
        {
            transparencyAlpha = 1f;

            foreach (string clip in ClipNames)
            {
                MeshRenderer renderer = GetClipRenderer(clip);
                if (renderer == null)
                {
                    continue;
                }

                Material[] mats = renderer.materials;
                foreach (Material m in mats)
                {
                    Color c = m.color;
                    c.a = 1f;
                    m.color = c;
                    SetMaterialOpaque(m);
                }
                renderer.materials = mats;
            }
        }

        // The per-instance materials of a creature that is flashing, kept so the flash can
        // set their alpha every frame without going back through the renderers.
        private Material[] blinkMaterials = null;

        /// <summary>
        /// Readies the creature to flash: switches its materials to alpha blending once, so
        /// SetBlinkAlpha is then only a colour write. Finish with EndBlink().
        /// </summary>
        public void BeginBlink()
        {
            if (blinkMaterials != null)
            {
                return;
            }

            List<Material> instances = new List<Material>();
            foreach (string clip in ClipNames)
            {
                MeshRenderer renderer = GetClipRenderer(clip);
                if (renderer == null)
                {
                    continue;
                }

                Material[] mats = renderer.materials; // per-instance copies
                foreach (Material m in mats)
                {
                    SetMaterialFade(m);
                    instances.Add(m);
                }
                renderer.materials = mats;
            }

            blinkMaterials = instances.ToArray();
        }

        /// <summary>
        /// Sets the flashing creature's opacity (0..1) and the colour its model is tinted
        /// with (white = no tint). No effect outside BeginBlink / EndBlink.
        /// </summary>
        public void SetBlink(float alpha, Color tint)
        {
            if (blinkMaterials == null)
            {
                return;
            }

            tint.a = alpha;
            foreach (Material m in blinkMaterials)
            {
                m.color = tint;
            }
        }

        /// <summary>Stops the flash and puts the creature back to full opacity.</summary>
        public void EndBlink()
        {
            if (blinkMaterials == null)
            {
                return;
            }

            // Back to the untinted colour first; ResetTransparency only restores the alpha.
            foreach (Material m in blinkMaterials)
            {
                m.color = Color.white;
            }

            blinkMaterials = null;
            ResetTransparency();
        }

        /// <summary>
        /// Picks the first idle frame immediately, before any Update runs.
        /// Without this the prefab spawns with all three clips enabled and the
        /// creature renders as all three animation frames stacked on top of each
        /// other for a frame or two - the visible "flash" on appearance.
        /// CreatureClip only propagates enabled to its children in its own Update,
        /// so the children are set here directly rather than relying on it.
        /// </summary>
        public void InitializeClipVisibility()
        {
            for (int i = 0; i < ClipNames.Length; i++)
            {
                Transform clip = transform.Find(ClipNames[i]);
                if (clip == null)
                {
                    continue;
                }

                bool visible = (i == 0);

                MeshRenderer clipRenderer = clip.GetComponent<MeshRenderer>();
                if (clipRenderer != null)
                {
                    clipRenderer.enabled = visible;
                }

                foreach (MeshRenderer child in clip.GetComponentsInChildren<MeshRenderer>(true))
                {
                    child.enabled = visible;
                }
            }
        }

        private MeshRenderer GetClipRenderer(string clipName)
        {
            Transform clip = transform.Find(clipName);
            if (clip == null || clip.childCount == 0)
            {
                return null;
            }
            Transform def = clip.GetChild(0).Find("default");
            return def != null ? def.GetComponent<MeshRenderer>() : null;
        }

        private static void SetMaterialFade(Material m)
        {
            m.SetFloat("_Mode", 2f); // Fade
            m.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            m.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            m.SetInt("_ZWrite", 0);
            m.DisableKeyword("_ALPHATEST_ON");
            m.EnableKeyword("_ALPHABLEND_ON");
            m.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            m.renderQueue = (int)RenderQueue.Transparent;
        }

        private static void SetMaterialOpaque(Material m)
        {
            m.SetFloat("_Mode", 0f); // Opaque
            m.SetInt("_SrcBlend", (int)BlendMode.One);
            m.SetInt("_DstBlend", (int)BlendMode.Zero);
            m.SetInt("_ZWrite", 1);
            m.DisableKeyword("_ALPHATEST_ON");
            m.DisableKeyword("_ALPHABLEND_ON");
            m.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            m.renderQueue = -1; // use the shader's default queue (Geometry)
        }

        // Start is called before the first frame update
        void Start()
        {
        }

        // Update is called once per frame
        void Update()
        {
            //if (isMoving)
            //{
            //    moveCount++;

            //    if (moveCount > 90)
            //    {
            //        isMoving = false;
            //        moveCount = 0;

            //        // update position
            //        creature.Position = path.Desitination;

            //        // remove component
            //        Destroy(gameObject.GetComponent<CreatureWalk>());
            //    }
            //}

        }

        // A frozen creature (a bound captive, say) trembles in place: it shakes from side to
        // side ShakesPerSecond times a second, so it reads as held fast at a glance.
        private const float ShakesPerSecond = 6f;
        private const float ShakeAmplitude = 0.07f;   // world units, a little over 3% of a tile

        // The animation clips' resting local positions while a shake is under way. The three
        // clips are what the eye sees, and the Animator only toggles them on and off, so
        // shaking them leaves the creature's own transform (walking, placement) untouched.
        private Vector3[] shakeRestPositions = null;

        void LateUpdate()
        {
            bool frozen = creature != null && creature.Hp > 0
                && creature.Effects.Contains(CreatureEffects.Frozen);

            if (frozen)
            {
                ApplyShake();
            }
            else if (shakeRestPositions != null)
            {
                EndShake();
            }
        }

        private void ApplyShake()
        {
            if (shakeRestPositions == null)
            {
                shakeRestPositions = new Vector3[ClipNames.Length];
                for (int i = 0; i < ClipNames.Length; i++)
                {
                    Transform clip = transform.Find(ClipNames[i]);
                    shakeRestPositions[i] = clip != null ? clip.localPosition : Vector3.zero;
                }
            }

            // Side to side as the player sees it: along the camera's right, flat on the ground.
            Vector3 right = Camera.main != null ? Camera.main.transform.right : Vector3.right;
            right.y = 0f;
            right = right.sqrMagnitude > 1e-6f ? right.normalized : Vector3.right;

            float offset = Mathf.Sin(Time.time * ShakesPerSecond * 2f * Mathf.PI) * ShakeAmplitude;
            for (int i = 0; i < ClipNames.Length; i++)
            {
                Transform clip = transform.Find(ClipNames[i]);
                if (clip != null)
                {
                    clip.localPosition = shakeRestPositions[i] + transform.InverseTransformVector(right * offset);
                }
            }
        }

        private void EndShake()
        {
            for (int i = 0; i < ClipNames.Length; i++)
            {
                Transform clip = transform.Find(ClipNames[i]);
                if (clip != null)
                {
                    clip.localPosition = shakeRestPositions[i];
                }
            }

            shakeRestPositions = null;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            PlayerInterface.getDefault().onSelectedPosition(this.creature.Position);
        }

        public void SetActioned(bool actioned)
        {
            creature.HasActioned = actioned;
            if (actioned)
            {
                SetGreyout(true);
            }
            else
            {
                SetGreyout(false);
            }
        }
    }
}
