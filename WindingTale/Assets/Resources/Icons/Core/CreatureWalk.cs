using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WindingTale.Core.Algorithms;

public class CreatureWalk : MonoBehaviour
{
    private FDMovePath path = null;

    public void Init(FDMovePath path)
    {
        this.path = path;
    }

    // Start is called before the first frame update
    void Start()
    {
        Debug.Log("Start walking");
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
