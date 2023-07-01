using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SyncedTick : MonoBehaviour
{
    private int tick = 0;

    public int GetTick()
    {
        return tick;
    }

    // Start is called before the first frame update
    void Start()
    {
        tick = 0;
    }

    // Update is called once per frame
    void Update()
    {
        tick++;
    }
}
