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
        private float rotateSpeed = 1.0f;

        private Vector3 rotationDirection = new Vector3(1, 0, 0);

        private int count = 0;

        // Start is called before the first frame update
        void Start()
        {
            count = 1;
        }

        // Update is called once per frame
        void Update()
        {
            transform.Rotate(rotateSpeed * rotationDirection * Time.deltaTime);

        }

    }
}