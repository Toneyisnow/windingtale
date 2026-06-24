using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor;
using UnityEditor.Animations;
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
using static UnityEditor.PlayerSettings;

namespace WindingTale.MapObjects.GameMap
{
    public class GameMap : MonoBehaviour
    {
        public GameObject creatureIconPrefab;

        public GameObject fieldLayer;

        public GameObject creaturesLayer;

        public GameObject obstaclesLayer;

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

        private GameObject cursorObject = null;
        private Cursor cursor = null;

        

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

        }

        /// <summary>
        /// Make an animation for cursor moving
        /// </summary>
        /// <param name="position"></param>
        public void SlideCursorTo(FDPosition position)
        {

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

            // Dim the tiles the menu covers, and the creatures standing on them, so the
            // menu reads clearly (same treatment as the move/target indicators).
            setMenuTilesFaded(menu, true);
        }

        public void CloseMenu(FDMenu menu)
        {
            GameObject menuObject = indicatorsLayer.transform.Find("menu")?.gameObject;
            if (menuObject != null)
            {
                // Direct destroy the menu object
                //// Destroy(menuObject.gameObject);

                // Close with animation
                Menu menuComponent = menuObject.GetComponent<Menu>();
                menuComponent.CloseMenu();


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