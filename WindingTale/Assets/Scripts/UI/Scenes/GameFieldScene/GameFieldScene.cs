using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WindingTale.Core.Algorithms;
using WindingTale.Core.Common;
using WindingTale.Core.Definitions;
using WindingTale.Core.Objects;
using WindingTale.UI.Activities;
using WindingTale.UI.Common;
using WindingTale.UI.Scenes.Game;

namespace WindingTale.UI.Scenes
{
    public class GameFieldScene : MonoBehaviour, IGameInterface
    {
        public GameObject creatureIconPrefab;

        public GameObject mapNode;

        public GameObject canvas;
        
        private GameMain gameMain;

        private Material defaultMaterial = null;

        // Start is called before the first frame update
        void Start()
        {
            defaultMaterial = Resources.Load<Material>("Materials/material-fd2-palette");

            ActivityManager activityManager = this.gameObject.GetComponent<ActivityManager>();
            gameMain = GameMain.StartNewGame(this, activityManager);

        }

        // Update is called once per frame
        void Update()
        {

        }

        public void AddCreatureUI(FDCreature creature, FDPosition pos)
        {
            GameObject creatureIcon = Instantiate(creatureIconPrefab);

            creatureIcon.name = string.Format("creature_{0}", StringUtils.Digit3(creature.Id));
            creatureIcon.transform.SetParent(mapNode.transform);
            creatureIcon.transform.SetPositionAndRotation(MapCoordinate.ConvertPosToVec3(pos), Quaternion.identity);

            AttachIcon(string.Format("Icons/{0}/Icon_{0}_01", StringUtils.Digit3(creature.Definition.AnimationId)), creatureIcon.transform.Find("Clip_01"));
            AttachIcon(string.Format("Icons/{0}/Icon_{0}_02", StringUtils.Digit3(creature.Definition.AnimationId)), creatureIcon.transform.Find("Clip_02"));
            AttachIcon(string.Format("Icons/{0}/Icon_{0}_03", StringUtils.Digit3(creature.Definition.AnimationId)), creatureIcon.transform.Find("Clip_03"));

            Creature comp = creatureIcon.GetComponent<Creature>();
            comp.SetCreature(creature);

            creature.Position = pos;
        }

        public void RemoveCreatureUI(FDCreature creature)
        {
            GameObject creatureObject = mapNode.transform.Find(string.Format("creature_{0}", StringUtils.Digit3(creature.Id)))?.gameObject;
            if (creatureObject != null)
            {
                Destroy(creatureObject);
            }
        }


        /*
        public void MoveCreatureUI(FDCreature creature, FDMovePath path)
        {
            GameObject creatureObject = mapNode.transform.Find(string.Format("creature_{0}", StringUtils.Digit3(creature.Id))).gameObject;

            Creature creatureComp = creatureObject.GetComponent<Creature>();
            creatureComp.SetCreature(creature);
            creatureComp.StartMove(path);

        }
        */

        #region Private Methods

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
