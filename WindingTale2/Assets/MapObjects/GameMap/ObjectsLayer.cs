using UnityEngine;
using WindingTale.Core.Common;
using WindingTale.Core.Definitions;
using WindingTale.Core.Map;
using WindingTale.Core.Objects;

namespace WindingTale.MapObjects.GameMap
{
    /// <summary>
    /// Instantiates the treasure chests declared on the map (FDMap.Treasures) under
    /// this layer, one child per chest, and keeps them in sync with the map state.
    ///
    /// Model choice follows TreasureType:
    ///   RedBox  (1) -> Others/Treasures/TreasureBox_01
    ///   BlueBox (2) -> Others/Treasures/TreasureBox_02
    ///   Hidden  (3) -> nothing is drawn
    /// and an opened chest swaps to the "_Opened" variant of the same model.
    ///
    /// Placement mirrors ObstaclesLayer (parent Euler(90) + inner Euler(180) stands
    /// the Z-up model up, clip shader, bottom seated on the ground plane). The one
    /// difference: a chest is exactly one tile, and its model is exported centred on
    /// its own origin, so it needs no horizontal anchoring -- an obstacle's
    /// Position is the top-left tile of a multi-tile footprint, a chest's is simply
    /// the tile it sits on.
    /// </summary>
    public class ObjectsLayer : MonoBehaviour
    {
        // How opaque a chest stays while something needs to be read through it:
        // a creature standing on its tile, the cursor, or a menu item.
        private const float TreasureAlpha = 0.2f;

        // World height of the board surface a chest stands on. ObjectsLayer's own
        // origin is y = 0, but the ground tiles are drawn by ShapesLayer, which sits
        // at y = 1.6 in the scene -- seating a chest at 0 buries it under the board.
        private const float GroundY = 1.6f;

        // The visible board surface stands a couple of voxels above GroundY, so
        // seating a chest exactly on GroundY sinks its bottom ~2 voxels into the
        // board and hides the chest floor. Lift every chest by that much so its
        // base rests on the visible surface. 0.2 = 2 voxels at the 0.1 export scale.
        private const float TreasureLift = 0.2f;

        private const string TreasureModelRoot = "Others/Treasures/";

        // Name given to the child holding the actual chest mesh, so the model can be
        // swapped (closed <-> opened) without disturbing the TreasureInstance above it.
        private const string ModelChildName = "model";

        private FDMap map = null;
        private GameMap gameMap = null;
        private bool initialized = false;

        public void Initialize(FDMap map, GameMap gameMap)
        {
            if (this.gameObject == null || initialized)
            {
                return;
            }

            this.map = map;
            this.gameMap = gameMap;
            buildTreasures();
            initialized = true;
        }

        private void buildTreasures()
        {
            if (map == null || map.Treasures == null)
            {
                Debug.LogWarning("ObjectsLayer: map has no Treasures list; nothing to draw.");
                return;
            }

            int built = 0;
            foreach (FDTreasure treasure in map.Treasures)
            {
                if (treasure == null || treasure.Position == null)
                {
                    continue;
                }

                // Hidden chests are found by searching the tile, never drawn.
                if (treasure.Type == TreasureType.Hidden)
                {
                    continue;
                }

                GameObject holder = new GameObject(string.Format("treasure_{0}", treasure.Id));
                holder.transform.SetParent(this.transform);

                TreasureInstance instance = holder.AddComponent<TreasureInstance>();
                instance.SetTreasure(treasure);

                mountModel(instance, treasure.HasOpened);
                built++;
            }

            Debug.Log(string.Format(
                "ObjectsLayer: built {0} treasure object(s) from {1} map treasure(s).",
                built, map.Treasures.Count));
        }

        /// <summary>
        /// Instantiates the chest model matching (type, opened) under the instance,
        /// replacing whatever model was there. Returns false when the model is missing.
        /// </summary>
        private bool mountModel(TreasureInstance instance, bool opened)
        {
            string resourcePath = GetModelPath(instance.Type, opened);
            GameObject prefab = Resources.Load<GameObject>(resourcePath);
            if (prefab == null)
            {
                Debug.LogWarning("Treasure model not found: " + resourcePath);
                return false;
            }

            Transform existing = instance.transform.Find(ModelChildName);
            if (existing != null)
            {
                Destroy(existing.gameObject);
            }

            GameObject model = Instantiate(prefab);
            model.name = ModelChildName;
            model.transform.SetParent(instance.transform);

            // The chest sits centred on its tile: the model is exported centre-pivoted
            // and is exactly one tile wide, so the tile centre is the final position.
            FDPosition pos = FDPosition.At(instance.TileX, instance.TileY);
            model.transform.SetLocalPositionAndRotation(
                MapCoordinate.ConvertPosToVec3(pos), Quaternion.Euler(90, 0, 0));
            model.transform.localScale = Vector3.one;

            Transform inner = model.transform.Find("default");
            if (inner != null)
            {
                inner.SetLocalPositionAndRotation(Vector3.zero, Quaternion.Euler(180, 0, 0));
            }

            ApplyClipShader(model);

            // Seat the bottom of the chest on the board surface. Driving this off the
            // model's own bounds rather than a fixed offset keeps the closed and opened
            // models level with each other: the opened one is taller (the raised lid),
            // but both have their base at bounds.min.y, so both land on the ground.
            if (TryGetWorldBounds(model, out Bounds bounds))
            {
                model.transform.position += new Vector3(0f, GroundY - bounds.min.y + TreasureLift, 0f);
            }

            instance.SetShowingOpened(opened);
            return true;
        }

        private static string GetModelPath(TreasureType type, bool opened)
        {
            string box = type == TreasureType.BlueBox ? "TreasureBox_02" : "TreasureBox_01";
            return TreasureModelRoot + box + (opened ? "_Opened" : string.Empty);
        }

        /// <summary>
        /// Keeps the chests in step with the map each frame: swaps a chest to its
        /// opened model once it has been emptied, and recomputes which chests are
        /// faded.
        ///
        /// This is polled rather than pushed because the three things that fade a
        /// chest change from unrelated places -- the cursor moves, a menu opens, a
        /// creature finishes a walk -- and a chest is opened deep inside
        /// MenuActionState. Polling a handful of chests keeps one owner of the
        /// state instead of scattering refresh calls across all of those paths; the
        /// per-frame cost is a few tile comparisons and the material work only runs
        /// when a value actually changes.
        /// </summary>
        void Update()
        {
            if (!initialized || map == null)
            {
                return;
            }

            refreshOpenedModels();
            refreshFade();
        }

        private void refreshOpenedModels()
        {
            foreach (TreasureInstance instance in GetComponentsInChildren<TreasureInstance>())
            {
                FDTreasure treasure = findTreasure(instance.TreasureId);
                if (treasure != null && treasure.HasOpened != instance.ShowingOpened)
                {
                    mountModel(instance, treasure.HasOpened);
                }
            }
        }

        private void refreshFade()
        {
            FDPosition[] uiTiles = gameMap != null ? gameMap.GetFadeTiles() : null;

            foreach (TreasureInstance instance in GetComponentsInChildren<TreasureInstance>())
            {
                if (ShouldFade(instance, uiTiles))
                {
                    instance.SetTransparency(TreasureAlpha);
                }
                else
                {
                    instance.ResetTransparency();
                }
            }
        }

        /// <summary>
        /// A chest fades when a creature stands on its tile (open or not), or when the
        /// cursor / an open menu item covers that tile.
        /// </summary>
        private bool ShouldFade(TreasureInstance instance, FDPosition[] uiTiles)
        {
            if (map.Creatures != null)
            {
                foreach (FDCreature creature in map.Creatures)
                {
                    if (creature != null && instance.Covers(creature.Position))
                    {
                        return true;
                    }
                }
            }

            if (uiTiles != null)
            {
                foreach (FDPosition tile in uiTiles)
                {
                    if (instance.Covers(tile))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Returns the chest on the given tile, or null. Hidden chests have no object
        /// here, so this only ever finds a drawn one.
        /// </summary>
        public TreasureInstance GetTreasureAt(FDPosition position)
        {
            if (position == null)
            {
                return null;
            }

            foreach (TreasureInstance instance in GetComponentsInChildren<TreasureInstance>())
            {
                if (instance.Covers(position))
                {
                    return instance;
                }
            }

            return null;
        }

        private FDTreasure findTreasure(int treasureId)
        {
            if (map.Treasures == null)
            {
                return null;
            }

            return map.Treasures.Find(t => t.Id == treasureId);
        }

        private static void ApplyClipShader(GameObject obj)
        {
            Shader clipShader = Shader.Find("Custom/MapClip");
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
