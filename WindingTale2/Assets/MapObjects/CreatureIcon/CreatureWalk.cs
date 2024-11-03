using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using WindingTale.Core.Algorithms;
using WindingTale.Core.Common;
using WindingTale.Core.Objects;
using WindingTale.MapObjects.GameMap;

namespace WindingTale.MapObjects.CreatureIcon
{

    public class CreatureWalk : MonoBehaviour
    {
        private static float StepLength = 0.06f;

        private FDMovePath movePath = null;

        private Animator animator = null;

        private int count = 0;

        private FDCreature creature = null;

        private Vector3 currentVector = Vector3.zero;
        private Vector3 nextVector = Vector3.zero;

        private int pathIndex = 0;
        private int stepCount = 0;

        private int signX = 0;
        private int signY = 0;
        private int signZ = 0;

        private Quaternion desiredRotation = Quaternion.identity;

        public void Init(FDMovePath path)
        {
            this.movePath = path;
            this.creature = this.gameObject.GetComponent<Creature>().creature;
            animator = gameObject.GetComponent<Animator>();

            this.currentVector = gameObject.transform.position;
            if (path.Vertexes.Count > 1)
            {
                pathIndex = 1;
                StartMove(path.Vertexes[0], path.Vertexes[1]);
            }
        }

        // Start is called before the first frame update
        void Start()
        {
            if (animator != null && movePath.Vertexes.Count > 0)
            {
                animator.SetInteger("state", 1);
            }
        }

        // Update is called once per frame
        void Update()
        {
            if (movePath.Vertexes.Count == 0)
            {
                // No need to walk, just return
                animator.SetInteger("state", 0);
                Destroy(this);
                return;
            }


            bool reached = TakeStep();
            if (reached)
            {
                if (pathIndex < movePath.Vertexes.Count - 1)
                {
                    //// this.gameObject.transform.SetPositionAndRotation(MapCoordinate.ConvertCreaturePosToVec3(movePath.Vertexes[pathIndex]), desiredRotation);

                    pathIndex++;
                    StartMove(movePath.Vertexes[pathIndex - 1], movePath.Vertexes[pathIndex]);
                }
                else
                {
                    // complete
                    FDPosition finalPos = movePath.Vertexes[pathIndex];
                    Vector3 finalVec = MapCoordinate.ConvertCreaturePosToVec3(finalPos);

                    this.enabled = false;
                    desiredRotation = Quaternion.Euler(0, 0, 0);
                    this.gameObject.transform.SetPositionAndRotation(MapCoordinate.ConvertCreaturePosToVec3(movePath.Vertexes[pathIndex]), desiredRotation);

                    animator.SetInteger("state", 0);
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
           
            ///transform.rotation = Quaternion.RotateTowards(transform.rotation, desiredRotation, 1.5f);
            this.transform.SetPositionAndRotation(nowVector, desiredRotation);
            //// this.transform.position = nowVector;

            // Check if reached the next position
            bool reached = false;
            if (signX != 0 && (nowVector.x == nextVector.x || Math.Sign(nowVector.x - nextVector.x) * signX > 0))
            {
                reached = true;
            }
            if (signY != 0 && (nowVector.y == nextVector.y || Math.Sign(nowVector.y - nextVector.y) * signY > 0))
            {
                reached = true;
            }
            if (signZ != 0 && (nowVector.z == nextVector.z || Math.Sign(nowVector.z - nextVector.z) * signZ > 0))
            {
                reached = true;
            }

            return reached;
        }

        private void StartMove(FDPosition curPos, FDPosition nextPos)
        {
            currentVector = MapCoordinate.ConvertCreaturePosToVec3(curPos);
            nextVector = MapCoordinate.ConvertCreaturePosToVec3(nextPos);
            stepCount = 0;

            signX = Math.Sign(nextVector.x - currentVector.x);
            signY = Math.Sign(nextVector.y - currentVector.y);
            signZ = Math.Sign(nextVector.z - currentVector.z);

            //Vector3 relativeVect = nextVector - currentVector;
            //Quaternion desiredRotation = Quaternion.LookRotation(Vector3.forward, relativeVect);
            //desiredRotation = Quaternion.Euler(0, desiredRotation.eulerAngles.y + 90, 0);

            if (nextPos.Y < curPos.Y)
            {
                TurnUp();
            }
            else if (nextPos.Y > curPos.Y)
            {
                TurnDown();
            }
            else if (nextPos.X > curPos.X)
            {
                TurnRight();
            }
            else if (nextPos.X < curPos.X)
            {
                TurnLeft();
            }
        }

        private void TurnUp()
        {
            desiredRotation = Quaternion.Euler(0, 180, 0);
        }

        private void TurnDown()
        {
            desiredRotation = Quaternion.Euler(0, 0, 0);
        }

        private void TurnLeft()
        {
            desiredRotation = Quaternion.Euler(0, 90, 0);

        }

        private void TurnRight()
        {
            desiredRotation = Quaternion.Euler(0, 270, 0);
        }

    }
}
