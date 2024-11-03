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
                GameRenderer.Instance.ApplyDefaultGreyMaterial(obj1);
                GameRenderer.Instance.ApplyDefaultGreyMaterial(obj2);
                GameRenderer.Instance.ApplyDefaultGreyMaterial(obj3);
            }
            else
            {
                GameRenderer.Instance.ApplyDefaultMaterial(obj1);
                GameRenderer.Instance.ApplyDefaultMaterial(obj2);
                GameRenderer.Instance.ApplyDefaultMaterial(obj3);
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
