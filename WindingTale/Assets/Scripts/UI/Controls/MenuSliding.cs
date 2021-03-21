using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WindingTale.UI.Controls
{
    public class MenuSliding : MonoBehaviour
    {
        public enum SlidingPhase
        {
            MovingIn = 1,
            Idle = 2,
            MovingOut = 3,
        }

        private Vector3 targetPosition;
        private Vector3 startPosition;

        private static readonly float elapseTime = 0.1f;
        private float startTime = 0;

        private SlidingPhase phase = SlidingPhase.MovingIn;

        private Action closingCallback = null;

        public void Initialize(Vector3 targetPosition)
        {
            this.targetPosition = targetPosition;
            startPosition = this.transform.localPosition;
        }

        public void Close(Action callback = null)
        {
            startTime = Time.time;
            phase = SlidingPhase.MovingOut;
            this.closingCallback = callback;
        }



        // Start is called before the first frame update
        void Start()
        {
            startTime = Time.time;
            phase = SlidingPhase.MovingIn;
        }

        // Update is called once per frame
        void Update()
        {
            if (phase == SlidingPhase.MovingIn)
            {
                transform.position = Vector3.Lerp(startPosition, targetPosition, (Time.time - startTime) / elapseTime);

                if (transform.position == targetPosition)
                {
                    phase = SlidingPhase.Idle;
                }
            }
            else if (phase == SlidingPhase.MovingOut)
            {
                transform.position = Vector3.Lerp(targetPosition, startPosition, (Time.time - startTime) / elapseTime);

                if (transform.position == startPosition)
                {
                    if (closingCallback != null)
                    {
                        closingCallback();
                    }
                }
            }

        }
    }
}