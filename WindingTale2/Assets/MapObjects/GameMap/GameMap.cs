using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;
using WindingTale.Chapters;
using WindingTale.Core.Common;
using WindingTale.Core.Definitions;
using WindingTale.Core.Map;
using WindingTale.Core.Objects;
using WindingTale.MapObjects.CreatureIcon;

namespace WindingTale.MapObjects.GameMap
{
    public class GameMap : MonoBehaviour
    {
        public GameObject creatureIconPrefab;

        public GameObject fieldLayer;

        public GameObject creaturesLayer;

        public GameObject indicatorsLayer;

        private Material defaultMaterial = null;

        public FDMap Map { get; private set; }

        void Start()
        {
            defaultMaterial = Resources.Load<Material>("Materials/material-fd2-palette");

        }

        public void Initialize(int chapterId)
        {
            this.Map = FDMap.loadFromChapter(chapterId);

            FieldLayer fieldComponent = fieldLayer.GetComponent<FieldLayer>();
            fieldComponent.Initialize(this.Map.Field);

        }


        //// public FDEvent[] Events { get; set; }


        public void clearAllIndicators()
        {
            foreach(Transform child in this.indicatorsLayer.transform)
            {
                if (child.gameObject.tag == "move_range")
                {
                    Destroy(child.gameObject);
                }
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

        private void AddCreatureUI(FDCreature creature, FDPosition pos)
        {
            GameObject creatureIcon = Instantiate(creatureIconPrefab);

            creatureIcon.name = string.Format("creature_{0}", StringUtils.Digit3(creature.Id));
            creatureIcon.transform.SetParent(creaturesLayer.transform);
            creatureIcon.transform.SetPositionAndRotation(MapCoordinate.ConvertPosToVec3(pos), Quaternion.identity);

            AttachIcon(string.Format("Icons/{0}/Icon_{0}_01", StringUtils.Digit3(creature.Definition.AnimationId)), creatureIcon.transform.Find("Clip_01"));
            AttachIcon(string.Format("Icons/{0}/Icon_{0}_02", StringUtils.Digit3(creature.Definition.AnimationId)), creatureIcon.transform.Find("Clip_02"));
            AttachIcon(string.Format("Icons/{0}/Icon_{0}_03", StringUtils.Digit3(creature.Definition.AnimationId)), creatureIcon.transform.Find("Clip_03"));

            Creature comp = creatureIcon.GetComponent<Creature>();
            comp.SetCreature(creature);

            creature.Position = pos;
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
    }
}