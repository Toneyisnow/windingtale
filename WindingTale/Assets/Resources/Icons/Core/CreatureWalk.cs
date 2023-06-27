using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using WindingTale.Core.Algorithms;
using WindingTale.Core.Common;
using WindingTale.Core.Objects;
using WindingTale.UI.Common;

public class CreatureWalk : MonoBehaviour
{
    private static float StepLength = 0.03f;

    private FDMovePath path = null;

    private int count = 0;

    private FDCreature creature = null;

    private Vector3 currentVector = Vector3.zero;
    private Vector3 nextVector = Vector3.zero;

    private int pathIndex = 0;
    private int stepCount = 0;


    public void Init(FDMovePath path)
    {
        this.path = path;
        this.creature = this.gameObject.GetComponent<Creature>().creature;

        currentVector = MapCoordinate.ConvertPosToVec3(creature.Position);
        FDPosition nextPos = path.Vertexes[pathIndex];
        nextVector = MapCoordinate.ConvertPosToVec3(nextPos);

        Debug.Log("Creature " + creature.Id + " Init vector3:" + currentVector.ToString());
    }

    // Start is called before the first frame update
    void Start()
    {
        Debug.Log("Start walking");
    }

    // Update is called once per frame
    void Update()
    {
        stepCount++;

        float signX = System.Math.Sign(nextVector.x - currentVector.x);
        float signY = System.Math.Sign(nextVector.y - currentVector.y);
        float signZ = System.Math.Sign(nextVector.z - currentVector.z);

        float deltaX = signX * StepLength * stepCount;
        float deltaY = signY * StepLength * stepCount;
        float deltaZ = signZ * StepLength * stepCount;

        Vector3 nowVector = new Vector3(currentVector.x + deltaX, currentVector.y + deltaY, currentVector.z + deltaZ);
        Debug.Log("currentVector: " + currentVector.ToString() + " nextVector: " + nextVector.ToString());
        Debug.Log("signs: " + signX + signY + signZ);

        this.gameObject.transform.SetPositionAndRotation(nowVector, Quaternion.identity);

        // Check if reached the next position
        bool reached = false;
        if (signX != 0 && (nowVector.x == nextVector.x || System.Math.Sign(nowVector.x - nextVector.x) * signX > 0))
        {
            reached = true;
        }
        if (signY != 0 && (nowVector.y == nextVector.y || System.Math.Sign(nowVector.y - nextVector.y) * signY > 0))
        {
            reached = true;
        }

        if (signZ != 0 && (nowVector.z == nextVector.z || System.Math.Sign(nowVector.z - nextVector.z) * signZ > 0))
        {
            reached = true;
        }


        if (reached)
        {
            // complete
            FDPosition targetPos = path.Vertexes[pathIndex];
            Vector3 targetVec = MapCoordinate.ConvertPosToVec3(targetPos);
            this.gameObject.transform.SetPositionAndRotation(targetVec, Quaternion.identity);
            Debug.Log("Creature " + creature.Id + " Completed walking: " + targetPos.X + " " + targetPos.Y);

            this.enabled = false;
            Destroy(this);
        }
    }
}
