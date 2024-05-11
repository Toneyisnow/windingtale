using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace WindingTale.MapObjects.CreatureIcon
{

    public class CreatureSynced : MonoBehaviour
    {
        public GameObject mapObject = null;

        private GameObject clip01 = null;
        private GameObject clip02 = null;
        private GameObject clip03 = null;


        // Start is called before the first frame update
        void Start()
        {
            clip01 = this.transform.Find("Clip_01").gameObject;
            clip02 = this.transform.Find("Clip_02").gameObject;
            clip03 = this.transform.Find("Clip_03").gameObject;
        }

        // Update is called once per frame
        void Update()
        {
            Creature[] creatures = mapObject.transform.GetComponentsInChildren<Creature>();

            foreach (Creature c in creatures)
            {
                Animator animator = c.GetComponent<Animator>();

                GameObject c1 = c.transform.Find("Clip_01").gameObject;
                GameObject c2 = c.transform.Find("Clip_02").gameObject;
                GameObject c3 = c.transform.Find("Clip_03").gameObject;

                if (animator != null && animator.GetInteger("state") == 0)
                {
                    c1.GetComponent<MeshRenderer>().enabled = clip01.activeSelf;
                    c2.GetComponent<MeshRenderer>().enabled = clip02.activeSelf;
                    c3.GetComponent<MeshRenderer>().enabled = clip03.activeSelf;
                }
                else
                {
                    c1.GetComponent<MeshRenderer>().enabled = true;
                    c2.GetComponent<MeshRenderer>().enabled = true;
                    c3.GetComponent<MeshRenderer>().enabled = true;
                }
            }
        }
    }
}