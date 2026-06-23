using UnityEngine;
using WindingTale.Core.Common;
using WindingTale.Core.Definitions;
using WindingTale.Core.Map;

namespace WindingTale.MapObjects.GameMap
{
    /// <summary>
    /// Instantiates the obstacle models declared in the chapter (FDField.Obstacles)
    /// under this layer. For each obstacle the model is loaded from
    ///   Resources/Obstacles/Obstacles_01/{DefinitionKey}
    /// and placed at its tile Position. Mirrors ShapesLayer's upright transform
    /// (parent Euler(90) + inner Euler(180) stands the Z-up model up), but keeps
    /// each obstacle's own imported palette material instead of the shared one.
    /// </summary>
    public class ObstaclesLayer : MonoBehaviour
    {
        private bool initialized = false;

        public void Initialize(FDField field)
        {
            if (this.gameObject != null && !initialized)
            {
                buildObstacles(field);
                initialized = true;
            }
        }

        private void buildObstacles(FDField field)
        {
            if (field == null || field.Obstacles == null)
            {
                return;
            }

            // Publish the map rectangle so the clip shader can truncate any obstacle
            // geometry that overhangs the board edge. Must run before the obstacles
            // are instantiated below so their first rendered frame already has it.
            SetMapClipBounds(field);
            Shader clipShader = Shader.Find("Custom/MapClip");

            foreach (ObstacleDefinition obstacle in field.Obstacles)
            {
                if (obstacle == null || string.IsNullOrEmpty(obstacle.DefinitionKey) || obstacle.Position == null)
                {
                    continue;
                }

                // Prefer a hand-tuned prefab (which may carry an ObstacleAnchor) and
                // fall back to the raw imported model. This lets obstacles migrate to
                // editor-authored prefabs one at a time without changing the pipeline.
                GameObject prefab =
                    Resources.Load<GameObject>(string.Format("Obstacles/Obstacles_01_Prefabs/{0}", obstacle.DefinitionKey))
                    ?? Resources.Load<GameObject>(string.Format("Obstacles/Obstacles_01/{0}", obstacle.DefinitionKey));
                if (prefab == null)
                {
                    Debug.LogWarning("Obstacle model not found: Obstacles/Obstacles_01/" + obstacle.DefinitionKey);
                    continue;
                }

                FDPosition pos = FDPosition.At(obstacle.Position.X, obstacle.Position.Y);

                GameObject obj = Instantiate(prefab);
                obj.name = string.Format("obstacle_{0}_{1}", obstacle.Id, obstacle.DefinitionKey);
                obj.transform.SetParent(this.transform);
                obj.transform.SetLocalPositionAndRotation(MapCoordinate.ConvertPosToVec3(pos), Quaternion.Euler(90, 0, 0));

                // House/hut buildings are too tall; halve their height. The model is
                // Z-up in its own local space (the upright rotation comes from the
                // parent Euler(90)), so the vertical axis is local Z. A Z-only scale
                // leaves the footprint (and the anchor math below) untouched.
                float heightScale = GetHeightScale(obstacle.DefinitionKey);
                obj.transform.localScale = new Vector3(1.0f, 1.0f, heightScale);

                Transform inner = obj.transform.Find("default");
                if (inner != null)
                {
                    inner.SetLocalPositionAndRotation(new Vector3(0, 0, 0), Quaternion.Euler(180, 0, 0));
                }

                // Swap to the clip shader so parts of this obstacle that stick out past
                // the map edge are truncated. Keeps each obstacle's own texture/colour
                // (the clip shader exposes the same _MainTex/_Color as the Standard one).
                ApplyClipShader(obj, clipShader);

                // The model is exported centre-pivoted, but Position is the
                // top-left tile of the footprint. Anchor it via its world bounds:
                //  - horizontally, shift so the top-left corner (max world X, min
                //    world Z = smallest tile X/Y) sits on the tile, so it extends
                //    INTO the map instead of toward the top-left;
                //  - vertically, seat the BOTTOM edge (bounds.min.y) on the ground
                //    plane (y = 0). This puts the effective anchor at the model's
                //    base, so changing the height scale above grows/shrinks the model
                //    upward from a fixed base and needs no further adjustment.
                ObstacleAnchor anchor = obj.GetComponent<ObstacleAnchor>();

                if (TryGetWorldBounds(obj, out Bounds bounds))
                {
                    // Horizontal anchor (shared by every obstacle): shift so the top-left
                    // tile corner sits on the tile and the model extends into the map.
                    float horizX = -bounds.extents.x;
                    float horizZ = bounds.extents.z;

                    // Vertical seating: prefer the prefab's authored anchor point (drop it
                    // onto the ground plane y = 0); otherwise drop the bounding-box bottom.
                    float seatY = (anchor != null && anchor.groundAnchor != null)
                        ? -anchor.groundAnchor.position.y
                        : -bounds.min.y;

                    // Per-prefab fine offset. When the prefab has no ObstacleAnchor we fall
                    // back to the per-key code table so existing models keep their tweaks.
                    Vector3 extra = (anchor != null)
                        ? anchor.anchorOffset
                        : new Vector3(0f, GetGroundYOffset(obstacle.DefinitionKey), 0f);

                    obj.transform.position += new Vector3(horizX, seatY, horizZ) + extra;
                }
            }
        }

        /// <summary>
        /// Publishes the map's world-space rectangle to the "Custom/MapClip" shader as
        /// the global "_MapClipMinMaxXZ" (xy = min, zw = max). Tiles are 2-unit cells
        /// centred at MapCoordinate.ConvertPosToVec3 (-x*2, 0, y*2) for x in [1,Width]
        /// and y in [1,Height], so the board spans these world bounds (+/-1 = half tile).
        /// </summary>
        private static void SetMapClipBounds(FDField field)
        {
            float minX = -2f * field.Width - 1f;
            float maxX = -1f;
            float minZ = 1f;
            float maxZ = 2f * field.Height + 1f;
            Shader.SetGlobalVector("_MapClipMinMaxXZ", new Vector4(minX, minZ, maxX, maxZ));
        }

        /// <summary>
        /// Re-points every renderer material on the obstacle at the clip shader, keeping
        /// the existing _MainTex/_Color (shared property names with the Standard shader).
        /// Accessing renderer.materials instantiates per-object copies, so this does not
        /// mutate the shared source material.
        /// </summary>
        private static void ApplyClipShader(GameObject obj, Shader clipShader)
        {
            if (clipShader == null)
            {
                return;
            }

            foreach (Renderer renderer in obj.GetComponentsInChildren<Renderer>())
            {
                Material[] mats = renderer.materials;
                for (int i = 0; i < mats.Length; i++)
                {
                    if (mats[i] != null)
                    {
                        mats[i].shader = clipShader;
                    }
                }
                renderer.materials = mats;
            }
        }

        private static float GetHeightScale(string definitionKey)
        {
            switch (definitionKey)
            {
                case "dwelling_house_1":
                case "thatched_hut_1":
                    return 0.5f;
                default:
                    return 1.0f;
            }
        }

        /// <summary>
        /// Per-model vertical adjustment (in world units) applied after the common
        /// base-seating. Negative lowers the model into the ground, positive raises it.
        /// One tile is 2 world units (see MapCoordinate.ConvertPosToVec3), so e.g.
        /// -0.5f sinks the model a quarter-tile. Tune the value to taste.
        /// </summary>
        private static float GetGroundYOffset(string definitionKey)
        {
            switch (definitionKey)
            {
                case "barrel_group_1":
                    return -0.5f;
                default:
                    return 0f;
            }
        }

        private static bool TryGetWorldBounds(GameObject obj, out Bounds bounds)
        {
            Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
            {
                bounds = new Bounds();
                return false;
            }

            bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }
            return true;
        }
    }
}
