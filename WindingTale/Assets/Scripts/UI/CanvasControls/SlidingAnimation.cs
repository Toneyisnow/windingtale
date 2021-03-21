using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SlidingAnimation : MonoBehaviour
{
    public enum SlidingDirection
    {
        Up,
        Down,
        Left,
        Right,
    }

    public enum SlidingPhase
    {
        MovingIn = 1,
        Idle = 2,
        MovingOut = 3,
    }

    public SlidingDirection direction;
    
    private Vector3 targetPosition;
    private Vector3 startPosition;

    private float elapseTime = 0.2f;
    private float startTime = 0;

    private SlidingPhase phase = SlidingPhase.Idle;

    private Action closingCallback = null;

    public void Close(Action callback = null)
    {
        phase = SlidingPhase.MovingOut;
        startTime = Time.time;
        this.closingCallback = callback;
    }

    // Start is called before the first frame update
    void Start()
    {
        targetPosition = this.transform.position;
        switch (direction)
        {
            case SlidingDirection.Left:
                startPosition = new Vector3(targetPosition.x - 5, targetPosition.y, targetPosition.z);
                break;
            case SlidingDirection.Right:
                startPosition = new Vector3(targetPosition.x + 12, targetPosition.y, targetPosition.z);
                break;
            case SlidingDirection.Down:
                startPosition = new Vector3(targetPosition.x, targetPosition.y - 10, targetPosition.z);
                break;

            default:
                break;
        }

        this.transform.position = startPosition;

        startTime = Time.time;
        phase = SlidingPhase.MovingIn;
    }

    // Update is called once per frame
    void Update()
    {
        if (phase == SlidingPhase.MovingIn)
        {
            transform.position = Vector3.Lerp(startPosition, targetPosition, (Time.time - startTime) / elapseTime);
            //// transform.position = targetPosition;

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
