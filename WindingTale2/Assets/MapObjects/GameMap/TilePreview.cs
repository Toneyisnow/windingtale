using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using WindingTale.Core.Common;
using WindingTale.Core.Objects;
using WindingTale.MapObjects.CreatureIcon;

namespace WindingTale.MapObjects.GameMap
{
    /// <summary>
    /// Renders a close-up of one board tile into a texture, for the shape info panel:
    /// the tile's ground, plus whatever stands on it.
    ///
    /// The ground is always taken straight down, so the tile fills the whole picture;
    /// what stands on it is then taken separately and drawn over it:
    ///
    ///   creature on the tile   -> the creature, from 45 degrees up, enlarged
    ///   treasure chest         -> the chest, from 45 degrees up
    ///   obstacle (tree, house) -> the obstacle, straight down with the ground, so only
    ///                             the part of it that is over this tile shows
    ///
    /// A creature wins over what it stands on, the way an obstacle fades out under
    /// one on the board itself.
    ///
    /// The live objects are never photographed: the cursor, the range indicators and
    /// the fade that props and tiles are under while the cursor sits on them would all
    /// be in the picture. Instead each mesh part of the tile's contents is copied onto
    /// a stage high above the board, in the same map column so the map clip shader
    /// treats it exactly as it treats the original, with fresh opaque materials. The
    /// stage exists only for the length of one Render call.
    /// </summary>
    public class TilePreview : MonoBehaviour
    {
        /// <summary>Side of the square texture, in pixels: four per pixel of a 24 px tile.</summary>
        public const int TextureSize = 96;

        // How far above the board the stage floats. Far enough that neither the game
        // camera nor its shadows can ever reach it.
        private const float StageHeight = 200f;

        // Camera distance from the thing it looks at; the clip planes bracket it.
        private const float CameraDistance = 20f;
        private const float FarClip = 40f;

        // Pitch of the oblique view (creatures and chests), and the game camera's yaw:
        // facing -Z, so map up is screen up.
        private const float ObliquePitch = 45f;
        private const float CameraYaw = 180f;

        // Half the height of the picture of a tile seen from above: one tile is two
        // world units.
        private const float TileHalfSize = 1f;

        // The oblique view is fitted to what is on stage, never tighter than this.
        private const float MinObliqueSize = 1.05f;
        private const float ObliqueFit = 0.8f;

        // A creature is drawn this much larger than the oblique fit would draw it.
        private const float CreatureZoom = 1.3f;

        public RenderTexture Picture { get; private set; }

        private Camera previewCamera = null;
        private Transform stage = null;
        private GameObject stageObject = null;
        private GameObject cameraObject = null;

        // The stage's copies and their materials, destroyed once the picture is taken.
        private readonly List<GameObject> stageObjects = new List<GameObject>();
        private readonly List<Material> stageMaterials = new List<Material>();

        void Awake()
        {
            Picture = new RenderTexture(TextureSize, TextureSize, 24, RenderTextureFormat.ARGB32);
            Picture.name = "TilePreview";
            Picture.filterMode = FilterMode.Point;
            Picture.Create();

            // Both are scene roots, not children of this component's object: that one
            // sits under the UI canvas, whose scale would be inherited by every copy.
            stageObject = new GameObject("TilePreviewStage");
            stage = stageObject.transform;

            cameraObject = new GameObject("TilePreviewCamera");

            previewCamera = cameraObject.AddComponent<Camera>();
            previewCamera.enabled = false;
            previewCamera.orthographic = true;
            previewCamera.clearFlags = CameraClearFlags.SolidColor;
            previewCamera.backgroundColor = new Color(0f, 0f, 0f, 0f);
            previewCamera.nearClipPlane = 0.1f;
            previewCamera.farClipPlane = FarClip;
            previewCamera.allowHDR = false;
            previewCamera.allowMSAA = false;
            previewCamera.useOcclusionCulling = false;
            previewCamera.targetTexture = Picture;
        }

        void OnDestroy()
        {
            ClearStage();

            if (stageObject != null)
            {
                Destroy(stageObject);
            }

            if (cameraObject != null)
            {
                Destroy(cameraObject);
            }

            if (Picture != null)
            {
                Picture.Release();
                Destroy(Picture);
                Picture = null;
            }
        }

        /// <summary>
        /// Draws the tile at <paramref name="position"/> into <see cref="Picture"/>.
        /// <paramref name="creature"/> is whoever stands there, or null. Returns true
        /// when the picture is of something that moves (an idling creature, a flickering
        /// fire pillar), so the caller knows to take it again a moment later.
        /// </summary>
        public bool Render(GameMap gameMap, FDPosition position, FDCreature creature)
        {
            if (gameMap == null || position == null || previewCamera == null)
            {
                return false;
            }

            Vector3 tileCentre = gameMap.GetTileWorldCentre(position);
            Vector3 lift = Vector3.up * StageHeight;
            Vector3 stageCentre = tileCentre + lift;

            bool animated = false;

            // Pass 1: the ground, with an obstacle standing on it (taken from straight
            // above, so the picture has only the part of it over this tile).
            Bounds groundBounds = new Bounds(stageCentre, Vector3.zero);
            ShapesLayer shapes = gameMap.Shapes;
            MeshRenderer ground = shapes != null ? shapes.GetShapeRenderer(position) : null;
            if (ground != null)
            {
                AddParts(new MeshRenderer[] { ground }, lift, ref groundBounds);
            }

            Creature icon = creature != null ? gameMap.GetCreature(creature) : null;
            TreasureInstance chest = null;
            if (icon != null)
            {
                animated = true;
            }
            else
            {
                ObjectsLayer objects = gameMap.Objects;
                chest = objects != null ? objects.GetTreasureAt(position) : null;
                if (chest == null)
                {
                    ObstaclesLayer obstacles = gameMap.Obstacles;
                    if (obstacles != null)
                    {
                        foreach (ObstacleInstance obstacle in obstacles.GetObstaclesAt(position))
                        {
                            AddParts(obstacle.GetComponentsInChildren<MeshRenderer>(), lift, ref groundBounds);

                            ObstacleAnimation animation = obstacle.GetComponent<ObstacleAnimation>();
                            if (animation != null && animation.FrameCount > 1)
                            {
                                animated = true;
                            }
                        }
                    }
                }
            }

            previewCamera.orthographicSize = TileHalfSize;
            previewCamera.transform.SetPositionAndRotation(
                stageCentre + Vector3.up * CameraDistance, Quaternion.Euler(90f, CameraYaw, 0f));
            previewCamera.clearFlags = CameraClearFlags.SolidColor;
            previewCamera.Render();
            ClearStage();

            // Pass 2: the creature or chest, drawn over the ground already in the texture.
            if (icon != null || chest != null)
            {
                Bounds bounds = new Bounds(stageCentre, Vector3.zero);
                if (icon != null)
                {
                    // Fitted together with the ground, as it always was, so the creature
                    // keeps its place in the frame; only the zoom is new.
                    bounds = groundBounds;
                    // Stand it on the tile centre whatever the icon is doing: mid-walk it
                    // is still on its way there, and the picture is of the tile it arrives on.
                    Vector3 at = icon.transform.position;
                    Vector3 shift = new Vector3(tileCentre.x - at.x, StageHeight, tileCentre.z - at.z);
                    AddParts(icon.GetComponentsInChildren<MeshRenderer>(), shift, ref bounds);
                    PlaceObliqueCamera(bounds, CreatureZoom);
                }
                else
                {
                    AddParts(chest.GetComponentsInChildren<MeshRenderer>(), lift, ref bounds);
                    PlaceObliqueCamera(bounds, 1f);
                }

                previewCamera.clearFlags = CameraClearFlags.Depth;
                previewCamera.Render();
                previewCamera.clearFlags = CameraClearFlags.SolidColor;
                ClearStage();
            }

            return animated;
        }

        private void PlaceObliqueCamera(Bounds bounds, float zoom)
        {
            Quaternion rotation = Quaternion.Euler(ObliquePitch, CameraYaw, 0f);
            previewCamera.orthographicSize = Mathf.Max(MinObliqueSize, bounds.extents.magnitude * ObliqueFit) / zoom;
            previewCamera.transform.SetPositionAndRotation(
                bounds.center - rotation * Vector3.forward * CameraDistance, rotation);
        }

        /// <summary>
        /// Copies every mesh that is currently showing onto the stage, moved by
        /// <paramref name="shift"/>, and grows <paramref name="bounds"/> to hold it.
        /// Only what is enabled is copied: an animated model keeps its other frames
        /// disabled, and this picks up whichever one is up right now.
        /// </summary>
        private void AddParts(IEnumerable<MeshRenderer> renderers, Vector3 shift, ref Bounds bounds)
        {
            foreach (MeshRenderer source in renderers)
            {
                if (source == null || !source.enabled || !source.gameObject.activeInHierarchy)
                {
                    continue;
                }

                MeshFilter filter = source.GetComponent<MeshFilter>();
                if (filter == null || filter.sharedMesh == null)
                {
                    continue;
                }

                GameObject part = new GameObject("part");
                part.transform.SetParent(stage, false); // the stage is an untransformed root, so local is world
                part.transform.SetPositionAndRotation(source.transform.position + shift, source.transform.rotation);
                part.transform.localScale = source.transform.lossyScale;
                stageObjects.Add(part);

                part.AddComponent<MeshFilter>().sharedMesh = filter.sharedMesh;

                Material[] originals = source.sharedMaterials;
                Material[] copies = new Material[originals.Length];
                for (int i = 0; i < originals.Length; i++)
                {
                    copies[i] = originals[i] != null ? OpaqueCopy(originals[i]) : null;
                }

                MeshRenderer target = part.AddComponent<MeshRenderer>();
                target.sharedMaterials = copies;
                target.shadowCastingMode = ShadowCastingMode.Off;
                target.receiveShadows = false;
                target.lightProbeUsage = LightProbeUsage.Off;
                target.reflectionProbeUsage = ReflectionProbeUsage.Off;

                Bounds partBounds = source.bounds;
                partBounds.center += shift;
                bounds.Encapsulate(partBounds);
            }
        }

        /// <summary>
        /// A private copy of a material at full opacity: the originals of whatever is
        /// under the cursor are mid-fade, and the point of the picture is to show it
        /// as it really is. A faded prop (MapClipFade) goes back to the opaque clip
        /// shader, a faded tile or creature (a Standard-style fade mode) back to opaque.
        /// </summary>
        private Material OpaqueCopy(Material source)
        {
            Material copy = new Material(source);
            stageMaterials.Add(copy);

            if (copy.shader != null && copy.shader.name == "Custom/MapClipFade")
            {
                Shader opaque = Shader.Find("Custom/MapClip");
                if (opaque != null)
                {
                    copy.shader = opaque;
                }
            }

            if (copy.HasProperty("_Color"))
            {
                Color color = copy.color;
                color.a = 1f;
                copy.color = color;
            }

            if (copy.HasProperty("_Mode") && copy.GetFloat("_Mode") >= 2f)
            {
                copy.SetFloat("_Mode", 0f);
                copy.SetInt("_SrcBlend", (int)BlendMode.One);
                copy.SetInt("_DstBlend", (int)BlendMode.Zero);
                copy.SetInt("_ZWrite", 1);
                copy.DisableKeyword("_ALPHATEST_ON");
                copy.DisableKeyword("_ALPHABLEND_ON");
                copy.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                copy.renderQueue = -1;
            }

            return copy;
        }

        private void ClearStage()
        {
            foreach (GameObject part in stageObjects)
            {
                if (part != null)
                {
                    Destroy(part);
                }
            }
            stageObjects.Clear();

            foreach (Material material in stageMaterials)
            {
                if (material != null)
                {
                    Destroy(material);
                }
            }
            stageMaterials.Clear();
        }
    }
}
