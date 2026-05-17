using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using WindingTale.Core.Objects;
using WindingTale.Core.Definitions;
using WindingTale.Core.Algorithms;
using WindingTale.Scenes.GameFieldScene;
using UnityEngine.EventSystems;

namespace WindingTale.MapObjects.CreatureIcon
{
    public class Creature : MonoBehaviour, IPointerClickHandler
    {
        private bool isInitialized = false;

        private int moveCount = 0;
        private bool isMoving = false;

        private FDMovePath path = null;

        private Material[] originalMaterials1 = null;
        private Material[] originalMaterials2 = null;
        private Material[] originalMaterials3 = null;

        public FDCreature creature
        {
            get; private set;
        }

        public void SetCreature(FDCreature creature)
        {
            this.creature = creature;
            isInitialized = true;
        }

        /// <summary>
        /// Reset to initial state when a new turn starts
        /// </summary>
        public void ResetTurnState()
        {
            this.SetActioned(false);
            this.creature.PrePosition = null;
        }

        public void SetGreyout(bool greyout)
        {
            GameObject obj1 = gameObject.transform.Find("Clip_01").GetChild(0).Find("default").gameObject;
            GameObject obj2 = gameObject.transform.Find("Clip_02").GetChild(0).Find("default").gameObject;
            GameObject obj3 = gameObject.transform.Find("Clip_03").GetChild(0).Find("default").gameObject;

            if (greyout)
            {
                MeshRenderer r1 = obj1.GetComponent<MeshRenderer>();
                MeshRenderer r2 = obj2.GetComponent<MeshRenderer>();
                MeshRenderer r3 = obj3.GetComponent<MeshRenderer>();
                // Save shared material references before greying out; guard against double-call overwriting originals
                if (originalMaterials1 == null && r1 != null) originalMaterials1 = r1.sharedMaterials;
                if (originalMaterials2 == null && r2 != null) originalMaterials2 = r2.sharedMaterials;
                if (originalMaterials3 == null && r3 != null) originalMaterials3 = r3.sharedMaterials;
                GameRenderer.Instance.ApplyDefaultGreyMaterial(obj1);
                GameRenderer.Instance.ApplyDefaultGreyMaterial(obj2);
                GameRenderer.Instance.ApplyDefaultGreyMaterial(obj3);
            }
            else
            {
                if (originalMaterials1 != null) { obj1.GetComponent<MeshRenderer>().sharedMaterials = originalMaterials1; originalMaterials1 = null; }
                if (originalMaterials2 != null) { obj2.GetComponent<MeshRenderer>().sharedMaterials = originalMaterials2; originalMaterials2 = null; }
                if (originalMaterials3 != null) { obj3.GetComponent<MeshRenderer>().sharedMaterials = originalMaterials3; originalMaterials3 = null; }
            }
        }

        // Start is called before the first frame update
        void Start()
        {
        }

        // Update is called once per frame
        void Update()
        {
            //if (isMoving)
            //{
            //    moveCount++;

            //    if (moveCount > 90)
            //    {
            //        isMoving = false;
            //        moveCount = 0;

            //        // update position
            //        creature.Position = path.Desitination;

            //        // remove component
            //        Destroy(gameObject.GetComponent<CreatureWalk>());
            //    }
            //}

        }

        public void OnPointerClick(PointerEventData eventData)
        {
            Debug.Log("OnPointerClick " + this.creature.Position.ToString());

            PlayerInterface.getDefault().onSelectedPosition(this.creature.Position);
        }

        public void SetActioned(bool actioned)
        {
            creature.HasActioned = actioned;
            if (actioned)
            {
                SetGreyout(true);
            }
            else
            {
                SetGreyout(false);
            }
        }
    }
}
