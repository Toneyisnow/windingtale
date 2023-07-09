using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CreatureDialog : MonoBehaviour
{
    private Action<int> callback;

    // Start is called before the first frame update
    void Start()
    {
    }

    public void Init(Action<int> callback)
    {
        this.callback = callback;
    }

    // Update is called once per frame
    public void OnConfirm()
    {
        this.callback(0);
        Destroy(gameObject);
    }

    public void OnCancel()
    {
        this.callback(-1);
        Destroy(gameObject);
    }

    public void OnConfirmed1()
    {
        this.callback(1);
        Destroy(gameObject);
    }
}
