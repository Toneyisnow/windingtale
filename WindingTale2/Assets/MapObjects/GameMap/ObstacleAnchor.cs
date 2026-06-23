using UnityEngine;

namespace WindingTale.MapObjects.GameMap
{
    /// <summary>
    /// Optional per-prefab placement hint read by <see cref="ObstaclesLayer"/> at
    /// map-build time. It lets a single obstacle prefab be tuned in the Unity editor
    /// without touching the shared generation code, so every obstacle still flows
    /// through the same pipeline (load -> place on tile -> rotate -> scale -> seat).
    ///
    /// Attach this to the root of an obstacle prefab. If the prefab has no
    /// ObstacleAnchor, ObstaclesLayer falls back to its automatic bounding-box
    /// seating (and the per-key code table), so existing models keep working.
    /// </summary>
    public class ObstacleAnchor : MonoBehaviour
    {
        [Tooltip("Empty child transform placed at the point of the model that should " +
                 "rest on the tile (its ground-contact point). If set, the obstacle is " +
                 "seated so this point sits on the ground plane (y = 0), instead of " +
                 "using the automatic bounding-box bottom. Position it visually inside " +
                 "the prefab to define the anchor point.")]
        public Transform groundAnchor;

        [Tooltip("Extra offset added after seating, in world units (one tile = 2 units). " +
                 "Y < 0 sinks the model into the ground; X / Z nudge it across the map " +
                 "(e.g. to pull an obstacle off the map edge). Use for fine tuning.")]
        public Vector3 anchorOffset = Vector3.zero;
    }
}
