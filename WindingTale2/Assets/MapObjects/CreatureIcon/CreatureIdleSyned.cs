using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace WindingTale.MapObjects.CreatureIcon
{

    public class CreatureIdleSynced : MonoBehaviour
    {
        public int Speed = 30;

        private GameObject clip01 = null;
        private GameObject clip02 = null;
        private GameObject clip03 = null;

        private 
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

            int timeDouble = (int)(Time.fixedTime * 100) / this.Speed;
            int frame = timeDouble % 4;

            Animator animator = this.transform.GetComponent<Animator>();


                if (animator != null && animator.GetInteger("state") == 0)
                {
                clip01.GetComponent<MeshRenderer>().enabled = (frame == 0);
                clip02.GetComponent<MeshRenderer>().enabled = (frame == 1 || frame == 3);
                clip03.GetComponent<MeshRenderer>().enabled = (frame == 2);
                }
                else
                {
                clip01.GetComponent<MeshRenderer>().enabled = true;
                clip02.GetComponent<MeshRenderer>().enabled = true;
                clip03.GetComponent<MeshRenderer>().enabled = true;
                }
        }
    }
}