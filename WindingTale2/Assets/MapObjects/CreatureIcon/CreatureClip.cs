using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WindingTale.MapObjects.CreatureIcon
{
    public class CreatureClip : MonoBehaviour
    {
        // Start is called before the first frame update
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {
            bool enabled = this.gameObject.GetComponent<MeshRenderer>().enabled;
            foreach (MeshRenderer renderer in GetComponentsInChildren<MeshRenderer>())
            {
                renderer.enabled = enabled;
            }
        }
    }
}