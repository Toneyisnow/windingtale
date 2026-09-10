using System.Collections.Generic;
using UnityEngine;
using WindingTale.Core.Common;
using WindingTale.Core.Definitions;
using WindingTale.Core.Map;
using WindingTale.Core.Objects;

namespace WindingTale.MapObjects.GameMap
{
    /// <summary>
    /// Instantiates the obstacle models declared in the chapter (FDField.Obstacles)
    /// under this layer. For each obstacle the model is loaded from
    ///   Resources/Obstacles/{DefinitionKey}
    /// Obstacle models are shared across every chapter, so they live in one flat
    /// folder keyed by DefinitionKey rather than per-chapter subfolders.
    /// and placed at its tile Position. Mirrors ShapesLayer's upright transform
    /// (parent Euler(90) + inner Euler(180) stands the Z-up model up), but keeps
    /// each obstacle's own imported palette material instead of the shared one.
    ///
    /// An obstacle may be animated: where Resources/Obstacles/{DefinitionKey}_f2,
    /// _f3, ... exist beside the model they are its later frames, instantiated
    /// under the same root and played by ObstacleAnimation at its global rate.
    /// Frame models are authored to the same footprint as the first frame, so
    /// the anchoring and footprint below read the same bounds whichever frame is
    /// showing.
    ///
    /// An obstacle may also glow (GetGlow): its materials are given emission and a
    /// point light is hung at it, see ObstacleGlow. The glow of every obstacle is
    /// switched together by ObstacleGlow.Enabled -- the Inspector checkbox below
    /// and, in the editor or a development build, the F9 key, so the two looks
    /// can be compared in Play mode.
    ///
    /// An obstacle may also be a ground cover -- chapter 25's lava sheets, keyed
    /// "lava_..." (IsGroundCover): a flat one-voxel sheet lying on its tile, kept at
    /// full tile size, seated on the tile's top surface, glowing without a light and
    /// never faded. Animated like any other obstacle, so the lava shimmers at the
    /// fire pillars' rate.
    ///
    /// An obstacle fades to almost nothing while something has to be read through
    /// it: a creature standing on one of its tiles, or the cursor, a menu item or a
    /// move/target indicator covering one. Like the chests (ObjectsLayer) this is
    /// polled in Update rather than pushed, because those things change from
    /// unrelated places; SetTransparency is a no-op when nothing changed.
    /// </summary>
    public class ObstaclesLayer : MonoBehaviour
    {
        // How opaque an obstacle stays while a creature or a UI element sits on one
        // of its tiles. Near-invisible: a unit under a tree must read at a glance.
        private const float FadedAlpha = 0.1f;

        private bool initialized = false;

        private FDMap map = null;
        private GameMap gameMap = null;

        // Every obstacle built, so the per-frame fade does not walk the hierarchy.
        private readonly List<ObstacleInstance> instances = new List<ObstacleInstance>();

        // The glow switch as seen in the Inspector. Tick or untick it in Play mode
        // and Update pushes the change to ObstacleGlow.Enabled; the hotkey flips it
        // the other way round so the two stay in step.
        [SerializeField]
        [Tooltip("Whether glowing obstacles (the fire pillars of chapters 10 and 25, chapter 22's orbs) shine and light their surroundings.")]
        private bool glowEnabled = true;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private const KeyCode GlowToggleKey = KeyCode.F9;
#endif

        void Update()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (Input.GetKeyDown(GlowToggleKey))
            {
                glowEnabled = !glowEnabled;
            }
#endif
            if (glowEnabled != ObstacleGlow.Enabled)
            {
                ObstacleGlow.Enabled = glowEnabled;
            }

            if (initialized)
            {
                refreshFade();
            }
        }

        public void Initialize(FDMap map, GameMap gameMap)
        {
            if (this.gameObject != null && !initialized)
            {
                this.map = map;
                this.gameMap = gameMap;
                buildObstacles(map != null ? map.Field : null);
                initialized = true;
            }
        }

        /// <summary>
        /// Fades every obstacle that a creature or a UI element (cursor, menu item,
        /// range indicator -- see GameMap.GetFadeTiles) currently sits on, and
        /// restores the rest.
        /// </summary>
        private void refreshFade()
        {
            FDPosition[] uiTiles = gameMap != null ? gameMap.GetFadeTiles() : null;
            List<FDCreature> creatures = map != null ? map.Creatures : null;

            foreach (ObstacleInstance instance in instances)
            {
                if (instance == null || instance.IsGroundCover)
                {
                    continue;
                }

                if (ShouldFade(instance, creatures, uiTiles))
                {
                    instance.SetTransparency(FadedAlpha);
                }
                else
                {
                    instance.ResetTransparency();
                }
            }
        }

        private static bool ShouldFade(ObstacleInstance instance, List<FDCreature> creatures, FDPosition[] uiTiles)
        {
            if (creatures != null)
            {
                foreach (FDCreature creature in creatures)
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

        private void buildObstacles(FDField field)
        {
            instances.Clear();
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
                GameObject prefab = LoadObstaclePrefab(obstacle.DefinitionKey);
                if (prefab == null)
                {
                    Debug.LogWarning("Obstacle model not found: Obstacles/" + obstacle.DefinitionKey);
                    continue;
                }

                FDPosition pos = FDPosition.At(obstacle.Position.X, obstacle.Position.Y);

                GameObject obj = Instantiate(prefab);
                obj.name = string.Format("obstacle_{0}_{1}", obstacle.Id, obstacle.DefinitionKey);
                obj.transform.SetParent(this.transform);
                obj.transform.SetLocalPositionAndRotation(MapCoordinate.ConvertPosToVec3(pos), Quaternion.Euler(90, 0, 0));

                // Every obstacle reads slightly oversized against the tiles, so all of
                // them are shrunk uniformly by ObstacleScale. On top of that, house/hut
                // buildings are too tall and get their height halved as well. The model
                // is Z-up in its own local space (the upright rotation comes from the
                // parent Euler(90)), so the vertical axis is local Z. The anchor math
                // below reads the scaled world bounds, so it needs no adjustment.
                float heightScale = GetHeightScale(obstacle.DefinitionKey);
                float scale = GetObstacleScale(obstacle.DefinitionKey);
                obj.transform.localScale = new Vector3(scale, scale, scale * heightScale);

                Transform inner = obj.transform.Find("default");
                if (inner != null)
                {
                    inner.SetLocalPositionAndRotation(new Vector3(0, 0, 0), Quaternion.Euler(180, 0, 0));
                }

                // The later frames of an animated obstacle, if it has any, go under the
                // same root so everything below -- scale, shader, anchoring, footprint --
                // treats them as part of this one object.
                ObstacleAnimation animation = AttachAnimationFrames(obj, obstacle);

                // Swap to the clip shader so parts of this obstacle that stick out past
                // the map edge are truncated. Keeps each obstacle's own texture/colour
                // (the clip shader exposes the same _MainTex/_Color as the Standard one).
                ApplyClipShader(obj, clipShader);

                // Which tiles this obstacle covers, from the model's own world bounds:
                // one tile is 2 world units (MapCoordinate.ConvertPosToVec3), map X runs
                // along world -X and map Y along world +Z, so the tile extents are just
                // the bounding-box size over the tile size. The bounds are already
                // shrunk by ObstacleScale, so divide it back out -- the obstacle still
                // occupies the tiles the chapter authored it on, it just renders a
                // little smaller inside them. Needed both to place the model (below)
                // and so the fade can tell what stands on it.
                ObstacleAnchor anchor = obj.GetComponent<ObstacleAnchor>();
                int tileWidth = 1;
                int tileHeight = 1;

                if (TryGetWorldBounds(obj, out Bounds bounds))
                {
                    float tileSize = WorldUnitsPerTile * scale;
                    tileWidth = Mathf.Max(1, Mathf.RoundToInt(bounds.size.x / tileSize));
                    tileHeight = Mathf.Max(1, Mathf.RoundToInt(bounds.size.z / tileSize));

                    // The model is exported centre-pivoted, but Position is the
                    // top-left tile of the footprint. Anchor it via its world bounds:
                    //  - horizontally, put the model's centre on the centre of its
                    //    footprint: the top-left tile's centre, moved half the footprint
                    //    (less one tile) into the map. A 1 x 1 tree lands dead centre on
                    //    its tile; a 6-wide house is centred over its six tiles;
                    //  - vertically, seat the BOTTOM edge (bounds.min.y) on the ground
                    //    plane (y = 0). This puts the effective anchor at the model's
                    //    base, so changing the height scale above grows/shrinks the model
                    //    upward from a fixed base and needs no further adjustment.
                    Vector3 tileCentre = this.transform.TransformPoint(MapCoordinate.ConvertPosToVec3(pos));
                    float footprintX = tileCentre.x - (tileWidth - 1) * WorldUnitsPerTile / 2f;
                    float footprintZ = tileCentre.z + (tileHeight - 1) * WorldUnitsPerTile / 2f;
                    float horizX = footprintX - bounds.center.x;
                    float horizZ = footprintZ - bounds.center.z;

                    // Vertical seating: prefer the prefab's authored anchor point (drop it
                    // onto the ground plane y = 0); otherwise drop the bounding-box bottom.
                    float seatY = (anchor != null && anchor.groundAnchor != null)
                        ? -anchor.groundAnchor.position.y
                        : -bounds.min.y;

                    // Per-prefab fine offset. When the prefab has no ObstacleAnchor we fall
                    // back to the per-key code table so existing models keep their tweaks.
                    // A ground cover is lifted onto the tile's top surface instead: seated
                    // at y = 0 like everything else it would sit inside the tile's own
                    // voxel layer and never show.
                    Vector3 extra = (anchor != null)
                        ? anchor.anchorOffset
                        : new Vector3(0f, GroundYOffset(obstacle.DefinitionKey), 0f);

                    obj.transform.position += new Vector3(horizX, seatY, horizZ) + extra;
                }

                ObstacleInstance instance = obj.GetComponent<ObstacleInstance>() ?? obj.AddComponent<ObstacleInstance>();
                instance.SetFootprint(pos.X, pos.Y, tileWidth, tileHeight);
                instance.IsGroundCover = IsGroundCover(obstacle.DefinitionKey);
                instances.Add(instance);

                // Only now, with the bounds read off every frame, settle on the first one.
                if (animation != null)
                {
                    animation.Show(0);
                }

                // Glow, for the obstacles that have one: needs the final bounds to place
                // the light and the clip-shader material instances to set emission on.
                ObstacleGlow.Spec glow = GetGlow(obstacle.DefinitionKey);
                if (glow != null && TryGetWorldBounds(obj, out Bounds glowBounds))
                {
                    obj.AddComponent<ObstacleGlow>().Init(glow, glowBounds);
                }
            }
        }

        /// <summary>
        /// How an obstacle glows, or null for the ordinary ones. The fire pillars of
        /// chapters 10 and 25: the whole model is fire, so it is self-lit almost fully,
        /// and each carries a warm point light at its flame. The light pillars of
        /// chapters 27-30 are the same idea in blue-white. One tile is 2 world units,
        /// so a range of 7 reaches about three tiles out; the bright pillars throw the
        /// most light, the bowl the least.
        /// </summary>
        private static ObstacleGlow.Spec GetGlow(string definitionKey)
        {
            if (definitionKey.StartsWith(LavaKeyPrefix))
            {
                // The lava sheets: the whole sheet is molten, so it is self-lit almost
                // fully -- but there are a couple of hundred of them on the board, so no
                // light each; the fire pillars light the cave.
                return new ObstacleGlow.Spec
                {
                    Emission = 0.9f,
                    LightColor = Color.white,
                    LightRange = 0f,
                    LightIntensity = 0f,
                    LightHeight = 0f,
                };
            }

            switch (definitionKey)
            {
                case "fire_pillar_1":       // the low bowl
                    return new ObstacleGlow.Spec
                    {
                        Emission = 0.85f,
                        LightColor = new Color(1.0f, 0.72f, 0.35f),
                        LightRange = 6f,
                        LightIntensity = 1.2f,
                        LightHeight = 0.7f,
                    };
                case "fire_pillar_2":       // the bright, white-hot pillar
                    return new ObstacleGlow.Spec
                    {
                        Emission = 0.9f,
                        LightColor = new Color(1.0f, 0.85f, 0.55f),
                        LightRange = 8f,
                        LightIntensity = 1.8f,
                        LightHeight = 0.8f,
                    };
                case "fire_pillar_3":       // the dim, orange pillar
                    return new ObstacleGlow.Spec
                    {
                        Emission = 0.8f,
                        LightColor = new Color(1.0f, 0.6f, 0.2f),
                        LightRange = 7f,
                        LightIntensity = 1.3f,
                        LightHeight = 0.8f,
                    };
                case "fire_pillar_4":       // chapter 25's taller bright pillar: the same fire, one tile higher
                    return new ObstacleGlow.Spec
                    {
                        Emission = 0.9f,
                        LightColor = new Color(1.0f, 0.85f, 0.55f),
                        LightRange = 9f,
                        LightIntensity = 1.8f,
                        LightHeight = 0.85f,
                    };
                case "light_pillar_1":      // chapters 27-30's pillars of blue light: the whole column is light
                    return new ObstacleGlow.Spec
                    {
                        Emission = 0.9f,
                        LightColor = new Color(0.7f, 0.85f, 1.0f),
                        LightRange = 7f,
                        LightIntensity = 1.4f,
                        LightHeight = 0.7f,
                    };
                case "light_pillar_2":      // the short one: the same light, a little less of it
                    return new ObstacleGlow.Spec
                    {
                        Emission = 0.9f,
                        LightColor = new Color(0.7f, 0.85f, 1.0f),
                        LightRange = 5.5f,
                        LightIntensity = 1.1f,
                        LightHeight = 0.7f,
                    };
                case "orb_pillar_yellow":   // chapter 22's crystal orbs: a soft light in the orb's colour
                    return OrbGlow(new Color(1.0f, 0.85f, 0.3f));
                case "orb_pillar_orange":
                    return OrbGlow(new Color(1.0f, 0.6f, 0.35f));
                case "orb_pillar_green":
                    return OrbGlow(new Color(0.6f, 0.9f, 0.3f));
                case "orb_pillar_purple":
                    return OrbGlow(new Color(0.85f, 0.55f, 0.95f));
                case "orb_pillar_red":
                    return OrbGlow(new Color(1.0f, 0.3f, 0.25f));
                case "orb_pillar_blue":
                    return OrbGlow(new Color(0.4f, 0.55f, 1.0f));
                default:
                    return null;
            }
        }

        /// <summary>
        /// The orb pillars are a stone pedestal with a glass ball on top, so only a
        /// little of the model is self-lit and the light hangs at the ball, about
        /// three quarters of the way up; two tiles of reach, well under the fire.
        /// </summary>
        private static ObstacleGlow.Spec OrbGlow(Color color)
        {
            return new ObstacleGlow.Spec
            {
                Emission = 0.35f,
                LightColor = color,
                LightRange = 4.5f,
                LightIntensity = 0.9f,
                LightHeight = 0.75f,
            };
        }

        /// <summary>
        /// The model for one obstacle name: a hand-tuned prefab (which may carry an
        /// ObstacleAnchor) when there is one, otherwise the raw imported model. Lets
        /// obstacles migrate to editor-authored prefabs one at a time without changing
        /// the pipeline. Null when neither exists.
        /// </summary>
        private static GameObject LoadObstaclePrefab(string name)
        {
            return Resources.Load<GameObject>(string.Format("Obstacles/Prefabs/{0}", name))
                ?? Resources.Load<GameObject>(string.Format("Obstacles/{0}", name));
        }

        /// <summary>
        /// Resource name of the k-th frame of an obstacle's animation: the model
        /// itself is frame 1, then {DefinitionKey}_f2, _f3, ... beside it.
        /// </summary>
        private static string FrameName(string definitionKey, int frame)
        {
            return string.Format("{0}_f{1}", definitionKey, frame);
        }

        /// <summary>
        /// Looks for the obstacle's later animation frames and, when there are any,
        /// instantiates each under the obstacle root with the same upright inner
        /// transform as the first frame, and returns the ObstacleAnimation that plays
        /// them. Returns null -- and adds nothing -- for an obstacle with one model.
        /// </summary>
        private static ObstacleAnimation AttachAnimationFrames(GameObject obj, ObstacleDefinition obstacle)
        {
            List<GameObject> framePrefabs = new List<GameObject>();
            for (int k = 2; ; k++)
            {
                GameObject framePrefab = LoadObstaclePrefab(FrameName(obstacle.DefinitionKey, k));
                if (framePrefab == null)
                {
                    break;
                }
                framePrefabs.Add(framePrefab);
            }

            if (framePrefabs.Count == 0)
            {
                return null;
            }

            ObstacleAnimation animation = obj.AddComponent<ObstacleAnimation>();

            // Frame 1 is the root's own renderers -- collected before the other frames
            // are parented under it.
            animation.AddFrame(obj);

            for (int i = 0; i < framePrefabs.Count; i++)
            {
                GameObject frame = Instantiate(framePrefabs[i]);
                frame.name = FrameName(obstacle.DefinitionKey, i + 2);
                frame.transform.SetParent(obj.transform, false);
                frame.transform.localPosition = Vector3.zero;
                frame.transform.localRotation = Quaternion.identity;
                frame.transform.localScale = Vector3.one;

                Transform frameInner = frame.transform.Find("default");
                if (frameInner != null)
                {
                    frameInner.SetLocalPositionAndRotation(new Vector3(0, 0, 0), Quaternion.Euler(180, 0, 0));
                }

                animation.AddFrame(frame);
            }

            // Stagger the clocks so a row of pillars does not flicker in step. The
            // golden-ratio step spreads consecutive ids evenly around the cycle.
            float cycle = animation.FrameCount / ObstacleAnimation.FramesPerSecond;
            animation.SetPhase((obstacle.Id * 0.618034f) % 1f * cycle);

            return animation;
        }

        // One map tile spans 2 world units; see MapCoordinate.ConvertPosToVec3.
        private const float WorldUnitsPerTile = 2f;

        /// <summary>
        /// Uniform shrink applied to every obstacle model as it is placed. The models
        /// are authored to fill their tile footprint exactly, which leaves them looking
        /// slightly oversized next to the units and the terrain; a little air around
        /// each one reads better. This is presentation only -- the tiles an obstacle
        /// occupies are unchanged (see the footprint math in buildObstacles).
        /// </summary>
        private const float ObstacleScale = 0.9f;

        /// <summary>
        /// Publishes the map's world-space rectangle to the "Custom/MapClip" shader as
        /// the global "_MapClipMinMaxXZ" (xy = min, zw = max). Tiles are 2-unit cells
        /// centred at MapCoordinate.ConvertPosToVec3 (-x*2, 0, y*2) for x in [1,Width]
        /// and y in [1,Height], so the board spans these world bounds (+/-1 = half tile).
        /// </summary>
        internal static void SetMapClipBounds(FDField field)
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

        // Keys of the ground covers: "lava_" + chapter + "_" + tile id, one flat
        // one-voxel sheet per lava tile shape (Tools/MapPipeline/build_obstacles_25.py).
        private const string LavaKeyPrefix = "lava_";

        /// <summary>
        /// Whether the obstacle is a ground cover: a flat sheet that lies on its tiles
        /// rather than standing on them -- chapter 25's lava. A cover keeps the full
        /// tile size instead of the ObstacleScale shrink (its edges must meet the
        /// neighbouring covers), is seated on the tile's top surface instead of at
        /// y = 0, is never faded, and glows without a light (GetGlow).
        /// </summary>
        private static bool IsGroundCover(string definitionKey)
        {
            return definitionKey != null && definitionKey.StartsWith(LavaKeyPrefix);
        }

        private static float GetObstacleScale(string definitionKey)
        {
            return IsGroundCover(definitionKey) ? 1f : ObstacleScale;
        }

        /// <summary>
        /// The vertical fine offset for a model with no ObstacleAnchor: the per-key
        /// table, except that a ground cover is lifted by the tiles' own thickness so
        /// it lies on the ground rather than inside it (ShapesLayer measured that
        /// height when it built the tiles, which happens before the obstacles).
        /// </summary>
        private float GroundYOffset(string definitionKey)
        {
            if (IsGroundCover(definitionKey))
            {
                return gameMap != null ? gameMap.GetGroundSurfaceHeight() : 0f;
            }
            return GetGroundYOffset(definitionKey);
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
