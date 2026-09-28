using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace WindingTale.MapObjects.CreatureIcon
{
    public class CreatureDying : MonoBehaviour
    {
        private float initialRotateSpeed = 192.0f;

        private Vector3 rotationDirection = new Vector3(0, 2, 0);

        private DateTime startTime;

        private static float dyingDuration = 1250f;

        private bool rotationFinished = false;

        void Start()
        {
            startTime = DateTime.Now;
        }

        void Update()
        {
            if (rotationFinished) return;

            float elapsed = (float)(DateTime.Now - startTime).TotalMilliseconds;
            float t = Mathf.Clamp01(elapsed / dyingDuration);
            float rotateSpeed = Mathf.Lerp(initialRotateSpeed, initialRotateSpeed * 2f, t);
            transform.Rotate(rotateSpeed * rotationDirection * Time.deltaTime);

            if (elapsed > dyingDuration)
            {
                rotationFinished = true;
                SpawnExplosion();
            }
        }

        private void SpawnExplosion()
        {
            // Measure creature bounds before hiding, then hide renderers
            Bounds creatureBounds = new();
            bool first = true;
            foreach (var r in GetComponentsInChildren<Renderer>())
            {
                if (first) { creatureBounds = r.bounds; first = false; }
                else creatureBounds.Encapsulate(r.bounds);
                r.enabled = false;
            }

            // Frames come from the original 040-04..11 sprite (death_explosion_to_obj.py):
            // 1 OBJ unit = 1 original pixel, one submesh per sprite colour.
            const int frameCount = 8;
            Mesh[] frames = new Mesh[frameCount];
            Material[][] frameMaterials = new Material[frameCount][];

            for (int i = 0; i < frameCount; i++)
            {
                GameObject model = Resources.Load<GameObject>($"Animations/exploration/explosion_{i:D2}");
                if (model != null)
                {
                    MeshFilter mf = model.GetComponentInChildren<MeshFilter>();
                    if (mf != null) frames[i] = mf.sharedMesh;
                    MeshRenderer mr = model.GetComponentInChildren<MeshRenderer>();
                    if (mr != null) frameMaterials[i] = FireMaterials(mr.sharedMaterials);
                }
            }

            GameObject explosionGO = new("Explosion");
            // Centred on the creature's body; the fireball bulges more above than below.
            explosionGO.transform.position = first ? transform.position : creatureBounds.center;
            // 48 sprite pixels to one map tile: the widest frame (72 px) spans about 1.5 tiles.
            explosionGO.transform.localScale = Vector3.one * (WorldUnitsPerTile / SpritePixelsPerTile);

            explosionGO.AddComponent<MeshFilter>();
            MeshRenderer explosionMR = explosionGO.AddComponent<MeshRenderer>();
            explosionMR.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            // Orange flash on the ground and the creatures round it, dying with the fire.
            GameObject lightGO = new("ExplosionLight");
            lightGO.transform.SetParent(explosionGO.transform, false);
            lightGO.transform.localPosition = Vector3.up * 12f;
            Light flash = lightGO.AddComponent<Light>();
            flash.type = LightType.Point;
            flash.color = new Color(1f, 0.55f, 0.2f);
            flash.range = WorldUnitsPerTile * 2f;
            flash.renderMode = LightRenderMode.ForcePixel;
            flash.shadows = LightShadows.None;

            ExplosionPlayer player = explosionGO.AddComponent<ExplosionPlayer>();
            player.frames = frames;
            player.frameMaterials = frameMaterials;
            player.frameLight = flash;
            player.lightIntensities = FlashIntensities;
            player.framesPerSecond = 6f;
            player.destroyOnEnd = true;

            StartCoroutine(WaitForExplosionAndDestroy(explosionGO));
        }

        private const float WorldUnitsPerTile = 2f;
        private const float SpritePixelsPerTile = 48f;
        private static readonly float[] FlashIntensities = { 4f, 3f, 2f, 1.4f, 0.8f, 0.4f, 0.15f, 0f };

        /// <summary>Self-lit copies of the imported per-colour materials, shared by every explosion.</summary>
        private static readonly Dictionary<Color, Material> fireMaterials = new();

        private static Material[] FireMaterials(Material[] imported)
        {
            Shader shader = Shader.Find("Custom/DeathExplosion");
            if (shader == null) return imported;

            Material[] result = new Material[imported.Length];
            for (int i = 0; i < imported.Length; i++)
            {
                Color c = imported[i] != null ? imported[i].color : Color.white;
                if (!fireMaterials.TryGetValue(c, out Material m) || m == null)
                {
                    m = new Material(shader) { color = c };
                    fireMaterials[c] = m;
                }
                result[i] = m;
            }
            return result;
        }

        private IEnumerator WaitForExplosionAndDestroy(GameObject explosionGO)
        {
            while (explosionGO != null)
                yield return null;

            Destroy(gameObject);
        }
    }
}
