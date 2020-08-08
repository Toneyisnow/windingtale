using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WindingTale.Common;
using WindingTale.Core.Components;
using WindingTale.Core.ObjectModels;
using WindingTale.UI.FieldMap;
using WindingTale.UI.MapObjects;

namespace WindingTale.UI.Components.Activities
{
    public class MoveCreatureActivity : ActivityBase
    {
        private float moveSpeed = 0.05f;

        private int currentVertex = 0;

        private Vector3 targetPosition = Vector3.zero;


        public int CreatureId
        {
            get; private set;
        }

        public UICreature UICreature
        {
            get; private set;
        }

        public FDMovePath MovePath
        {
            get; private set;
        }

        public MoveCreatureActivity(int creaId, FDMovePath path)
        {
            this.CreatureId = creaId;
            this.MovePath = path;
            currentVertex = 0;
        }

        public override void Start(IGameInterface gameInterface)
        {
            this.UICreature = gameInterface.GetUICreature(this.CreatureId);
            if (this.UICreature == null)
            {
                this.HasFinished = true;
                return;
            }

            if (this.MovePath.Vertexes.Count == 0)
            {
                this.HasFinished = true;
                return;
            }
            
            currentVertex = -1;

        }

        public override void Update(IGameInterface gameInterface)
        {
            if (IsCurrentMoveDone())
            {
                // Pick the next vertex, or finish
                currentVertex++;
                if (currentVertex < this.MovePath.Vertexes.Count)
                {
                    var targetVertex = this.MovePath.Vertexes[currentVertex];
                    targetPosition = FieldTransform.GetCreaturePixelPosition(targetVertex);

                    FDPosition current = this.UICreature.GetCurrentPosition();
                    if (targetVertex.X > current.X)
                    {
                        this.UICreature.SetAnimateState(UICreature.AnimateStates.WalkRight);
                    }
                    else if(targetVertex.X < current.X)
                    {
                        this.UICreature.SetAnimateState(UICreature.AnimateStates.WalkLeft);
                    }
                    else if (targetVertex.Y > current.Y)
                    {
                        this.UICreature.SetAnimateState(UICreature.AnimateStates.WalkDown);
                    }
                    else if (targetVertex.Y < current.Y)
                    {
                        this.UICreature.SetAnimateState(UICreature.AnimateStates.WalkRight);
                    }


                }
                else
                {
                    this.UICreature.SetAnimateState(UICreature.AnimateStates.Idle);
                    this.HasFinished = true;
                }

                return;
            }

            DoMoving();
        }

        private bool IsCurrentMoveDone()
        {
            if (this.UICreature.AnimateState == UICreature.AnimateStates.Idle)
            {
                return true;
            }

            Vector3 currentPosition = this.UICreature.GetCurrentPixelPosition();
            
            if (this.UICreature.AnimateState == UICreature.AnimateStates.WalkDown
                && currentPosition.z <= targetPosition.z)
            {
                return true;
            }

            if (this.UICreature.AnimateState == UICreature.AnimateStates.WalkUp
                && currentPosition.z >= targetPosition.z)
            {
                return true;
            }

            if (this.UICreature.AnimateState == UICreature.AnimateStates.WalkLeft
                && currentPosition.x <= targetPosition.x)
            {
                return true;
            }

            if (this.UICreature.AnimateState == UICreature.AnimateStates.WalkRight
                && currentPosition.x >= targetPosition.x)
            {
                return true;
            }

            return false;
        }

        private void DoMoving()
        {
            Vector3 delta = Vector3.zero;
            switch(this.UICreature.AnimateState)
            {
                case UICreature.AnimateStates.WalkUp:
                    delta = new Vector3(0, 0, moveSpeed);
                    break;
                case UICreature.AnimateStates.WalkDown:
                    delta = new Vector3(0, 0, - moveSpeed);
                    break;
                case UICreature.AnimateStates.WalkLeft:
                    delta = new Vector3(- moveSpeed, 0, 0);
                    break;
                case UICreature.AnimateStates.WalkRight:
                    delta = new Vector3(moveSpeed, 0, 0);
                    break;
                case UICreature.AnimateStates.Idle:
                    break;
                default:
                    break;
            }

            Vector3 position = this.UICreature.GetCurrentPixelPosition();
            this.UICreature.SetPixelPosition(position + delta);

        }
    }
}