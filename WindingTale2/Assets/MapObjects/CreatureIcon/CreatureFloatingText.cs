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
        public const float RiseDistance = 2.8f;

        /// <summary>
        /// Where it starts above the creature's feet, in world units: over the creature's
        /// body (a creature stands about 2 units tall). It climbs to about twice the
        /// creature's height, StartHeight + RiseDistance.
        /// </summary>
        public const float StartHeight = 1.2f;

        /// <summary>Text size. TextMeshPro world text is about a tenth of a unit per point.</summary>
        public const float FontSize = 14f * 0.7f * 0.6f;

        // The part of the lifetime it holds full opacity for, before fading over the rest.
        private const float FadeStartFraction = 0.6f;

        // TMP's distance-field shader keeps its depth test in this property; "Always" (8)
        // draws the text over whatever stands in front of it.
        private const string DepthTestProperty = "unity_GUIZTestMode";

        // The rainbow sheen on a stat potion's text, at full strength: pure rainbow over the
        // base colour, fully saturated, the whole spectrum across one line of text and
        // running fast enough to shimmer.
        private const float SheenStrength = 1f;
        private const float SheenSaturation = 1f;
        private const float SheenSpeed = 3f;
        private const float SheenBandsPerUnit = 0.9f;

        // Sheen text is drawn larger than a plain number, and pulses gently while it rises.
        private const float SheenSizeScale = 1.35f;
        private const float SheenPulseAmount = 0.08f;
        private const float SheenPulseSpeed = 14f;

        private TextMeshPro textMesh = null;
        private Vector3 origin = Vector3.zero;
        private Color baseColor = Color.white;
        private float elapsed = 0f;
        private bool iridescent = false;

        /// <summary>
        /// Puts a floating line over a creature and returns it, or null when the creature
        /// has no icon on the map (nothing to float over). An iridescent line carries a
        /// shifting rainbow sheen over its colour, like the surface of a soap bubble.
        /// </summary>
        public static CreatureFloatingText Spawn(Creature creature, string text, Color color, bool iridescent = false)
        {
            if (creature == null || string.IsNullOrEmpty(text))
            {
                return null;
            }

            GameObject textObject = new GameObject("floating_text");
            CreatureFloatingText floating = textObject.AddComponent<CreatureFloatingText>();
            floating.Initialize(creature.transform.position, text, color, iridescent);
            return floating;
        }

        private void Initialize(Vector3 creatureFeet, string text, Color color, bool iridescent)
        {
            this.iridescent = iridescent;
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
            textMesh.fontSize = iridescent ? FontSize * SheenSizeScale : FontSize;
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

            // A dark edge makes the saturated rainbow stand out; a soft white glow round it
            // makes the sheen text shine.
            textMesh.outlineColor = iridescent ? new Color32(20, 0, 40, 255) : new Color32(0, 0, 0, 255);
            if (iridescent)
            {
                material.EnableKeyword(ShaderUtilities.Keyword_Glow);
                material.SetColor(ShaderUtilities.ID_GlowColor, new Color(1f, 1f, 1f, 0.75f));
                material.SetFloat(ShaderUtilities.ID_GlowOuter, 0.6f);
                material.SetFloat(ShaderUtilities.ID_GlowPower, 0.6f);
            }

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

                if (iridescent)
                {
                    ApplySheen(color);
                    transform.localScale = Vector3.one * (1f + SheenPulseAmount * Mathf.Sin(elapsed * SheenPulseSpeed));
                }
            }

            FaceCamera();
        }

        /// <summary>
        /// Recolours every glyph corner: the base colour with a rainbow band mixed in, the
        /// band's hue set by where the corner sits along the line and by the time, so the
        /// colours slide across the text like light over a soap bubble. Per-vertex colours
        /// rather than a shader, so it rides on the ordinary text material.
        /// </summary>
        private void ApplySheen(Color tint)
        {
            textMesh.ForceMeshUpdate();
            TMP_TextInfo info = textMesh.textInfo;

            for (int i = 0; i < info.characterCount; i++)
            {
                TMP_CharacterInfo character = info.characterInfo[i];
                if (!character.isVisible)
                {
                    continue;
                }

                Color32[] colors = info.meshInfo[character.materialReferenceIndex].colors32;
                Vector3[] vertices = info.meshInfo[character.materialReferenceIndex].vertices;
                for (int corner = 0; corner < 4; corner++)
                {
                    int index = character.vertexIndex + corner;
                    Vector3 v = vertices[index];
                    float hue = Mathf.Repeat(elapsed * SheenSpeed * 0.5f + (v.x + v.y * 0.6f) * SheenBandsPerUnit, 1f);
                    Color rainbow = Color.HSVToRGB(hue, SheenSaturation, 1f);
                    Color mixed = Color.Lerp(tint, rainbow, SheenStrength);
                    mixed.a = tint.a;
                    colors[index] = mixed;
                }
            }

            textMesh.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);
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
