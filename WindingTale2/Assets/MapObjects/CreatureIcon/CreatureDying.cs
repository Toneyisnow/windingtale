using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using WindingTale.Core.Algorithms;
using WindingTale.Core.Common;
using WindingTale.Core.Objects;


namespace WindingTale.MapObjects.CreatureIcon
{
    public class CreatureDying : MonoBehaviour
    {
        private float rotateSpeed = 180.0f;

        private Vector3 rotationDirection = new Vector3(0, 2, 0);

        private int count = 0;

        private DateTime startTime;

        // Start is called before the first frame update
        void Start()
        {
            count = 1;
            startTime = DateTime.Now;
        }

        // Update is called once per frame
        void Update()
        {
            Debug.Log(" ====== CreatureDying rotate ====== ");
            transform.Rotate(rotateSpeed * rotationDirection * Time.deltaTime);

            var now = DateTime.Now;
            if ((now - startTime).TotalMilliseconds > 3500)
            {
                Destroy(this);
            }
        }

    }
}