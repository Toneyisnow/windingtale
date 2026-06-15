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

            foreach (ObstacleDefinition obstacle in field.Obstacles)
            {
                if (obstacle == null || string.IsNullOrEmpty(obstacle.DefinitionKey) || obstacle.Position == null)
                {
                    continue;
                }

                GameObject prefab = Resources.Load<GameObject>(
                    string.Format("Obstacles/Obstacles_01/{0}", obstacle.DefinitionKey));
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
                obj.transform.localScale = new Vector3(1.0f, 1.0f, 1.0f);

                Transform inner = obj.transform.Find("default");
                if (inner != null)
                {
                    inner.SetLocalPositionAndRotation(new Vector3(0, 0, 0), Quaternion.Euler(180, 0, 0));
                }

                // The model is exported centre-pivoted, but Position is the
                // top-left tile of the footprint. Shift the model so its top-left
                // corner (max world X, min world Z = smallest tile X/Y) sits on the
                // tile, so it extends INTO the map instead of toward the top-left.
                if (TryGetWorldBounds(obj, out Bounds bounds))
                {
                    obj.transform.position += new Vector3(-bounds.extents.x, 0f, bounds.extents.z);
                }
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
