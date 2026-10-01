using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;
using WindingTale.Chapters;
using WindingTale.Core.Algorithms;
using WindingTale.Core.Common;
using WindingTale.Core.Definitions;
using WindingTale.Core.Map;
using WindingTale.Core.Objects;
using WindingTale.MapObjects.CreatureIcon;
using WindingTale.Scenes.GameFieldScene;

namespace WindingTale.MapObjects.GameMap
{
    public class GameMap : MonoBehaviour
    {
        public GameObject creatureIconPrefab;

        public GameObject fieldLayer;

        public GameObject creaturesLayer;

        public GameObject obstaclesLayer;

        // Holds the map's treasure chests (see ObjectsLayer).
        public GameObject objectsLayer;

        public GameObject indicatorsLayer;

        public GameObject cursorPrefab;

        public GameObject menuPrefab;



        // How opaque a tile's grass stays while an indicator/menu covers it (0 = invisible).
        private const float IndicatorTileAlpha = 0.2f;

        // How opaque a creature stays while a menu item covers its tile: 20%, the same as
        // the grass, so the menu icon reads clearly over it. (0.7 barely showed under the
        // VoxelCreature shader's rim light.)
        private const float MenuCreatureAlpha = 0.2f;

        // Acted (greyed-out) creatures fade the same way.
        private const float MenuActionedCreatureAlpha = 0.2f;

        private GameObject cursorObject = null;
        private Cursor cursor = null;

        // The menu currently on screen, if any. Kept so the obstacle fade can be
        // recomputed from both the cursor and the menu tiles at once.
        private FDMenu currentMenu = null;

        // Tiles currently covered by a move/target range indicator. Tracked here rather
        // than read back off the spawned "move_indicator" objects, so a prop can tell
        // whether to fade without walking the indicator layer every frame.
        private readonly List<FDPosition> indicatorTiles = new List<FDPosition>();

        // The live menu's component. Held directly rather than looked up by name: a
        // closing menu lingers in the hierarchy while the next one is already open.
        private Menu currentMenuComponent = null;

        

        public FDMap Map { get; private set; }

        void Start()
        {
            cursorObject = Instantiate(cursorPrefab);
            cursorObject.transform.parent = indicatorsLayer.transform;
            cursorObject.name = "cursor";
            cursor = cursorObject.GetComponent<Cursor>();

            SetCursorTo(FDPosition.At(8, 12));

            //// var animator = sampleFight.GetComponent<Animator>();
            //// var controller = Resources.Load<AnimatorController>("Fights/734/animator_734");

            //// AnimatorController.SetAnimatorController(animator, controller);

            //// var anim = 9;
            //// animator.runtimeAnimatorController = Resources.Load<AnimatorController>(
            ////    string.Format("Fights/{0}/animator_{0}", StringUtils.Digit3(anim))
            //// );

            //// animator.SetInteger("actionState", 1);

        }

        public void Initialize(int chapterId)
        {
            this.Map = FDMap.LoadFromChapter(chapterId);

            ShapesLayer fieldComponent = fieldLayer.GetComponent<ShapesLayer>();
            fieldComponent.Initialize(this.Map.Field);

            if (obstaclesLayer != null)
            {
                ObstaclesLayer obstaclesComponent = obstaclesLayer.GetComponent<ObstaclesLayer>();
                if (obstaclesComponent == null)
                {
                    obstaclesComponent = obstaclesLayer.AddComponent<ObstaclesLayer>();
                }
                obstaclesComponent.Initialize(this.Map, this);
            }

            // Fall back to the child by name when the Inspector reference is unset, so
            // the chests still appear in a scene that was serialized before the
            // objectsLayer field existed.
            GameObject objects = objectsLayer;
            if (objects == null)
            {
                Transform found = this.transform.Find("ObjectsLayer");
                objects = found != null ? found.gameObject : null;
            }

            if (objects != null)
            {
                ObjectsLayer objectsComponent = objects.GetComponent<ObjectsLayer>();
                if (objectsComponent == null)
                {
                    objectsComponent = objects.AddComponent<ObjectsLayer>();
                }
                objectsComponent.Initialize(this.Map, this);
            }
            else
            {
                Debug.LogWarning("GameMap: no ObjectsLayer found; treasures will not be drawn.");
            }
        }

        /// <summary>
        /// The tiles where a UI element currently sits on top of the board: the cursor
        /// (only while it is on screen -- it hides behind an open menu), the open menu's
        /// four item tiles, and any move/target range indicators. Props standing on
        /// these tiles fade so the UI reads through them.
        /// </summary>
        public FDPosition[] GetFadeTiles()
        {
            List<FDPosition> tiles = new List<FDPosition>();

            if (cursor != null && cursorObject != null && cursorObject.activeSelf)
            {
                tiles.Add(cursor.Position);
            }

            if (currentMenu != null)
            {
                tiles.AddRange(FDMenu.GetItemPositions(currentMenu.Position));
            }

            tiles.AddRange(indicatorTiles);

            return tiles.ToArray();
        }


        //// public FDEvent[] Events { get; set; }

        public FDPosition GetCursorPosition()
        {
            return cursor.Position;
        }

        public void SetCursorTo(FDPosition position)
        {
            cursor.Position = position;
            cursorObject.transform.SetLocalPositionAndRotation(indicatorPositionAt(position), Quaternion.identity);
        }

        // The cursor's own (single-tile) mesh, kept so SetCursorScope can put it back.
        private Mesh singleTileCursorMesh = null;

        /// <summary>
        /// Switches the cursor to the outline of a spell's blast. The scope is the magic data's
        /// EffectScope, counted from 0: 0 is the target tile alone (the ordinary cursor), 1 the
        /// cross of five (回复术), 2 and 3 the wider diamonds. Scope N uses the original's
        /// Cursor-0(N+1) (Resources/Others/Cursors/Cursor_N+1, the diamond of every tile within
        /// N of the centre); anything past 3 uses the widest, Cursor_4. Only the mesh is
        /// swapped, so the cursor keeps its material, position and slide.
        /// </summary>
        public void SetCursorScope(int scope)
        {
            MeshFilter filter = cursorObject != null ? cursorObject.GetComponentInChildren<MeshFilter>(true) : null;
            if (filter == null)
            {
                return;
            }

            if (singleTileCursorMesh == null)
            {
                singleTileCursorMesh = filter.sharedMesh;
            }

            Mesh mesh = singleTileCursorMesh;
            if (scope >= 1)
            {
                GameObject model = Resources.Load<GameObject>(string.Format("Others/Cursors/Cursor_{0}", Mathf.Min(scope + 1, 4)));
                MeshFilter modelFilter = model != null ? model.GetComponentInChildren<MeshFilter>(true) : null;
                if (modelFilter != null && modelFilter.sharedMesh != null)
                {
                    mesh = modelFilter.sharedMesh;
                }
            }

            filter.sharedMesh = mesh;
        }

        /// <summary>
        /// Shows or hides the map cursor (hidden while a menu is open).
        /// </summary>
        public void SetCursorVisible(bool visible)
        {
            if (cursorObject != null)
            {
                cursorObject.SetActive(visible);
            }
        }

        // Cursor slide speed, in map tiles per second.
        private const float CursorSlideTilesPerSecond = 20f;

        // World units per tile (see MapCoordinate.ConvertPosToVec3, which scales by 2).
        private const float WorldUnitsPerTile = 2f;

        private Coroutine cursorSlideCoroutine = null;

        private MainCamera mainCamera = null;

        /// <summary>
        /// True while the cursor itself is gliding to a tile. Its logical position is already
        /// the destination, so moving it by hand now would leave the glide to drag the visual
        /// back to the old destination -- the player's input is ignored until it lands.
        /// </summary>
        public bool IsCursorSliding => cursorSlideCoroutine != null;

        // True while either the cursor or its follow camera is still animating a slide.
        public bool IsSlideBusy => cursorSlideCoroutine != null
            || (mainCamera != null && mainCamera.IsFollowSliding);

        /// <summary>
        /// Slides the cursor to the given tile: first horizontally from (x0, y0) to
        /// (x, y0), then vertically to (x, y), at a constant tiles-per-second speed.
        ///
        /// keepCameraFraming is for a slide the player asked for (cycling to the next
        /// friend, a target state parking on its target): the camera only pans across at
        /// the height and angle the player left it, and lets go when it lands. Otherwise
        /// the camera takes the conversation framing and returns to where it was once the
        /// activity queue runs dry (see MainCamera.SlideFocusTo / ReturnToGameplay).
        /// </summary>
        public void SlideCursorTo(FDPosition position, GameCanvas.DialogPosition dialogPosition, bool keepCameraFraming = false)
        {
            if (cursor == null || cursorObject == null || position == null)
            {
                return;
            }

            if (cursorSlideCoroutine != null)
            {
                StopCoroutine(cursorSlideCoroutine);
            }
            cursorSlideCoroutine = StartCoroutine(CursorSlideCoroutine(position, dialogPosition, keepCameraFraming));
        }

        private IEnumerator CursorSlideCoroutine(FDPosition target, GameCanvas.DialogPosition dialogPosition, bool keepCameraFraming)
        {
            FDPosition from = cursor.Position;

            // Update the logical position immediately; only the visual glides.
            cursor.Position = target;

            // Path in tile space: (x0,y0) -> (x,y0) -> (x,y). Each corner sits on its
            // own tile's surface, so the cursor climbs onto a bridge as it slides.
            Vector3 p0 = indicatorPositionAt(from);
            Vector3 p1 = indicatorPositionAt(FDPosition.At(target.X, from.Y));
            Vector3 p2 = indicatorPositionAt(target);

            float worldSpeed = CursorSlideTilesPerSecond * WorldUnitsPerTile;

            // Have the camera follow: it slides straight from p0 to p2 (ease in / out)
            // over the same duration as the cursor's L-shaped path.
            int tileDistance = Mathf.Abs(target.X - from.X) + Mathf.Abs(target.Y - from.Y);
            float slideDuration = tileDistance / CursorSlideTilesPerSecond;
            EnsureMainCamera();
            if (mainCamera != null && keepCameraFraming)
            {
                mainCamera.PanFocusTo(MapCoordinate.ConvertPosToVec3(target), slideDuration);
            }
            else if (mainCamera != null)
            {
                // Top dialog covers the top of the screen: keep the cursor lower.
                bool keepCursorLow = dialogPosition == GameCanvas.DialogPosition.Top;
                mainCamera.SlideFocusTo(MapCoordinate.ConvertPosToVec3(target), slideDuration, keepCursorLow);
            }

            yield return MoveCursorAlong(p0, p1, worldSpeed);
            yield return MoveCursorAlong(p1, p2, worldSpeed);

            cursorObject.transform.localPosition = p2;
            cursorSlideCoroutine = null;
        }

        private void EnsureMainCamera()
        {
            if (mainCamera == null)
            {
                mainCamera = GameObject.FindFirstObjectByType<MainCamera>();
            }
        }

        /// <summary>
        /// Tells the camera to keep the cursor in view while the player drives it by
        /// keyboard: the camera pans (accel/decel) whenever the cursor drifts into the
        /// outer margin of the screen, and holds otherwise. Called on each keyboard
        /// cursor move.
        /// </summary>
        public void BeginCursorEdgeFollow()
        {
            EnsureMainCamera();
            if (mainCamera != null && cursorObject != null)
            {
                mainCamera.BeginCursorFollow(cursorObject.transform);
            }
        }

        /// <summary>
        /// Highlights the given menu item as the active one, swapping it to its "active"
        /// art variant (and restoring the others). No-op if the menu isn't shown.
        /// </summary>
        public void SetMenuActiveItem(int itemIndex)
        {
            // Must be the menu ShowMenu just created, not a by-name lookup: while a menu
            // opens another one, the previous menu's GameObject is still in the hierarchy
            // playing its close animation, and a name lookup would find that one instead.
            if (currentMenuComponent != null)
            {
                currentMenuComponent.SetActiveItem(itemIndex);
            }
        }

        /// <summary>
        /// Lifts the camera to its highest, most top-down framing, so the board still reads
        /// while a menu covers part of it. The player's next zoom takes control back.
        /// </summary>
        public void ZoomCameraToTop()
        {
            EnsureMainCamera();
            if (mainCamera != null)
            {
                mainCamera.ZoomToTop();
            }
        }

        /// <summary>
        /// Makes the tile the cursor was just slid to the place the camera settles on when
        /// the follow ends, instead of the spot it was standing at before the slide.
        /// Call right after SlideCursorTo. See MainCamera.RebaseSavedFraming.
        /// </summary>
        public void RebaseCameraOnTile(FDPosition position)
        {
            EnsureMainCamera();
            if (mainCamera != null && position != null)
            {
                mainCamera.RebaseSavedFraming(MapCoordinate.ConvertPosToVec3(position));
            }
        }

        /// <summary>
        /// Ends the conversation camera follow, smoothly handing control back to
        /// gameplay. Safe to call any time (no-op if the camera isn't following).
        /// </summary>
        public void EndCursorCameraFollow()
        {
            EnsureMainCamera();
            if (mainCamera != null)
            {
                mainCamera.ReturnToGameplay();
            }
        }

        private IEnumerator MoveCursorAlong(Vector3 a, Vector3 b, float worldSpeed)
        {
            float distance = Vector3.Distance(a, b);
            if (distance < 0.0001f)
            {
                cursorObject.transform.localPosition = b;
                yield break;
            }

            float traveled = 0f;
            while (traveled < distance)
            {
                traveled += worldSpeed * Time.deltaTime;
                float t = Mathf.Clamp01(traveled / distance);
                cursorObject.transform.localPosition = Vector3.Lerp(a, b, t);
                yield return null;
            }
            cursorObject.transform.localPosition = b;
        }

        public Creature GetCreature(FDCreature creature)
        {
            string creatureName = string.Format("creature_{0}", StringUtils.Digit3(creature.Id));
            Transform creatureIcon = this.creaturesLayer.transform.Find(creatureName);
            if (creatureIcon != null)
            {
                return creatureIcon.GetComponent<Creature>();
            }

            return null;
        }

        public void ShowMenu(FDMenu menu)
        {
            GameObject menuObject = Instantiate(menuPrefab, indicatorsLayer.transform);
            menuObject.name = "menu";

            menuObject.transform.SetLocalPositionAndRotation(MapCoordinate.ConvertPosToVec3(menu.Position), Quaternion.identity);
            Menu menuComponent = menuObject.GetComponent<Menu>();
            menuComponent.Init(menu);

            currentMenuComponent = menuComponent;

            // Dim the tiles the menu covers, and the creatures standing on them, so the
            // menu reads clearly (same treatment as the move/target indicators).
            setMenuTilesFaded(menu, true);
        }

        public void CloseMenu(FDMenu menu)
        {
            if (currentMenuComponent != null)
            {
                // Direct destroy the menu object
                //// Destroy(menuObject.gameObject);

                // Close with animation. The GameObject outlives this call by the length
                // of the slide-out, and a nested menu's ShowMenu runs in between, so
                // rename it: nothing may find the dying menu under the live menu's name.
                currentMenuComponent.gameObject.name = "menu_closing";
                currentMenuComponent.CloseMenu();
                currentMenuComponent = null;
            }

            // Restore the tiles and creatures dimmed by ShowMenu.
            setMenuTilesFaded(menu, false);
        }

        /// <summary>
        /// Fades (or restores) the grass on the menu's tiles and the creatures standing
        /// on them. Grass dims on the centre tile plus the four item tiles, but creatures
        /// are only dimmed on the four item tiles, NOT on the menu's own centre tile.
        /// </summary>
        private void setMenuTilesFaded(FDMenu menu, bool faded)
        {
            if (menu == null)
            {
                return;
            }

            // Obstacles and chests under the menu items fade too: their layers poll
            // GetFadeTiles, which reads this.
            currentMenu = faded ? menu : null;

            ShapesLayer shapes = getShapesLayer();
            FDPosition[] itemTiles = FDMenu.GetItemPositions(menu.Position);

            // Grass: centre tile + the four item tiles.
            if (shapes != null)
            {
                if (faded) shapes.SetTileFaded(menu.Position, IndicatorTileAlpha);
                else shapes.ResetTileFade(menu.Position);

                foreach (FDPosition tile in itemTiles)
                {
                    if (faded) shapes.SetTileFaded(tile, IndicatorTileAlpha);
                    else shapes.ResetTileFade(tile);
                }
            }

            // Creatures: only the four item tiles dim creatures (the menu's own tile,
            // where the acting creature usually stands, is left at full opacity).
            if (this.Map != null)
            {
                foreach (FDPosition tile in itemTiles)
                {
                    foreach (FDCreature c in this.Map.Creatures)
                    {
                        if (c.Position != null && c.Position.X == tile.X && c.Position.Y == tile.Y)
                        {
                            Creature comp = GetCreature(c);
                            if (comp != null)
                            {
                                if (faded)
                                {
                                    float alpha = c.HasActioned ? MenuActionedCreatureAlpha : MenuCreatureAlpha;
                                    comp.SetTransparency(alpha);
                                }
                                else
                                {
                                    comp.ResetTransparency();
                                }
                            }
                        }
                    }
                }
            }
        }


        public void showMoveRange(FDCreature creature, FDMoveRange moveRange)
        {
            Debug.Log("showMoveRange");

            ShapesLayer shapes = getShapesLayer();
            GameObject indicatorPrefab = Resources.Load<GameObject>("Others/Cursors/MoveIndicator");
            foreach (FDPosition position in moveRange.ToList())
            {
                GameObject indicator = Instantiate(indicatorPrefab, indicatorsLayer.transform);
                indicator.name = "move_indicator";
                indicator.transform.SetLocalPositionAndRotation(indicatorPositionAt(position), Quaternion.identity);
                indicator.transform.localScale = new Vector3(0.82f, 2f, 0.82f);
                indicator.AddComponent<BlockBlinkEffect>();
                indicatorTiles.Add(position);

                // Dim the tile's grass so it doesn't fight the indicator visually.
                if (shapes != null) shapes.SetTileFaded(position, IndicatorTileAlpha);
            }
        }

        public void showActionTargetRange(FDCreature creature, FDRange targetRange)
        {
            Debug.Log("showAttackRange");

            ShapesLayer shapes = getShapesLayer();
            GameObject indicatorPrefab = Resources.Load<GameObject>("Others/Cursors/MoveIndicator");
            foreach (FDPosition position in targetRange.ToList())
            {
                GameObject indicator = Instantiate(indicatorPrefab, indicatorsLayer.transform);
                indicator.name = "move_indicator";
                indicator.transform.localScale = new Vector3(0.82f, 2f, 0.82f);
                indicator.transform.SetLocalPositionAndRotation(indicatorPositionAt(position), Quaternion.identity);

                // Attack/magic range indicators blink slowly and nearly fade out, so
                // they read differently from the steadier move range (which uses the
                // BlockBlinkEffect defaults: blinkSpeed 2.0, minAlpha 0.15).
                BlockBlinkEffect blink = indicator.AddComponent<BlockBlinkEffect>();
                blink.blinkSpeed = 3.0f; // larger = slower fade than the move indicator (2.0)
                blink.minAlpha = 0.02f;  // fade almost to invisible
                indicatorTiles.Add(position);

                if (shapes != null) shapes.SetTileFaded(position, IndicatorTileAlpha);
            }
        }

        public void clearAllIndicators()
        {
            foreach(Transform child in this.indicatorsLayer.transform)
            {
                if (child.gameObject.name == "move_indicator")
                {
                    Destroy(child.gameObject);
                }
            }

            indicatorTiles.Clear();

            // Un-dim any tiles that were faded for the indicators above.
            ShapesLayer shapes = getShapesLayer();
            if (shapes != null) shapes.ResetFadedTiles();
        }


        // A flashing valid target swings between full opacity and this, in 0.6 seconds per
        // full there-and-back (100% -> 30% -> 100%).
        private const float BlinkMinAlpha = 0.3f;
        private const float BlinkPeriod = 0.6f;

        private class BlinkingCreature
        {
            public Creature creature;
            public float startTime;
        }

        // The creatures flashing right now, by creature id.
        private readonly Dictionary<int, BlinkingCreature> blinkingCreatures = new Dictionary<int, BlinkingCreature>();
        private readonly List<int> blinkScratch = new List<int>();

        // The colour the flashing creatures lean towards at their faintest.
        private Color blinkTint = Color.white;

        /// <summary>
        /// Makes exactly these creatures flash and everyone else stand still. Called every
        /// frame with the current answer, so a creature already flashing keeps its phase,
        /// a new one starts at full opacity, and one that dropped out is restored. Null
        /// or empty stops all flashing. The creatures shift towards <paramref name="tint"/>
        /// as they fade (none at full opacity, the whole of it at the faintest point);
        /// leave it out for no tint.
        /// </summary>
        public void SetBlinkTargets(List<FDCreature> targets, Color? tint = null)
        {
            blinkTint = tint ?? Color.white;

            if (blinkingCreatures.Count == 0 && (targets == null || targets.Count == 0))
            {
                return;
            }

            blinkScratch.Clear();
            foreach (KeyValuePair<int, BlinkingCreature> pair in blinkingCreatures)
            {
                if (targets == null || !targets.Exists(t => t.Id == pair.Key))
                {
                    blinkScratch.Add(pair.Key);
                }
            }

            foreach (int id in blinkScratch)
            {
                Creature stopped = blinkingCreatures[id].creature;
                if (stopped != null)
                {
                    stopped.EndBlink();
                }
                blinkingCreatures.Remove(id);
            }

            if (targets == null)
            {
                return;
            }

            foreach (FDCreature target in targets)
            {
                if (blinkingCreatures.ContainsKey(target.Id))
                {
                    continue;
                }

                Creature component = GetCreature(target);
                if (component == null)
                {
                    continue;
                }

                component.BeginBlink();
                blinkingCreatures[target.Id] = new BlinkingCreature { creature = component, startTime = Time.time };
            }
        }

        void Update()
        {
            if (blinkingCreatures.Count == 0)
            {
                return;
            }

            foreach (BlinkingCreature blinking in blinkingCreatures.Values)
            {
                if (blinking.creature == null)
                {
                    // Destroyed while flashing; SetBlinkTargets drops it on its next call.
                    continue;
                }

                // Cosine, so it opens at full opacity and dips to BlinkMinAlpha at mid-period.
                float phase = (Time.time - blinking.startTime) / BlinkPeriod;
                float t = 0.5f + 0.5f * Mathf.Cos(2f * Mathf.PI * phase);
                blinking.creature.SetBlink(Mathf.Lerp(BlinkMinAlpha, 1f, t), Color.Lerp(blinkTint, Color.white, t));
            }
        }

        public void ResetCreaturePosition(FDCreature creature, FDPosition position)
        {
            //// creature.Position = position;
            Transform creatureIcon = getCreatureObjectById(creature.Id);
            if (creatureIcon != null)
            {
                creatureIcon.SetPositionAndRotation(MapCoordinate.ConvertCreaturePosToVec3(position), Quaternion.identity);
            }

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="creature"></param>
        /// <param name="pos"></param>
        public void AddCreature(FDCreature creature, FDPosition position)
        {
            creature.Position = position;
            this.Map.Creatures.Add(creature);

            AddCreatureUI(creature, position);
        }

        /// <summary>
        /// Directly remove the creature from the map
        /// </summary>
        /// <param name="creature"></param>
        public void RemoveCreature(int creatureId)
        {
            string creatureKey = string.Format("creature_{0}", StringUtils.Digit3(creatureId));
            Transform creatureIcon = this.creaturesLayer.transform.Find(creatureKey);
            if (creatureIcon != null)
            {
                Destroy(creatureIcon.gameObject);
            }

            FDCreature creature = this.Map.Creatures.Find(c => c.Id == creatureId);
            this.Map.Creatures.Remove(creature);
        }

        /// <summary>
        /// Puts a creature that is already on the map onto another tile at once, with no
        /// walk -- the original's setLocation, which chapter 30 uses to bring each boss
        /// down from the wall to the altar the turn his guard has fallen.
        /// </summary>
        public void RelocateCreature(int creatureId, FDPosition position)
        {
            FDCreature creature = this.Map.Creatures.Find(c => c.Id == creatureId);
            if (creature == null)
            {
                return;
            }

            creature.Position = position;

            string creatureName = string.Format("creature_{0}", StringUtils.Digit3(creatureId));
            Transform creatureIcon = this.creaturesLayer.transform.Find(creatureName);
            if (creatureIcon != null)
            {
                creatureIcon.SetPositionAndRotation(MapCoordinate.ConvertCreaturePosToVec3(position), Quaternion.identity);
            }
        }

        public void MoveCreature(FDCreature creature, FDMovePath movePath)
        {
            string creatureName = string.Format("creature_{0}", StringUtils.Digit3(creature.Id));
            Transform creatureIcon = this.creaturesLayer.transform.Find(creatureName);
            if (creatureIcon != null)
            {
                CreatureWalk walk = creatureIcon.AddComponent<CreatureWalk>();
                walk.Init(movePath);

                //// creatureIcon.SetPositionAndRotation(MapCoordinate.ConvertCreaturePosToVec3(position), Quaternion.identity);
            }
        }


        #region Private Methods

        private ShapesLayer getShapesLayer()
        {
            return fieldLayer != null ? fieldLayer.GetComponent<ShapesLayer>() : null;
        }

        /// <summary>The tile models' layer, or null before the scene is wired.</summary>
        public ShapesLayer Shapes
        {
            get { return getShapesLayer(); }
        }

        /// <summary>The obstacles' layer, or null when the map has none.</summary>
        public ObstaclesLayer Obstacles
        {
            get { return obstaclesLayer != null ? obstaclesLayer.GetComponent<ObstaclesLayer>() : null; }
        }

        /// <summary>
        /// The treasure chests' layer, or null when the scene has none. Looked up by
        /// name when the Inspector reference is unset, the same way Initialize does.
        /// </summary>
        public ObjectsLayer Objects
        {
            get
            {
                GameObject layer = objectsLayer;
                if (layer == null)
                {
                    Transform found = this.transform.Find("ObjectsLayer");
                    layer = found != null ? found.gameObject : null;
                }

                return layer != null ? layer.GetComponent<ObjectsLayer>() : null;
            }
        }

        /// <summary>
        /// True while the map cursor is on screen -- it hides behind an open menu.
        /// </summary>
        public bool IsCursorVisible
        {
            get { return cursor != null && cursorObject != null && cursorObject.activeSelf; }
        }

        /// <summary>
        /// The world position of a tile's model, i.e. where the tile centre stands
        /// on the board (the tile models are placed by ShapesLayer at exactly this).
        /// </summary>
        public Vector3 GetTileWorldCentre(FDPosition position)
        {
            Vector3 local = MapCoordinate.ConvertPosToVec3(position);
            return fieldLayer != null ? fieldLayer.transform.TransformPoint(local) : local;
        }

        /// <summary>
        /// Whether the tile is drawn in the right half of the screen. The corner panels
        /// use it to put themselves on the side the cursor is not on.
        /// </summary>
        public bool IsTileOnRightHalfOfScreen(FDPosition position)
        {
            return GetTileScreenFraction(position) > 0.5f;
        }

        /// <summary>
        /// Where the tile is drawn across the screen, 0 at the left edge and 1 at the
        /// right (0 when there is no camera or position). A cursor the camera has just
        /// centred sits right at 0.5, so a panel that flips sides on the halves should
        /// use this and leave a dead band around the middle.
        /// </summary>
        public float GetTileScreenFraction(FDPosition position)
        {
            Camera camera = Camera.main;
            if (camera == null || position == null)
            {
                return 0f;
            }

            Vector3 screen = camera.WorldToScreenPoint(GetTileWorldCentre(position));
            return screen.x / Screen.width;
        }

        /// <summary>
        /// How far above a tile's origin the board's usual ground surface sits, in
        /// world units (ShapesLayer.GroundSurfaceHeight); 0 before the tiles are built.
        /// </summary>
        public float GetGroundSurfaceHeight()
        {
            ShapesLayer shapes = getShapesLayer();
            return shapes != null ? shapes.GroundSurfaceHeight : 0f;
        }

        /// <summary>
        /// Where something that lies flat on a tile (the cursor, a range indicator)
        /// goes: the tile centre, raised onto the tile's surface when the tile as a
        /// whole stands above the usual ground -- a bridge deck, for instance -- so
        /// the tile does not hide it. Ordinary tiles get no lift (see
        /// ShapesLayer.GetIndicatorLift).
        /// </summary>
        private Vector3 indicatorPositionAt(FDPosition position)
        {
            Vector3 tile = MapCoordinate.ConvertPosToVec3(position);

            ShapesLayer shapes = getShapesLayer();
            if (shapes != null)
            {
                tile.y += shapes.GetIndicatorLift(position);
            }

            return tile;
        }

        private void AddCreatureUI(FDCreature creature, FDPosition pos)
        {
            GameObject creatureIcon = Instantiate(creatureIconPrefab);

            if (creature.Id == 11)
            {
                Debug.Log("Here");
            }

            creatureIcon.name = string.Format("creature_{0}", StringUtils.Digit3(creature.Id));
            creatureIcon.transform.SetParent(creaturesLayer.transform);
            creatureIcon.transform.SetPositionAndRotation(MapCoordinate.ConvertCreaturePosToVec3(pos), Quaternion.identity);

            AttachIcon(string.Format("Icons/{0}/Icon_{0}_01", StringUtils.Digit3(creature.Definition.AnimationId)), creatureIcon.transform.Find("Clip_01"));
            AttachIcon(string.Format("Icons/{0}/Icon_{0}_02", StringUtils.Digit3(creature.Definition.AnimationId)), creatureIcon.transform.Find("Clip_02"));
            AttachIcon(string.Format("Icons/{0}/Icon_{0}_03", StringUtils.Digit3(creature.Definition.AnimationId)), creatureIcon.transform.Find("Clip_03"));

            Creature comp = creatureIcon.GetComponent<Creature>();
            comp.SetCreature(creature);
            comp.InitializeClipVisibility();
        }

        private void AttachIcon(string iconFilePath, Transform parent)
        {
            Debug.Log("iconFilePath: " + iconFilePath);

            GameObject prefab = Resources.Load<GameObject>(iconFilePath);
            GameObject icon = Instantiate(prefab);
            CreatureMaterial.Apply(icon);
            icon.transform.SetParent(parent);
            icon.transform.SetLocalPositionAndRotation(new Vector3(0, 0, 0), Quaternion.identity);
        }

        private Transform getCreatureObjectById(int creatureId)
        {
            string creatureName = string.Format("creature_{0}", StringUtils.Digit3(creatureId));
            Transform creatureIcon = this.creaturesLayer.transform.Find(creatureName);
            return creatureIcon;
        }

        #endregion
    }
}