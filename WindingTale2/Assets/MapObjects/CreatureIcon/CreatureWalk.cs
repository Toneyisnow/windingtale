using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using WindingTale.Core.Algorithms;
using WindingTale.Core.Common;
using WindingTale.Core.Objects;
using WindingTale.MapObjects.GameMap;
using WindingTale.Core.Definitions;
using WindingTale.Scenes.GameFieldScene;
using WindingTale.UI.Audio;

namespace WindingTale.MapObjects.CreatureIcon
{
    public class CreatureWalk : MonoBehaviour
    {
        private static float StepLength = 0.08f;

        // The walk is timed, not counted in frames: StepLength used to be added once per
        // frame, so the pace followed the frame rate and a creature crawled whenever frames
        // were slow -- which is how some walks came out visibly slower than others. This is
        // the old pace at 60 frames a second (2.4 tiles a second).
        private static float WalkSpeed = StepLength * 60f;

        // Distance covered along the current segment, and what overshot the last one (carried
        // on, so a path with corners keeps an even pace instead of losing a frame at each).
        private float travelled = 0f;
        private float carried = 0f;

        private FDMovePath movePath = null;

        private Animator animator = null;

        private int count = 0;

        private FDCreature creature = null;

        private Vector3 currentVector = Vector3.zero;
        private Vector3 nextVector = Vector3.zero;

        private int pathIndex = 0;

        private Quaternion desiredRotation = Quaternion.identity;

        private CreatureWalkSound walkSound = null;

        // The tile the walker is over, for the footsteps: the one it is nearer to along the
        // current segment (path vertexes are only the corners).
        private FDPosition soundTile = null;

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
                soundTile = path.Vertexes[0];
                walkSound = CreatureWalkSound.Play(this.transform, GetWalkSurface(soundTile));
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
            if (movePath.Vertexes.Count <= 1)
            {
                // No need to walk, just return
                animator.SetInteger("state", 0);
                Destroy(this);
                return;
            }


            bool reached = TakeStep();
            UpdateWalkSurface();
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

        void OnDestroy()
        {
            if (walkSound != null)
            {
                walkSound.Stop();
            }
        }

        /// <summary>Switches the footsteps when the walker crosses onto a tile of other ground.</summary>
        private void UpdateWalkSurface()
        {
            if (walkSound == null || pathIndex <= 0 || pathIndex >= movePath.Vertexes.Count)
            {
                return;
            }

            FDPosition from = movePath.Vertexes[pathIndex - 1];
            FDPosition to = movePath.Vertexes[pathIndex];
            int dx = to.X - from.X;
            int dy = to.Y - from.Y;
            int steps = Math.Abs(dx) + Math.Abs(dy);
            float length = Vector3.Distance(currentVector, nextVector);
            if (steps == 0 || length <= 0f)
            {
                return;
            }

            int step = Mathf.Clamp(Mathf.RoundToInt(travelled / length * steps), 0, steps);
            FDPosition tile = FDPosition.At(from.X + Math.Sign(dx) * step, from.Y + Math.Sign(dy) * step);
            if (soundTile != null && soundTile.AreSame(tile))
            {
                return;
            }

            soundTile = tile;
            walkSound.SetSurface(GetWalkSurface(tile));
        }

        private WalkSurface GetWalkSurface(FDPosition tile)
        {
            GameMain gameMain = GameMain.getDefault();
            ShapeDefinition shape = gameMain?.gameMap?.Map?.Field?.GetShapeAt(tile);
            return SoundEffects.GetWalkSurface(this.creature.Definition, shape);
        }

        private bool TakeStep()
        {
            float length = Vector3.Distance(currentVector, nextVector);
            travelled += WalkSpeed * Time.deltaTime;

            bool reached = travelled >= length;
            Vector3 nowVector = reached ? nextVector : Vector3.MoveTowards(currentVector, nextVector, travelled);
            this.transform.SetPositionAndRotation(nowVector, desiredRotation);

            if (reached)
            {
                carried = travelled - length;
            }

            return reached;
        }

        private void StartMove(FDPosition curPos, FDPosition nextPos)
        {
            currentVector = MapCoordinate.ConvertCreaturePosToVec3(curPos);
            nextVector = MapCoordinate.ConvertCreaturePosToVec3(nextPos);
            travelled = carried;
            carried = 0f;

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
