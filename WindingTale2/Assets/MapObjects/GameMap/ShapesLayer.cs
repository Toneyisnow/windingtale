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
        private bool initialized = false;

        private Material defaultMaterial = null;

        // Tile -> the shape's renderer, so indicators can fade the shape underneath
        // them (and back) without searching the hierarchy each time.
        private readonly Dictionary<int, MeshRenderer> shapeRendererByPos = new Dictionary<int, MeshRenderer>();
        private readonly Dictionary<int, Material> originalMaterialByPos = new Dictionary<int, Material>();
        private readonly HashSet<int> fadedTiles = new HashSet<int>();

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

                    GameObject shapePrefab = Resources.Load<GameObject>(string.Format("Shapes/Shapes_01/Shape_1_{0}", shapeDef.Id));
                    if (shapePrefab != null)
                    {
                        GameObject shapeObj = Instantiate(shapePrefab);
                        shapeObj.transform.SetParent(this.transform);
                        shapeObj.transform.SetLocalPositionAndRotation(MapCoordinate.ConvertPosToVec3(pos), Quaternion.Euler(90, 0, 0));
                        shapeObj.transform.localScale = new Vector3(1.0f, 1.0f, 1.0f);

                        Transform inner = shapeObj.transform.Find("default");
                        // New Shapes_01 OBJs are exported centered (center=true, scale=0.1),
                        // so the mesh origin is already at the tile centre -> no offset.
                        // (The old (24,-24,0) was to re-centre the corner-origin ShapePanel01
                        // meshes; at scale 1.0 it shifted every tile by ~half the map.)
                        inner.SetLocalPositionAndRotation(new Vector3(0, 0, 0), Quaternion.Euler(180, 0, 0));

                        MeshRenderer renderer = inner.GetComponent<MeshRenderer>();
                        renderer.materials = new Material[1] { defaultMaterial };
                        shapeRendererByPos[TileKey(pos)] = renderer;

                        Shape shape = shapeObj.AddComponent<Shape>();
                        shape.Init(pos, shapeDef);
                    }
                }
            }
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