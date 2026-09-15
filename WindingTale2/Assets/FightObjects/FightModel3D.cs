using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using WindingTale.Core.Common;
using WindingTale.MapObjects.CreatureIcon;

namespace WindingTale.FightObjects
{
    /// <summary>
    /// Shows a fight body as the 3D models built from its 2D frames
    /// (Tools/generate_fight.bat -> Resources/Fights3D/NNN) instead of the sprite.
    ///
    /// The Animator is left in charge of everything. It still plays the sprite clips,
    /// fires onAttackHit / onAttackFinish, is frozen by MagicRunner and read for the
    /// remote-attack frame; the SpriteRenderer is only switched off. Every LateUpdate this
    /// reads which frame the Animator put on the renderer and shows that frame's model,
    /// so timing, events and freezes are the 2D ones by construction. A creature with no
    /// models, or a frame that was not converted, falls back to the sprite.
    ///
    /// Placement: one voxel is one pixel of the original 320x210 frame, laid over the
    /// sprite's own rect, so from the battle camera the model covers the drawing it was
    /// built from. The model is then pushed back along the camera's rays (moved away and
    /// scaled up by the same factor, which leaves its picture unchanged) until its front
    /// surface is behind the sprite plane -- the magic effects and hit flashes are drawn
    /// just in front of that plane and would otherwise disappear into the body.
    /// </summary>
    public class FightModel3D : MonoBehaviour
    {
        /// <summary>Global switch; off shows the 2D sprites everywhere.</summary>
        public static bool Enabled = true;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private const KeyCode ToggleKey = KeyCode.F8;
        private static int toggledFrame = -1;
#endif

        // Width of the original fight frame in pixels; the sprites are it scaled up.
        private const float ScreenWidth = 320f;
        // How far behind the sprite plane the model's front surface ends up, world units.
        private const float BehindPlane = 0.02f;

        private static readonly Regex FrameName = new Regex(@"Fight-(\d{3})-(\d)-(\d{2})");

        [Serializable]
        private class Crop
        {
            public int x0;
            public int y0;
            public int x1;
            public int y1;
        }

        [Serializable]
        private class FrameEntry
        {
            public string animation;
            public int index;
            public string body;
            public string fx;
        }

        [Serializable]
        private class Manifest
        {
            public string creature;
            public Crop crop;
            public int[] size;
            public int midPlaneY;
            public int frontY;
            public float objScale;
            public FrameEntry[] frames;
        }

        private class FrameModel
        {
            public GameObject Body;
            public GameObject Fx;
        }

        private int animationId = -1;
        private Manifest manifest;
        private Transform root;
        private readonly Dictionary<string, FrameModel> frames = new Dictionary<string, FrameModel>();
        private FrameModel shown;
        private SpriteRenderer spriteRenderer;
        private Camera sceneCamera;
        private MaterialPropertyBlock tintBlock;
        private Color appliedTint = Color.white;

        // Signs that turn the imported mesh's axes back into voxel axes, whatever the
        // OBJ importer did with handedness (see MeasureAxes).
        private Vector3 axisSign = Vector3.one;
        private static Shader fxShader;
        // One fx material per palette texture, like CreatureMaterial: every frame of a
        // creature shares its palette. Dropped when the texture it was made for is gone.
        private static readonly Dictionary<Texture, Material> fxMaterials = new Dictionary<Texture, Material>();

        /// <summary>
        /// Gives <paramref name="body"/> the 3D models of <paramref name="animationId"/>, or
        /// puts its sprite back when that animation has none. Safe to call again when the
        /// body is rebound to another creature.
        /// </summary>
        public static FightModel3D Attach(GameObject body, int animationId)
        {
            FightModel3D model = body.GetComponent<FightModel3D>();
            if (model == null)
            {
                if (!HasModels(animationId))
                {
                    return null;
                }
                model = body.AddComponent<FightModel3D>();
            }
            model.Load(animationId);
            return model;
        }

        private static string FolderOf(int animationId)
        {
            return string.Format("Fights3D/{0}/", StringUtils.Digit3(animationId));
        }

        private static bool HasModels(int animationId)
        {
            return Resources.Load<TextAsset>(FolderOf(animationId) + "Fight_" + StringUtils.Digit3(animationId)) != null;
        }

        private void Load(int newAnimationId)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            if (newAnimationId == animationId && root != null)
            {
                return;
            }
            Clear();
            animationId = newAnimationId;

            string folder = FolderOf(animationId);
            TextAsset json = Resources.Load<TextAsset>(folder + "Fight_" + StringUtils.Digit3(animationId));
            manifest = json != null ? JsonUtility.FromJson<Manifest>(json.text) : null;
            if (manifest == null || manifest.frames == null || manifest.crop == null)
            {
                manifest = null;
                ShowSprite();
                return;
            }
            if (manifest.objScale <= 0)
            {
                manifest.objScale = 0.1f;
            }

            root = new GameObject("Model3D").transform;
            root.SetParent(transform, false);

            // Frames with identical drawings name the same model; build each model once.
            Dictionary<string, GameObject> spawned = new Dictionary<string, GameObject>();
            bool measured = false;
            foreach (FrameEntry entry in manifest.frames)
            {
                bool missing;
                GameObject body = SpawnShared(spawned, folder, entry.body, false, out missing);
                GameObject fx = SpawnShared(spawned, folder, entry.fx, true, out bool fxMissing);
                if (missing || fxMissing)
                {
                    // a model the manifest names is not in the project: let the sprite show
                    continue;
                }
                if (!measured && (body != null || fx != null))
                {
                    MeasureAxes(body != null ? body : fx);
                    measured = true;
                }
                // Both null is a real frame too: the creature has vanished (mid-spell).
                FrameModel frame = new FrameModel { Body = body, Fx = fx };
                int animationType = entry.animation == "idle" ? 1 : entry.animation == "attack" ? 2 : 3;
                frames[FrameKey(animationType, entry.index)] = frame;
            }

            sceneCamera = FindSceneCamera();
            Sync();
        }

        private GameObject SpawnShared(Dictionary<string, GameObject> spawned, string folder, string file, bool isFx,
            out bool missing)
        {
            missing = false;
            if (string.IsNullOrEmpty(file))
            {
                return null;
            }
            GameObject instance;
            if (!spawned.TryGetValue(file, out instance))
            {
                instance = Spawn(folder, file, isFx);
                spawned[file] = instance;
            }
            missing = instance == null;
            return instance;
        }

        private GameObject Spawn(string folder, string file, bool isFx)
        {
            if (string.IsNullOrEmpty(file))
            {
                return null;
            }
            GameObject prefab = Resources.Load<GameObject>(folder + System.IO.Path.GetFileNameWithoutExtension(file));
            if (prefab == null)
            {
                Debug.LogWarning("[FightModel3D] missing model " + folder + file);
                return null;
            }

            GameObject instance = Instantiate(prefab, root, false);
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one;
            foreach (Transform part in instance.GetComponentsInChildren<Transform>(true))
            {
                part.gameObject.layer = gameObject.layer;
            }

            if (isFx)
            {
                ApplyFxMaterial(instance);
            }
            else
            {
                CreatureMaterial.Apply(instance);
            }
            foreach (Renderer renderer in instance.GetComponentsInChildren<Renderer>(true))
            {
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }
            instance.SetActive(false);
            return instance;
        }

        private static void ApplyFxMaterial(GameObject instance)
        {
            if (fxShader == null)
            {
                fxShader = Shader.Find("Custom/FightFx");
                if (fxShader == null)
                {
                    Debug.LogError("[FightModel3D] Shader 'Custom/FightFx' not found!");
                    return;
                }
            }
            foreach (MeshRenderer renderer in instance.GetComponentsInChildren<MeshRenderer>(true))
            {
                Material original = renderer.sharedMaterial;
                Texture palette = original != null && original.HasProperty("_MainTex") ? original.GetTexture("_MainTex") : null;
                Material material = null;
                if (palette != null)
                {
                    fxMaterials.TryGetValue(palette, out material);
                }
                if (material == null || material.mainTexture == null)
                {
                    material = new Material(fxShader);
                    material.mainTexture = palette;
                    if (palette != null)
                    {
                        fxMaterials[palette] = material;
                    }
                }
                renderer.sharedMaterial = material;
            }
        }

        /// <summary>
        /// The exporter writes a voxel (x, y, z) as objScale * (x, z, -y), all coordinates
        /// positive; the importer may mirror an axis on the way in. Every voxel coordinate is
        /// positive, so the side of zero each axis of the imported model lands on says which
        /// way it points.
        /// </summary>
        private void MeasureAxes(GameObject instance)
        {
            bool any = false;
            Bounds bounds = new Bounds();
            Matrix4x4 toRoot = instance.transform.worldToLocalMatrix;
            foreach (MeshFilter filter in instance.GetComponentsInChildren<MeshFilter>(true))
            {
                if (filter.sharedMesh == null)
                {
                    continue;
                }
                Matrix4x4 m = toRoot * filter.transform.localToWorldMatrix;
                Bounds b = filter.sharedMesh.bounds;
                for (int i = 0; i < 8; i++)
                {
                    Vector3 corner = b.center + Vector3.Scale(b.extents,
                        new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                    Vector3 p = m.MultiplyPoint3x4(corner);
                    if (!any)
                    {
                        bounds = new Bounds(p, Vector3.zero);
                        any = true;
                    }
                    else
                    {
                        bounds.Encapsulate(p);
                    }
                }
            }
            if (!any)
            {
                return;
            }
            // x = +voxel x, y = +voxel z, z = -voxel y as written.
            axisSign = new Vector3(bounds.center.x >= 0 ? 1 : -1,
                                   bounds.center.y >= 0 ? 1 : -1,
                                   bounds.center.z <= 0 ? 1 : -1);
        }

        private Camera FindSceneCamera()
        {
            // Not Camera.main: the battle scene is loaded over the field scene and both have
            // one tagged MainCamera.
            foreach (Camera camera in Camera.allCameras)
            {
                if (camera.gameObject.scene == gameObject.scene)
                {
                    return camera;
                }
            }
            return null;
        }

        private static string FrameKey(int animationType, int index)
        {
            return animationType + "-" + index.ToString("D2");
        }

        private void Clear()
        {
            frames.Clear();
            shown = null;
            manifest = null;
            if (root != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(root.gameObject);
                }
                else
                {
                    DestroyImmediate(root.gameObject);
                }
                root = null;
            }
        }

        private void ShowSprite()
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.enabled = true;
            }
            Show(null);
        }

        private void Show(FrameModel frame)
        {
            if (frame == shown)
            {
                return;
            }
            if (shown != null)
            {
                if (shown.Body != null) shown.Body.SetActive(false);
                if (shown.Fx != null) shown.Fx.SetActive(false);
            }
            shown = frame;
            if (shown != null)
            {
                if (shown.Body != null) shown.Body.SetActive(true);
                if (shown.Fx != null) shown.Fx.SetActive(true);
                appliedTint = new Color(-1, -1, -1, -1);   // re-apply the tint to the new frame
            }
        }

        void LateUpdate()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (Input.GetKeyDown(ToggleKey) && toggledFrame != Time.frameCount)
            {
                toggledFrame = Time.frameCount;
                Enabled = !Enabled;
            }
#endif
            Sync();
        }

        /// <summary>
        /// Shows the model of the frame the sprite renderer holds and lines it up with the
        /// sprite. LateUpdate does this every frame; editor tools that render outside play
        /// mode call it themselves.
        /// </summary>
        public void Sync()
        {
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
            }
            Sprite sprite = spriteRenderer != null ? spriteRenderer.sprite : null;
            FrameModel frame = null;
            if (Enabled && manifest != null && sprite != null)
            {
                Match match = FrameName.Match(sprite.name);
                if (match.Success && int.Parse(match.Groups[1].Value) == animationId)
                {
                    frames.TryGetValue(FrameKey(int.Parse(match.Groups[2].Value), int.Parse(match.Groups[3].Value)), out frame);
                }
            }

            if (frame == null)
            {
                ShowSprite();
                return;
            }

            spriteRenderer.enabled = false;
            Show(frame);
            Place(sprite);
            ApplyTint();
        }

        private void Place(Sprite sprite)
        {
            float unitsPerPixel = sprite.rect.width / ScreenWidth / sprite.pixelsPerUnit;
            float left = sprite.bounds.min.x;
            float top = sprite.bounds.max.y;

            // One voxel of depth is as deep as a pixel is wide, in world terms, whatever the
            // body's own z scale is.
            Vector3 lossy = transform.lossyScale;
            float depthPerVoxel = unitsPerPixel * Mathf.Abs(lossy.x) / Mathf.Max(Mathf.Abs(lossy.z), 1e-6f);

            Transform cameraTransform = sceneCamera != null ? sceneCamera.transform : null;
            float towardCamera = -1f;
            if (cameraTransform != null)
            {
                Vector3 toCamera = transform.InverseTransformDirection(cameraTransform.position - transform.position);
                towardCamera = toCamera.z >= 0 ? 1f : -1f;
            }

            float voxelsPerUnit = 1f / manifest.objScale;
            float midDepth = manifest.midPlaneY + 0.5f;
            Vector3 localPosition = new Vector3(
                left + manifest.crop.x0 * unitsPerPixel,
                top - manifest.crop.y1 * unitsPerPixel,
                towardCamera * midDepth * depthPerVoxel);
            Vector3 localScale = new Vector3(
                axisSign.x * voxelsPerUnit * unitsPerPixel,
                axisSign.y * voxelsPerUnit * unitsPerPixel,
                towardCamera * axisSign.z * voxelsPerUnit * depthPerVoxel);

            float push = 1f;
            if (cameraTransform != null)
            {
                Vector3 eye = cameraTransform.position;
                Vector3 forward = cameraTransform.forward;
                float planeDistance = Vector3.Dot(transform.position - eye, forward);
                Vector3 front = transform.TransformPoint(new Vector3(0, 0,
                    towardCamera * (midDepth - manifest.frontY) * depthPerVoxel));
                float frontDistance = Vector3.Dot(front - eye, forward);
                if (frontDistance > 0.01f && planeDistance + BehindPlane > frontDistance)
                {
                    push = (planeDistance + BehindPlane) / frontDistance;
                }
                Vector3 world = transform.TransformPoint(localPosition);
                root.position = eye + (world - eye) * push;
            }
            else
            {
                root.localPosition = localPosition;
            }
            root.localRotation = Quaternion.identity;
            root.localScale = localScale * push;
        }

        /// <summary>
        /// EnemyHitEffect flashes a body by setting its renderer's material colour; carry that
        /// onto the model so a hit still reads.
        /// </summary>
        private void ApplyTint()
        {
            Material material = spriteRenderer.sharedMaterial;
            Color tint = material != null && material.HasProperty("_Color") ? material.color : Color.white;
            if (tint == appliedTint || shown == null || shown.Body == null)
            {
                return;
            }
            appliedTint = tint;
            if (tintBlock == null)
            {
                tintBlock = new MaterialPropertyBlock();
            }
            foreach (Renderer renderer in shown.Body.GetComponentsInChildren<Renderer>(true))
            {
                renderer.GetPropertyBlock(tintBlock);
                tintBlock.SetColor("_Color", tint);
                renderer.SetPropertyBlock(tintBlock);
            }
        }

        void OnDestroy()
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.enabled = true;
            }
        }
    }
}
