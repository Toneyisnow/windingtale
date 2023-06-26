using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WindingTale.Core.Algorithms;
using WindingTale.UI.Common;

public class CreatureWalk : MonoBehaviour
{
    private FDMovePath path = null;

    private int count = 0;


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
        count++;

        if (count > 360)
        {
            // complete
            Debug.Log("Completed walking");

            Vector3 targetVec = MapCoordinate.ConvertPosToVec3(path.Vertexes[path.Vertexes.Count - 1]);
            this.gameObject.transform.SetPositionAndRotation(targetVec, Quaternion.identity);


            this.enabled = false;

            Destroy(this);
        }
    }
}
