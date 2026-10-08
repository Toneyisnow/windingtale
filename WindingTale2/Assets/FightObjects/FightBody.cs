using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using WindingTale.Core.Common;
using WindingTale.Core.Definitions;
using WindingTale.UI.Utils;
using WindingTale.UI.Audio;

namespace WindingTale.FightObjects
{
    public class FightBody : MonoBehaviour
    {
        private int animationId;
        private FightAnimation fightAnimation;
        private List<int> animationHitPoints = null;
        private int animationHitIndex = 0;

        private Action<int> onHit = null;
        private Action onFinish = null;

        // The wind-up sound plays once per attack, when the clip reaches its second frame.
        // It re-arms only once the animator has left the attack state, so a double attack
        // (the clip played again) winds up twice but a clip lingering on its last frame does not.
        private const int WindUpFrame = 1;
        private bool windUpPlayed = false;

        // Start is called before the first frame update
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {
            if (fightAnimation == null || fightAnimation.AttackFrameCount <= WindUpFrame)
            {
                return;
            }

            Animator animator = this.GetComponent<Animator>();
            if (animator == null || animator.runtimeAnimatorController == null)
            {
                return;
            }

            AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
            if (!state.IsName("attack"))
            {
                windUpPlayed = false;
                return;
            }

            if (!windUpPlayed && state.normalizedTime * fightAnimation.AttackFrameCount >= WindUpFrame)
            {
                windUpPlayed = true;
                SoundEffects.PlayBattleWindUp(animationId);
            }
        }

        public void Initialize(int animationId, Action<int> onHit, Action onFinish)
        {
            this.animationId = animationId;
            this.fightAnimation = DefinitionStore.Instance.GetFightAnimation(animationId);
            this.animationHitPoints = this.fightAnimation.AttackPercentageByFrame.Values.ToList().FindAll(val => val > 0);
            this.animationHitIndex = 0;

            this.onHit = onHit;
            this.onFinish = onFinish;

            // Show the 3D models built from this animation's frames, when there are any.
            FightModel3D.Attach(gameObject, animationId);
        }

        /// <summary>
        /// Callback function
        /// </summary>
        public void onAttackHit()
        {
            if (this.animationHitIndex >= this.animationHitPoints.Count)
            {
                return;
            }

            var hitPoint = this.animationHitPoints[this.animationHitIndex++];

            Debug.Log("=== onAttackHit === " + hitPoint.ToString());

            this.onHit?.Invoke(hitPoint);

        }

        /// <summary>
        /// Callback function
        /// </summary>
        public void onAttackFinish()
        {
            Debug.Log("=== onAttackFinish ===");

            // A double attack plays the same clip again: its hit points start over, or the
            // second strike's hits are all swallowed and the HP bar never moves.
            this.animationHitIndex = 0;

            Animator animator = this.GetComponent<Animator>();

            MonoBehaviourUtils.ExecuteWithDelay(this, 0.1f, () =>
            {
                animator.SetInteger("actionState", 0);
            });

            this.onFinish?.Invoke();
        }

        /// <summary>
        /// Seems not working.
        /// </summary>
        private void LoadAnimationEvents()
        {
            Animator animator = this.GetComponent<Animator>();

            animator.fireEvents = true;

            // attack animation
            var attackEvents = animator.runtimeAnimatorController.animationClips[1].events;
            if (attackEvents != null && attackEvents.Length > 0)
            {
                // Last event is the finish of the attack
                var hitCount = attackEvents.Length - 1;
                for (int i = 0; i < hitCount; i++)
                {
                    attackEvents[i].intParameter = 99;
                    attackEvents[i].functionName = "FightBody/Methods/onAttackHit()";
                }

                attackEvents[attackEvents.Length - 1].functionName = "FightBody/Methods/onAttackFinish()";
            }
        }


    }
}