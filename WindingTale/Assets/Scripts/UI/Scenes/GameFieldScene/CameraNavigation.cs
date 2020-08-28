using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WindingTale.UI.Scenes.GameFieldScene
{

    public class CameraNavigation : MonoBehaviour
    {
        public float speed = 2;
        private Vector3 dragOrigin;
        private Vector3 elevateOrigin;


        void Update()
        {
            UpdateDrag();
            UpdateElevate();
        }

        private void UpdateDrag()
        {
            if (Input.GetMouseButtonDown(0))
            {
                dragOrigin = Input.mousePosition;
                return;
            }

            if (!Input.GetMouseButton(0)) return;

            Vector3 pos = Camera.main.ScreenToViewportPoint(Input.mousePosition - dragOrigin);
            Vector3 move = new Vector3(-pos.x * speed, 0, -pos.y * speed);

            transform.Translate(move, Space.World);
        }

        private void UpdateElevate()
        {
            if (Input.GetMouseButtonDown(1))
            {
                elevateOrigin = Input.mousePosition;
                return;
            }

            if (!Input.GetMouseButton(1)) return;

            Vector3 pos = Camera.main.ScreenToViewportPoint(Input.mousePosition - elevateOrigin);
            Vector3 move = new Vector3(-pos.x * speed, -pos.y * speed, 0);

            transform.Translate(move, Space.World);
        }

        // Start is called before the first frame update
        void Start()
        {

        }
    }
}