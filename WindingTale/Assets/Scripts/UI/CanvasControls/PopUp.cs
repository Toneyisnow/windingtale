using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PopUp : MonoBehaviour
{
    public enum PoppingPhase
    {
        Opening = 1,
        Idle = 2,
        Closing = 3,
    }

    private static readonly float elapseTime = 0.2f;

    private Vector3 targetPosition;
    private Vector3 startPosition;

    private Vector3 targetScale;
    private Vector3 startScale = new Vector3(0.05f, 0.05f, 0.05f);

    private PoppingPhase phase = PoppingPhase.Opening;

    private float startTime = 0;

    private Action closingCallback = null;

    public void Initialize(Vector2 popupPosition)
    {
        startPosition = popupPosition;
        /// startPosition = new Vector3(this.transform.localPosition.x, this.transform.localPosition.y + 3, 0);
        targetPosition = this.transform.localPosition;
        targetScale = this.transform.localScale;

        Debug.Log("PopUp.Initialize: startPosition=" + startPosition + ", targetPosition=" + targetPosition);
    }

    public void Close(Action callback)
    {
        startTime = Time.time;
        closingCallback = callback;
        phase = PoppingPhase.Closing;
    }

    // Start is called before the first frame update
    void Start()
    {
        if (startPosition == Vector3.zero)
        {
            phase = PoppingPhase.Idle;
        }
        else
        {
            phase = PoppingPhase.Opening;
        }

        transform.localScale = startScale;
        transform.localPosition = startPosition;

        startTime = Time.time;
        
    }

    // Update is called once per frame
    void Update()
    {
        if (phase == PoppingPhase.Opening)
        {
            float t = (Time.time - startTime) / elapseTime;
            transform.localPosition = Vector3.Lerp(startPosition, targetPosition, t);
            transform.localScale = Vector3.Lerp(startScale, targetScale, t);

            if (t >= 1)
            {
                phase = PoppingPhase.Idle;
            }
        }
        else if (phase == PoppingPhase.Closing)
        {
            float t = (Time.time - startTime) / elapseTime;
            transform.localPosition = Vector3.Lerp(targetPosition, startPosition, t);
            transform.localScale = Vector3.Lerp(targetScale, startScale, t);

            if (t >= 1)
            {
                if (closingCallback != null)
                {
                    closingCallback();
                }
            }
        }
    }
}
