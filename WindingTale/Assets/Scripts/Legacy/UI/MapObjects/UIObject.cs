using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WindingTale.UI.MapObjects
{
    public abstract class UIObject : MonoBehaviour
    {
        // Start is called before the first frame update
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {

        }


        private void OnMouseOver()
        {
            if (Input.GetMouseButtonDown(0))
            {
                this.OnTouched();
            }
        }

        protected virtual void OnTouched()
        {


        }
    }
}
