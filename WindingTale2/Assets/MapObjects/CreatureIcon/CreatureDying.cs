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

            LoadFrames();
            Mesh[] frames = cachedFrames;
            Material[][] frameMaterials = cachedFrameMaterials;

            GameObject explosionGO = new("Explosion");
            // Centred on the creature's body; the fireball bulges more above than below.
            explosionGO.transform.position = first ? transform.position : creatureBounds.center;
            // 64 sprite pixels to one map tile: the widest frame (72 px) spans about 1.13
            // tiles, under 130% of a tile's area (user 2026-09-28).
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
            // The fireball (040-04) and its bursting open (040-05) a little longer; the ring,
            // the embers and the last sparks after them go quickly. The first frame is already a closed fireball at
            // ~80% of the full size (death_explosion_to_obj.py FRAME_SCALE), so no scale-up.
            player.frameDurations = FrameSeconds;
            player.destroyOnEnd = true;

            StartCoroutine(WaitForExplosionAndDestroy(explosionGO));
        }

        private const float WorldUnitsPerTile = 2f;
        private const float SpritePixelsPerTile = 64f;
        private static readonly float[] FlashIntensities = { 4f, 3.2f, 1.8f, 0.8f, 0.4f, 0.2f, 0.1f, 0f };
        private static readonly float[] FrameSeconds = { 0.15f, 0.15f, 0.12f, 0.1f, 0.08f, 0.08f, 0.08f, 0.08f };

        // Frames come from the original 040-04..11 sprite (death_explosion_to_obj.py):
        // 1 OBJ unit = 1 original pixel, one "g m_RRGGBB" group per sprite colour.
        private const int FrameCount = 8;
        private static Mesh[] cachedFrames;
        private static Material[][] cachedFrameMaterials;

        /// <summary>
        /// Builds the frame meshes once. Unity imports every colour group of an explosion OBJ
        /// as a mesh of its own (a child object each), so a frame is all its children merged
        /// into one mesh, one submesh per colour, with the matching self-lit materials. Taking
        /// only the first child drew nothing but the darkest red specks of each frame.
        /// </summary>
        private static void LoadFrames()
        {
            // (A destroyed mesh compares equal to null, so a stale cache is rebuilt.)
            if (cachedFrames != null && cachedFrames[0] != null) return;

            cachedFrames = new Mesh[FrameCount];
            cachedFrameMaterials = new Material[FrameCount][];

            for (int i = 0; i < FrameCount; i++)
            {
                GameObject model = Resources.Load<GameObject>($"Animations/exploration/explosion_{i:D2}");
                if (model == null) continue;

                List<CombineInstance> parts = new();
                List<Material> materials = new();
                Matrix4x4 toRoot = model.transform.worldToLocalMatrix;
                foreach (MeshFilter mf in model.GetComponentsInChildren<MeshFilter>(true))
                {
                    MeshRenderer mr = mf.GetComponent<MeshRenderer>();
                    if (mf.sharedMesh == null || mr == null) continue;

                    for (int s = 0; s < mf.sharedMesh.subMeshCount; s++)
                    {
                        parts.Add(new CombineInstance
                        {
                            mesh = mf.sharedMesh,
                            subMeshIndex = s,
                            transform = toRoot * mf.transform.localToWorldMatrix,
                        });
                        Material[] shared = mr.sharedMaterials;
                        materials.Add(shared.Length > 0 ? shared[Mathf.Min(s, shared.Length - 1)] : null);
                    }
                }
                if (parts.Count == 0) continue;

                Mesh frame = new() { name = $"explosion_{i:D2}" };
                frame.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
                frame.CombineMeshes(parts.ToArray(), false, true);
                cachedFrames[i] = frame;
                cachedFrameMaterials[i] = FireMaterials(materials.ToArray());
            }
        }

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
