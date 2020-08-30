using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WindingTale.UI.Common
{
    public class Clickable : MonoBehaviour
    {
        private Action action = null;

        public Clickable()
        {

        }

        public void Initialize(Action action)
        {
            this.action = action;
        }

        private void OnMouseDown()
        {
            if (Input.GetMouseButtonDown(0))
            {
                if (action != null)
                {
                    action();
                }
            }
        }
    }
}