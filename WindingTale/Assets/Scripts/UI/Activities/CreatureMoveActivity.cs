using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WindingTale.Core.Algorithms;
using WindingTale.Core.Common;
using WindingTale.Core.Objects;
using WindingTale.UI.Scenes;
using WindingTale.UI.Scenes.Game;

namespace WindingTale.UI.Activities
{
    public class CreatureMoveActivity : ActivityBase
    {
        public static float CreatureMoveSpeed = 0.2f;

        protected FDCreature creature;

        protected FDMovePath movePath;

        protected GameObject creatureUI;


        private int targetPosIndex = 0;


        public CreatureMoveActivity(FDCreature creature, FDMovePath movePath)
        {
            this.creature = creature;
            this.movePath = movePath;

            this.targetPosIndex = 0;
        }

        public override void Start(GameObject gameInterface)
        {
            GameObject mapNode = gameInterface.GetComponent<GameFieldScene>().mapNode;

            string creatureName = string.Format("creature_{0}", StringUtils.Digit3(creature.Id));
            creatureUI = mapNode.transform.Find(creatureName).gameObject;

            CreatureWalk walk = creatureUI.AddComponent<CreatureWalk>();
            walk.Init(movePath);
        }

        public override void Update(GameObject gameInterface)
        {
            CreatureWalk walk = creatureUI.GetComponent<CreatureWalk>();
            if(walk == null || !walk.enabled)
            {
                this.HasFinished = true;

                // Update the object to the final position
                Creature creatureObj = creatureUI.GetComponent<Creature>();
                creatureObj.creature.Position = movePath.Vertexes[movePath.Vertexes.Count - 1];
            }
        }

    }
}

        /*

        private float moveDeltaPerTick = 1.0f / Common.Constants.TICK_PER_FRAME;

        private int currentVertex = 0;

        private Vector3 targetPosition = Vector3.zero;


        public FDCreature Creature
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

        public CreatureMoveActivity(FDCreature creature, FDMovePath path)
        {
            this.Creature = creature;
            this.MovePath = path;
            currentVertex = 0;
        }

        public override void Start(IGameInterface gameInterface)
        {
            this.UICreature = gameInterface.GetUICreature(this.Creature.Id);
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
                // Set precise position for the UI
                if (currentVertex >= 0)
                {
                    var vertex = this.MovePath.Vertexes[currentVertex];
                    this.UICreature.SetPixelPosition(FieldTransform.GetCreaturePixelPosition(vertex));
                }

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
                        this.UICreature.SetAnimateState(UICreature.AnimateStates.WalkUp);
                    }
                    else
                    {
                        // Make the status to done so that next Update will pick the next vertex
                        this.UICreature.SetAnimateState(UICreature.AnimateStates.Idle);
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
                    delta = new Vector3(0, 0, moveDeltaPerTick);
                    break;
                case UICreature.AnimateStates.WalkDown:
                    delta = new Vector3(0, 0, - moveDeltaPerTick);
                    break;
                case UICreature.AnimateStates.WalkLeft:
                    delta = new Vector3(- moveDeltaPerTick, 0, 0);
                    break;
                case UICreature.AnimateStates.WalkRight:
                    delta = new Vector3(moveDeltaPerTick, 0, 0);
                    break;
                case UICreature.AnimateStates.Idle:
                    break;
                default:
                    break;
            }

            Vector3 position = this.UICreature.GetCurrentPixelPosition();
            this.UICreature.SetPixelPosition(position + delta);

        }

        private void DoMoving2()
        {
            Vector3 delta = Vector3.zero;
            switch (this.UICreature.AnimateState)
            {
                case UICreature.AnimateStates.WalkUp:
                    delta = new Vector3(0, 0, moveDeltaPerTick);
                    break;
                case UICreature.AnimateStates.WalkDown:
                    delta = new Vector3(0, 0, -moveDeltaPerTick);
                    break;
                case UICreature.AnimateStates.WalkLeft:
                    delta = new Vector3(-moveDeltaPerTick, 0, 0);
                    break;
                case UICreature.AnimateStates.WalkRight:
                    delta = new Vector3(moveDeltaPerTick, 0, 0);
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
        */