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

    private int signX = 0;
    private int signY = 0;
    private int signZ = 0;


    public void Init(FDMovePath path)
    {
        this.path = path;
        this.creature = this.gameObject.GetComponent<Creature>().creature;

        StartMove(creature.Position, path.Vertexes[0]);

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
        bool reached = TakeStep();
        if (reached)
        {
            if (pathIndex < path.Vertexes.Count - 1)
            {
                pathIndex++;
                StartMove(path.Vertexes[pathIndex - 1], path.Vertexes[pathIndex]);
            }
            else
            {
                // complete
                FDPosition finalPos = path.Vertexes[pathIndex];
                Vector3 finalVec = MapCoordinate.ConvertPosToVec3(finalPos);
                this.gameObject.transform.SetPositionAndRotation(finalVec, Quaternion.identity);
                Debug.Log("Creature " + creature.Id + " Completed walking: " + finalPos.X + " " + finalPos.Y);

                this.enabled = false;
                Destroy(this);
            }
        }
    }

    private bool TakeStep()
    {
        stepCount++;
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

        return reached;
    }

    private void StartMove(FDPosition curPos, FDPosition nextPos)
    {
        currentVector = MapCoordinate.ConvertPosToVec3(curPos);
        nextVector = MapCoordinate.ConvertPosToVec3(nextPos);
        stepCount = 0;

        signX = Math.Sign(nextVector.x - currentVector.x);
        signY = Math.Sign(nextVector.y - currentVector.y);
        signZ = Math.Sign(nextVector.z - currentVector.z);

    }

}
