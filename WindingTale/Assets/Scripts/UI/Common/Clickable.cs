using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

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
            Debug.Log(this.gameObject.name + " Was OnMouseDown Clicked.");
            if (Input.GetMouseButtonDown(0))
            {
                if (action != null)
                {
                    action();
                }
            }
        }

        private void OnMouseUpAsButton()
        {
            Debug.Log(this.gameObject.name + " Was OnMouseUpAsButton Clicked.");
            if (Input.GetMouseButtonDown(0))
            {
                if (action != null)
                {
                    action();
                }
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            Debug.Log(this.gameObject.name + " Was OnPointerDown Clicked.");
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