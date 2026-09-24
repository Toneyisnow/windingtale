using TMPro;
using UnityEngine;
using UnityEngine.Rendering;

namespace WindingTale.MapObjects.CreatureIcon
{
    /// <summary>
    /// A short line of text that floats up from above a creature and fades out -- the
    /// "+30" of a healing spell, the "攻击提升" of a buff. It is world-space text that
    /// turns to face the camera every frame, so it reads the same however the player has
    /// rotated the view, and it is drawn on top of the map so no voxel building hides it.
    ///
    /// The object destroys itself when the float is over, which is what
    /// ActivityFactory.CreatureFloatingTextActivity polls for. Create one with Spawn.
    /// </summary>
    public class CreatureFloatingText : MonoBehaviour
    {
        /// <summary>Seconds from appearing to being gone.</summary>
        public const float Lifetime = 1.1f;

        /// <summary>How far it climbs over its lifetime, in world units (a tile is 2).</summary>
        public const float RiseDistance = 1.4f;

        /// <summary>Where it starts above the creature's feet, in world units.</summary>
        public const float StartHeight = 3.2f;

        /// <summary>Text size. TextMeshPro world text is about a tenth of a unit per point.</summary>
        public const float FontSize = 14f * 0.7f;

        // The part of the lifetime it holds full opacity for, before fading over the rest.
        private const float FadeStartFraction = 0.6f;

        // TMP's distance-field shader keeps its depth test in this property; "Always" (8)
        // draws the text over whatever stands in front of it.
        private const string DepthTestProperty = "unity_GUIZTestMode";

        private TextMeshPro textMesh = null;
        private Vector3 origin = Vector3.zero;
        private Color baseColor = Color.white;
        private float elapsed = 0f;

        /// <summary>
        /// Puts a floating line over a creature and returns it, or null when the creature
        /// has no icon on the map (nothing to float over).
        /// </summary>
        public static CreatureFloatingText Spawn(Creature creature, string text, Color color)
        {
            if (creature == null || string.IsNullOrEmpty(text))
            {
                return null;
            }

            GameObject textObject = new GameObject("floating_text");
            CreatureFloatingText floating = textObject.AddComponent<CreatureFloatingText>();
            floating.Initialize(creature.transform.position, text, color);
            return floating;
        }

        private void Initialize(Vector3 creatureFeet, string text, Color color)
        {
            origin = creatureFeet + Vector3.up * StartHeight;
            baseColor = color;
            transform.position = origin;

            textMesh = gameObject.AddComponent<TextMeshPro>();

            // The baked message atlases lack most glyphs; the same dynamic Chinese font
            // the save/load rows use rasterizes whatever this line needs.
            TMP_FontAsset font = ShoppingRecordDialog.GetRecordFont();
            if (font != null)
            {
                textMesh.font = font;
            }

            textMesh.text = text;
            textMesh.fontSize = FontSize;
            textMesh.fontStyle = FontStyles.Bold;
            textMesh.alignment = TextAlignmentOptions.Center;
            textMesh.textWrappingMode = TextWrappingModes.NoWrap;
            textMesh.color = color;

            // Draw over the map. fontMaterial is this text's own copy, so changing it
            // leaves the shared font asset alone.
            Material material = textMesh.fontMaterial;
            material.SetFloat(DepthTestProperty, (float)CompareFunction.Always);
            material.renderQueue = (int)RenderQueue.Overlay;
            textMesh.outlineWidth = 0.22f;
            textMesh.outlineColor = new Color32(0, 0, 0, 255);

            FaceCamera();
        }

        void LateUpdate()
        {
            elapsed += Time.deltaTime;
            if (elapsed >= Lifetime)
            {
                Destroy(gameObject);
                return;
            }

            float t = elapsed / Lifetime;

            // Climb fast at first and ease out, so the number settles where it can be read.
            float eased = 1f - (1f - t) * (1f - t);
            transform.position = origin + Vector3.up * (RiseDistance * eased);

            if (textMesh != null)
            {
                Color color = baseColor;
                color.a = t <= FadeStartFraction ? 1f : 1f - (t - FadeStartFraction) / (1f - FadeStartFraction);
                textMesh.color = color;
            }

            FaceCamera();
        }

        private void FaceCamera()
        {
            Camera camera = Camera.main;
            if (camera != null)
            {
                transform.rotation = camera.transform.rotation;
            }
        }
    }
}
