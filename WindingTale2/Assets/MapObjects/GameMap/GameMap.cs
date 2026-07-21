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

        // How opaque a creature stays while a menu item covers its tile.
        private const float MenuCreatureAlpha = 0.7f;

        // Acted (greyed-out) creatures use a lower alpha; the grey shader washes them
        // out, so 0.7 barely reads as faded.
        private const float MenuActionedCreatureAlpha = 0.4f;

        // How opaque an obstacle stays while the cursor or a menu item covers one of
        // its tiles. Same treatment as a creature standing under a menu item.
        private const float ObstacleAlpha = 0.7f;

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
                obstaclesComponent.Initialize(this.Map.Field);
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
            cursorObject.transform.SetLocalPositionAndRotation(MapCoordinate.ConvertPosToVec3(position), Quaternion.identity);

            refreshObstacleFade();
        }

        /// <summary>
        /// Shows or hides the map cursor (hidden while a menu is open).
        /// </summary>
        public void SetCursorVisible(bool visible)
        {
            if (cursorObject != null)
            {
                cursorObject.SetActive(visible);
                refreshObstacleFade();
            }
        }

        /// <summary>
        /// Fades every obstacle whose footprint is currently under the cursor or under
        /// one of the open menu's item tiles, and restores all the others. Recomputed
        /// from scratch on each cursor move / menu change, so no state can leak.
        /// </summary>
        private void refreshObstacleFade()
        {
            ObstaclesLayer obstacles = obstaclesLayer != null ? obstaclesLayer.GetComponent<ObstaclesLayer>() : null;
            if (obstacles == null)
            {
                return;
            }

            obstacles.ResetAllTransparency();

            // The cursor only counts while it is actually on screen (it is hidden
            // while a menu is open).
            if (cursor != null && cursorObject != null && cursorObject.activeSelf)
            {
                obstacles.GetObstacleAt(cursor.Position)?.SetTransparency(ObstacleAlpha);
            }

            if (currentMenu != null)
            {
                foreach (FDPosition tile in FDMenu.GetItemPositions(currentMenu.Position))
                {
                    obstacles.GetObstacleAt(tile)?.SetTransparency(ObstacleAlpha);
                }
            }
        }

        // Cursor slide speed, in map tiles per second.
        private const float CursorSlideTilesPerSecond = 20f;

        // World units per tile (see MapCoordinate.ConvertPosToVec3, which scales by 2).
        private const float WorldUnitsPerTile = 2f;

        private Coroutine cursorSlideCoroutine = null;

        private MainCamera mainCamera = null;

        // True while either the cursor or its follow camera is still animating a slide.
        public bool IsSlideBusy => cursorSlideCoroutine != null
            || (mainCamera != null && mainCamera.IsFollowSliding);

        /// <summary>
        /// Slides the cursor to the given tile: first horizontally from (x0, y0) to
        /// (x, y0), then vertically to (x, y), at a constant tiles-per-second speed.
        /// </summary>
        public void SlideCursorTo(FDPosition position, GameCanvas.DialogPosition dialogPosition)
        {
            if (cursor == null || cursorObject == null || position == null)
            {
                return;
            }

            if (cursorSlideCoroutine != null)
            {
                StopCoroutine(cursorSlideCoroutine);
            }
            cursorSlideCoroutine = StartCoroutine(CursorSlideCoroutine(position, dialogPosition));
        }

        private IEnumerator CursorSlideCoroutine(FDPosition target, GameCanvas.DialogPosition dialogPosition)
        {
            FDPosition from = cursor.Position;

            // Update the logical position immediately; only the visual glides.
            cursor.Position = target;
            refreshObstacleFade();

            // Path in tile space: (x0,y0) -> (x,y0) -> (x,y).
            Vector3 p0 = MapCoordinate.ConvertPosToVec3(from);
            Vector3 p1 = MapCoordinate.ConvertPosToVec3(FDPosition.At(target.X, from.Y));
            Vector3 p2 = MapCoordinate.ConvertPosToVec3(target);

            float worldSpeed = CursorSlideTilesPerSecond * WorldUnitsPerTile;

            // Have the camera follow: it slides straight from p0 to p2 (ease in / out)
            // over the same duration as the cursor's L-shaped path.
            int tileDistance = Mathf.Abs(target.X - from.X) + Mathf.Abs(target.Y - from.Y);
            float slideDuration = tileDistance / CursorSlideTilesPerSecond;
            EnsureMainCamera();
            if (mainCamera != null)
            {
                // Top dialog covers the top of the screen: keep the cursor lower.
                bool keepCursorLow = dialogPosition == GameCanvas.DialogPosition.Top;
                mainCamera.SlideFocusTo(p2, slideDuration, keepCursorLow);
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

            // Obstacles under the menu items fade too; recomputed centrally so the
            // cursor and the menu can't clobber each other's fades.
            currentMenu = faded ? menu : null;
            refreshObstacleFade();

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
                indicator.transform.SetLocalPositionAndRotation(MapCoordinate.ConvertPosToVec3(position), Quaternion.identity);
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
                indicator.transform.SetLocalPositionAndRotation(MapCoordinate.ConvertPosToVec3(position), Quaternion.identity);

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