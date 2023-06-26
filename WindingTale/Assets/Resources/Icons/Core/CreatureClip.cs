using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CreatureClip : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        foreach (MeshRenderer renderer in GetComponentsInChildren<MeshRenderer>())
        {
            renderer.enabled = this.isActiveAndEnabled;
        }
    }
}
