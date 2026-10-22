using UnityEngine;
using WindingTale.Core.Common;
using WindingTale.MapObjects.CreatureIcon;
using WindingTale.MapObjects.GameMap;
using WindingTale.UI.Audio;

namespace WindingTale.Scenes.GameFreeFieldScene
{
    /// <summary>
    /// Walks a creature icon one tile at a time and keeps it turned the way it last went,
    /// unlike CreatureWalk, which follows a whole path and turns the creature back to face
    /// the camera at the end. The pace is CreatureWalk's, 2.4 tiles a second, and so are
    /// the footsteps: the same looping CreatureWalkSound for the ground underfoot, kept
    /// going from one step to the next while the player holds the key.
    /// </summary>
    public class FreeFieldWalker : MonoBehaviour
    {
        private const float WalkSpeed = 0.08f * 60f;

        private Animator animator = null;

        private Vector3 from;
        private Vector3 to;
        private float travelled = 0f;

        private CreatureWalkSound walkSound = null;

        public bool IsWalking { get; private set; }

        public FreeFieldFacing Facing { get; private set; }

        void Awake()
        {
            animator = GetComponent<Animator>();
        }

        public static Quaternion RotationFor(FreeFieldFacing facing)
        {
            switch (facing)
            {
                case FreeFieldFacing.Up: return Quaternion.Euler(0, 180, 0);
                case FreeFieldFacing.Left: return Quaternion.Euler(0, 90, 0);
                case FreeFieldFacing.Right: return Quaternion.Euler(0, 270, 0);
                default: return Quaternion.identity;
            }
        }

        public void Face(FreeFieldFacing facing)
        {
            Facing = facing;
            transform.rotation = RotationFor(facing);
        }

        /// <summary>
        /// Starts the walk onto the next tile; the creature is turned that way first.
        /// <paramref name="surface"/> is the ground of the tile it is walking onto.
        /// </summary>
        public void StepTo(FDPosition position, FreeFieldFacing facing, WalkSurface surface)
        {
            if (walkSound == null)
            {
                walkSound = CreatureWalkSound.Play(transform, surface);
            }
            else
            {
                walkSound.SetSurface(surface);
            }

            Face(facing);
            from = transform.position;
            to = MapCoordinate.ConvertCreaturePosToVec3(position);
            travelled = 0f;
            IsWalking = true;
            SetWalkAnimation(true);
        }

        /// <summary>Back to the idle animation, and the footsteps off, once the player lets go of the keys.</summary>
        public void Stop()
        {
            SetWalkAnimation(false);
            StopWalkSound();
        }

        void OnDestroy()
        {
            StopWalkSound();
        }

        private void StopWalkSound()
        {
            if (walkSound != null)
            {
                walkSound.Stop();
                walkSound = null;
            }
        }

        void Update()
        {
            if (!IsWalking)
            {
                return;
            }

            float length = Vector3.Distance(from, to);
            travelled += WalkSpeed * Time.deltaTime;
            if (travelled >= length)
            {
                transform.position = to;
                IsWalking = false;
                return;
            }

            transform.position = Vector3.MoveTowards(from, to, travelled);
        }

        private void SetWalkAnimation(bool walking)
        {
            if (animator != null)
            {
                animator.SetInteger("state", walking ? 1 : 0);
            }
        }
    }
}
