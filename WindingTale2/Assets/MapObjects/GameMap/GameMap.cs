using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using WindingTale.Chapters;
using WindingTale.Core.Algorithms;
using WindingTale.Core.Common;
using WindingTale.Core.Definitions;
using WindingTale.Core.Map;
using WindingTale.Core.Objects;
using WindingTale.MapObjects.CreatureIcon;
using static UnityEditor.PlayerSettings;

namespace WindingTale.MapObjects.GameMap
{
    public class GameMap : MonoBehaviour
    {
        public GameObject creatureIconPrefab;

        public GameObject fieldLayer;

        public GameObject creaturesLayer;

        public GameObject indicatorsLayer;

        public GameObject cursorPrefab;

        public GameObject menuPrefab;


        private Material defaultMaterial = null;

        private GameObject cursorObject = null;
        private Cursor cursor = null;


        public FDMap Map { get; private set; }

        void Start()
        {
            defaultMaterial = Resources.Load<Material>("Materials/material-fd2-palette");

            cursorObject = Instantiate(cursorPrefab);
            cursorObject.transform.parent = indicatorsLayer.transform;
            cursorObject.name = "cursor";
            cursor = cursorObject.GetComponent<Cursor>();

            SetCursorTo(FDPosition.At(8, 12));
        }

        public void Initialize(int chapterId)
        {
            this.Map = FDMap.loadFromChapter(chapterId);

            FieldLayer fieldComponent = fieldLayer.GetComponent<FieldLayer>();
            fieldComponent.Initialize(this.Map.Field);

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

        public void ShowMenu(FDMenu menu)
        {
            GameObject menuObject = Instantiate(menuPrefab, indicatorsLayer.transform);
            menuObject.name = "menu";

            menuObject.transform.SetLocalPositionAndRotation(MapCoordinate.ConvertPosToVec3(menu.Position), Quaternion.identity);
            Menu menuComponent = menuObject.GetComponent<Menu>();
            menuComponent.Init(menu);
        }

        public void CloseMenu(FDMenu menu)
        {
            GameObject menuObject = indicatorsLayer.transform.Find("menu").gameObject;
            if (menuObject != null)
            {
                Destroy(menuObject.gameObject);
            }
        }


        public void showMoveRange(FDCreature creature, FDMoveRange moveRange)
        {
            Debug.Log("showMoveRange");

            GameObject indicatorPrefab = Resources.Load<GameObject>("Others/Cursors/MoveIndicator");
            foreach (FDPosition position in moveRange.ToList())
            {
                GameObject indicator = MonoBehaviour.Instantiate(indicatorPrefab, indicatorsLayer.transform);
                indicator.name = "move_indicator";
                indicator.transform.SetLocalPositionAndRotation(MapCoordinate.ConvertPosToVec3(position), Quaternion.identity);
            }
        }

        public void showActionTargetRange(FDCreature creature, FDRange targetRange)
        {
            Debug.Log("showAttackRange");

            GameObject indicatorPrefab = Resources.Load<GameObject>("Others/Cursors/MoveIndicator");
            foreach (FDPosition position in targetRange.ToList())
            {
                GameObject indicator = MonoBehaviour.Instantiate(indicatorPrefab, indicatorsLayer.transform);
                indicator.name = "move_indicator";
                indicator.transform.SetLocalPositionAndRotation(MapCoordinate.ConvertPosToVec3(position), Quaternion.identity);
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
        }


        public void ResetCreaturePosition(FDCreature creature, FDPosition position)
        {

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

        public void MoveCreature(FDCreature creature, FDPosition position)
        {
            string creatureName = string.Format("creature_{0}", StringUtils.Digit3(creature.Id));
            creature.Position = position;

            Transform creatureIcon = this.creaturesLayer.transform.Find(creatureName);
            if (creatureIcon != null)
            {
                creatureIcon.SetPositionAndRotation(MapCoordinate.ConvertCreaturePosToVec3(position), Quaternion.identity);
            }
        }


        #region Private Methods

        private void AddCreatureUI(FDCreature creature, FDPosition pos)
        {
            GameObject creatureIcon = Instantiate(creatureIconPrefab);

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
            GameObject d = prefab.transform.Find("default").gameObject;
            MeshRenderer renderer = d.GetComponent<MeshRenderer>();
            renderer.materials = new Material[1] { defaultMaterial };

            GameObject icon = Instantiate(prefab);
            icon.transform.SetParent(parent);
            icon.transform.SetLocalPositionAndRotation(new Vector3(0, 0, 0), Quaternion.identity);
        }

        #endregion
    }
}