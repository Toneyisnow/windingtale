using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using WindingTale.Core.Common;
using WindingTale.Core.Definitions;
using WindingTale.Core.Map;
using WindingTale.MapObjects.Blocks;
using WindingTale.Scenes.GameFieldScene;

namespace WindingTale.MapObjects.GameMap
{

    public class ShapesLayer : MonoBehaviour
    {
        // Chapters that have not been remastered yet borrow this one's tile models.
        private const int FallbackShapeChapter = 1;

        private bool initialized = false;

        // One warning per field build, not one per tile.
        private bool warnedMissingShapeSet = false;

        private Material defaultMaterial = null;

        // Tile -> the shape's renderer, so indicators can fade the shape underneath
        // them (and back) without searching the hierarchy each time.
        private readonly Dictionary<int, MeshRenderer> shapeRendererByPos = new Dictionary<int, MeshRenderer>();
        private readonly Dictionary<int, Material> originalMaterialByPos = new Dictionary<int, Material>();
        private readonly HashSet<int> fadedTiles = new HashSet<int>();

        // Tile -> how far the tile's dominant top surface sits above the tile origin,
        // in world units. Measured off the tile meshes (see MeasureSurfaceHeight), so
        // on-tile UI (cursor, range indicators) can sit ON a tall tile instead of
        // being buried in it. A bridge deck over water is the motivating case: the
        // model is grounded on its lowest voxel (the water), which leaves the deck
        // a few voxels above the plain ground tiles around it.
        private readonly Dictionary<int, float> surfaceHeightByPos = new Dictionary<int, float>();

        // Every instance of one shape shares a mesh, so measure it once per shape id.
        private readonly Dictionary<int, float> surfaceHeightByShapeId = new Dictionary<int, float>();

        // The surface height most tiles on the board share: the level the cursor and
        // indicator prefabs were authored against. Tiles are lifted relative to it.
        private float groundSurfaceHeight = 0f;

        // One warning per field build when meshes can't be read.
        private bool warnedUnreadableMesh = false;

        // Surface heights are bucketed at this resolution when picking the dominant
        // one; a voxel is 0.1 world units, so this cleanly separates voxel layers.
        private const float SurfaceHeightBucket = 0.01f;

        public FDField Field { get; private set; }

        // Start is called before the first frame update
        void Start()
        {
            defaultMaterial = Resources.Load<Material>("Materials/material-fd2-palette");
        }

        public void Initialize(FDField field)
        {
            if (this.gameObject != null && !initialized)
            {
                buildField(field);
                initialized = true;
            }
        }


        private void buildField(FDField field)
        {
            for (int i = 1; i <= field.Width; i++)
            {
                for (int j = 1; j <= field.Height; j++)
                {
                    FDPosition pos = FDPosition.At(i, j);
                    ShapeDefinition shapeDef = field.GetShapeAt(pos);

                    // The model comes from the cleaned map (no buildings painted into the
                    // ground), the ShapeDefinition handed to Shape from the painted one.
                    GameObject shapePrefab = LoadShape(field.ChapterId, field.GetRenderShapeIdAt(pos));
                    if (shapePrefab != null)
                    {
                        GameObject shapeObj = Instantiate(shapePrefab);
                        shapeObj.transform.SetParent(this.transform);
                        shapeObj.transform.SetLocalPositionAndRotation(MapCoordinate.ConvertPosToVec3(pos), Quaternion.Euler(90, 0, 0));
                        shapeObj.transform.localScale = new Vector3(1.0f, 1.0f, 1.0f);

                        Transform inner = shapeObj.transform.Find("default");
                        // Remastered shape OBJs are exported centered (center=true, scale=0.1),
                        // so the mesh origin is already at the tile centre -> no offset.
                        // (The old (24,-24,0) was to re-centre the corner-origin ShapePanel
                        // meshes; at scale 1.0 it shifted every tile by ~half the map.)
                        inner.SetLocalPositionAndRotation(new Vector3(0, 0, 0), Quaternion.Euler(180, 0, 0));

                        MeshRenderer renderer = inner.GetComponent<MeshRenderer>();
                        renderer.materials = new Material[1] { defaultMaterial };
                        shapeRendererByPos[TileKey(pos)] = renderer;

                        surfaceHeightByPos[TileKey(pos)] = MeasureSurfaceHeight(
                            field.GetRenderShapeIdAt(pos), inner, shapeObj.transform);

                        Shape shape = shapeObj.AddComponent<Shape>();
                        shape.Init(pos, shapeDef);
                    }
                }
            }

            groundSurfaceHeight = DominantHeight(surfaceHeightByPos.Values);
        }

        /// <summary>
        /// How far the given tile's dominant top surface sits above the board's usual
        /// ground level, in world units; 0 for an ordinary tile, an unknown tile, or a
        /// field that has not been built yet. Add it to the Y of anything that is
        /// drawn lying on a tile (cursor, range indicators) so a tall tile such as a
        /// bridge deck does not swallow it. Tiles that sit lower than the usual ground
        /// are not lowered: the UI already reads fine there.
        /// </summary>
        public float GetIndicatorLift(FDPosition pos)
        {
            if (pos == null || !surfaceHeightByPos.TryGetValue(TileKey(pos), out float surface))
            {
                return 0f;
            }

            return Mathf.Max(0f, surface - groundSurfaceHeight);
        }

        /// <summary>
        /// The height, above the tile origin, of the tile mesh's dominant upward-facing
        /// surface: the level that covers the most ground area when seen from above.
        /// Using the dominant level rather than the mesh top means a tree's crown or a
        /// few grass blades do not count as "the tile is tall", but a bridge deck that
        /// spans the whole tile does. Measured in the tile's own transform, so every
        /// instance of the shape gets the same answer.
        /// </summary>
        private float MeasureSurfaceHeight(int shapeId, Transform inner, Transform tileRoot)
        {
            if (surfaceHeightByShapeId.TryGetValue(shapeId, out float cached))
            {
                return cached;
            }

            float height = 0f;

            MeshFilter filter = inner != null ? inner.GetComponent<MeshFilter>() : null;
            Mesh mesh = filter != null ? filter.sharedMesh : null;
            if (mesh != null)
            {
                // A model imported without Read/Write enabled hands back empty arrays
                // in a player build (the editor can always read them). Assets/Editor/
                // ShapeModelImporter keeps the tile models readable; warn if one slipped
                // through so the tile is not silently treated as flat.
                Vector3[] vertices = mesh.vertices;
                int[] triangles = mesh.triangles;
                if (vertices.Length == 0 || triangles.Length == 0)
                {
                    if (!warnedUnreadableMesh)
                    {
                        warnedUnreadableMesh = true;
                        Debug.LogWarning(string.Format(
                            "Tile mesh '{0}' is not readable; enable Read/Write on the shape models so the cursor can follow tall tiles.",
                            mesh.name));
                    }
                }
                else
                {
                    height = DominantUpFacingHeight(vertices, triangles, inner, tileRoot);
                }
            }

            surfaceHeightByShapeId[shapeId] = height;
            return height;
        }

        /// <summary>
        /// Projects every upward-facing triangle onto the ground plane and sums the
        /// projected area per height; the height holding the most area wins. Heights
        /// are relative to the tile root's position.
        /// </summary>
        private static float DominantUpFacingHeight(Vector3[] vertices, int[] triangles, Transform inner, Transform tileRoot)
        {
            Dictionary<int, float> areaByBucket = new Dictionary<int, float>();
            float baseY = tileRoot.position.y;

            for (int i = 0; i + 2 < triangles.Length; i += 3)
            {
                Vector3 a = inner.TransformPoint(vertices[triangles[i]]);
                Vector3 b = inner.TransformPoint(vertices[triangles[i + 1]]);
                Vector3 c = inner.TransformPoint(vertices[triangles[i + 2]]);

                // Unity front faces wind clockwise, so this cross product points along
                // the face normal; its Y component is twice the triangle's area as seen
                // from above, and is positive only for faces that look up.
                float projectedArea = Vector3.Cross(b - a, c - a).y;
                if (projectedArea <= 0f)
                {
                    continue;
                }

                float y = (a.y + b.y + c.y) / 3f - baseY;
                int bucket = Mathf.RoundToInt(y / SurfaceHeightBucket);
                areaByBucket.TryGetValue(bucket, out float area);
                areaByBucket[bucket] = area + projectedArea;
            }

            int bestBucket = 0;
            float bestArea = -1f;
            foreach (KeyValuePair<int, float> entry in areaByBucket)
            {
                if (entry.Value > bestArea)
                {
                    bestArea = entry.Value;
                    bestBucket = entry.Key;
                }
            }

            return bestBucket * SurfaceHeightBucket;
        }

        /// <summary>
        /// The surface height shared by the most tiles: the board's ground level.
        /// </summary>
        private static float DominantHeight(IEnumerable<float> heights)
        {
            Dictionary<int, int> tilesByBucket = new Dictionary<int, int>();
            foreach (float h in heights)
            {
                int bucket = Mathf.RoundToInt(h / SurfaceHeightBucket);
                tilesByBucket.TryGetValue(bucket, out int count);
                tilesByBucket[bucket] = count + 1;
            }

            int bestBucket = 0;
            int bestCount = -1;
            foreach (KeyValuePair<int, int> entry in tilesByBucket)
            {
                if (entry.Value > bestCount)
                {
                    bestCount = entry.Value;
                    bestBucket = entry.Key;
                }
            }

            return bestBucket * SurfaceHeightBucket;
        }

        /// <summary>
        /// The tile model for one shape id, out of the chapter's own set. Chapters that
        /// have not been remastered yet borrow chapter 01's tiles, so their maps still
        /// render something instead of an empty board.
        /// </summary>
        private GameObject LoadShape(int chapterId, int shapeId)
        {
            if (shapeId < 0)
            {
                return null;
            }

            GameObject prefab = chapterId > 0
                ? Resources.Load<GameObject>(ShapeResourcePath(chapterId, shapeId))
                : null;
            if (prefab != null)
            {
                return prefab;
            }

            if (chapterId != FallbackShapeChapter && !warnedMissingShapeSet)
            {
                warnedMissingShapeSet = true;
                Debug.LogWarning(string.Format(
                    "Shape {0} is missing from Resources/Shapes/Shapes_{1:D2}, falling back to chapter {2:D2}'s tiles.",
                    shapeId, chapterId, FallbackShapeChapter));
            }

            return Resources.Load<GameObject>(ShapeResourcePath(FallbackShapeChapter, shapeId));
        }

        /// <summary>
        /// Resource path of one remastered tile model. Each chapter has its own set,
        /// laid out as Resources/Shapes/Shapes_NN/Shape_N_&lt;tileId&gt; -- the folder uses
        /// the two-digit chapter number, the file the plain one (Shapes_02/Shape_2_153).
        /// </summary>
        private static string ShapeResourcePath(int chapterId, int shapeId)
        {
            return string.Format("Shapes/Shapes_{0:D2}/Shape_{0}_{1}", chapterId, shapeId);
        }

        /// <summary>
        /// Fades the shape (grass/voxels) on a tile so an indicator placed there reads
        /// cleanly and stops interpenetrating/z-fighting with the tall ground geometry.
        /// Uses a per-tile material instance, so the shared shape material is untouched.
        /// Call ResetFadedTiles() (e.g. from clearAllIndicators) to restore them.
        /// </summary>
        public void SetTileFaded(FDPosition pos, float alpha)
        {
            int key = TileKey(pos);
            if (!shapeRendererByPos.TryGetValue(key, out MeshRenderer renderer) || renderer == null)
            {
                return;
            }

            if (!fadedTiles.Contains(key))
            {
                originalMaterialByPos[key] = renderer.sharedMaterial;
                Material faded = new Material(renderer.sharedMaterial);
                MakeTransparent(faded);
                renderer.material = faded;
                fadedTiles.Add(key);
            }

            Material mat = renderer.material;
            Color c = mat.color;
            c.a = alpha;
            mat.color = c;
        }

        /// <summary>
        /// Restores every tile faded by SetTileFaded back to the shared opaque material
        /// and destroys the temporary per-tile instances.
        /// </summary>
        public void ResetFadedTiles()
        {
            // Copy keys first since ResetTileFade mutates the collections.
            foreach (int key in new List<int>(fadedTiles))
            {
                RestoreTile(key);
            }

            fadedTiles.Clear();
            originalMaterialByPos.Clear();
        }

        /// <summary>
        /// Restores a single tile faded by SetTileFaded. Use this when only some tiles
        /// (e.g. a menu's tiles) should be un-faded, leaving others faded.
        /// </summary>
        public void ResetTileFade(FDPosition pos)
        {
            int key = TileKey(pos);
            if (!fadedTiles.Contains(key))
            {
                return;
            }

            RestoreTile(key);
            fadedTiles.Remove(key);
            originalMaterialByPos.Remove(key);
        }

        private void RestoreTile(int key)
        {
            if (!shapeRendererByPos.TryGetValue(key, out MeshRenderer renderer) || renderer == null)
            {
                return;
            }

            Material instance = renderer.material;
            if (originalMaterialByPos.TryGetValue(key, out Material original) && original != null)
            {
                renderer.sharedMaterial = original;
            }
            if (instance != null)
            {
                Destroy(instance);
            }
        }

        private static int TileKey(FDPosition pos)
        {
            return pos.X * 1000 + pos.Y;
        }

        /// <summary>
        /// Switches a Standard-shader material instance into Fade transparency mode so
        /// its colour alpha takes effect.
        /// </summary>
        private static void MakeTransparent(Material m)
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
    }
}