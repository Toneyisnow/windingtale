using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WindingTale.Core.Definitions;

namespace WindingTale.UI.Battle
{
    public interface IFightAnimator
    {
        void Initialize(int animationId);

        void StartAttack();

        void StartKnocked();

        void OnAttackCompleteCallback(Action callback);

        void OnAttackHittingCallback(Action<int> callback);

    }

    public class FightAnimator : MonoBehaviour, IFightAnimator
    {
        private Action<int> onAttackHitting;
        private Action onAttackComplete;

        private int animationId = 0;

        public enum ActionState
        {
            Idle = 0,
            Attack = 1,
            Skill = 2,
            Knocked = 11

        }

        private Animator animator = null;


        #region Interface

        public void StartAttack()
        {
            SetActionState(ActionState.Attack);
        }

        public void StartKnocked()
        {
            SetActionState(ActionState.Knocked);
        }

        public void OnAttackCompleteCallback(Action callback)
        {
            this.onAttackComplete = callback;
        }

        public void OnAttackHittingCallback(Action<int> callback)
        {
            this.onAttackHitting = callback;
        }


        #endregion

        void Start()
        {
            Debug.Log("FightAniEvent Start.");

            // Invoke("TestChangeAnimation", 1.5f);

            animator = this.gameObject.GetComponent<Animator>();
            if (animator == null)
            {
                throw new MissingComponentException("animator is missing");
            }

            /// var clip = UnityEditor.Animations.AnimationClip.CreateAnimatorControllerAtPath("Assets/Resources/Fights/001/auto001.controller");
            FightAnimation fightAnimation = DefinitionStore.Instance.GetFightAnimation(animationId);

            AnimationClip idleClip = animator.runtimeAnimatorController.animationClips[0];
            idleClip.wrapMode = WrapMode.Loop;

            AnimationClip attackClip = animator.runtimeAnimatorController.animationClips[1];

            foreach (int frame in fightAnimation.AttackPercentageByFrame.Keys)
            {
                int percentage = fightAnimation.AttackPercentageByFrame[frame];
                if (percentage < 0 || percentage > 100)
                {
                    continue;
                }

                AnimationEvent evt = new AnimationEvent();
                evt.intParameter = percentage;
                evt.time = 0.15f * frame;
                evt.functionName = "OnAttackHitting";
                attackClip.AddEvent(evt);
            }

            AnimationEvent evt2 = new AnimationEvent();
            evt2.time = attackClip.length;
            evt2.functionName = "OnAttackComplete";
            attackClip.AddEvent(evt2);
        }

        public void Initialize(int animationId)
        {
            this.animationId = animationId;
        }

        public void OnAttackHitting(int value)
        {
            Debug.Log("Hitting for " + value + " points.");

            if (onAttackHitting != null)
            {
                onAttackHitting(value);
            }
        }

        public void OnAttackComplete()
        {
            SetActionState(ActionState.Idle);

            if (this.onAttackComplete != null)
            {
                this.onAttackComplete();
            }
        }

        public void OnKnock()
        {
            SetActionState(ActionState.Knocked);
        }

        public void OnKnockComplete()
        {
            SetActionState(ActionState.Idle);
        }

        private void SetActionState(ActionState state)
        {
            animator.SetInteger("actionState", state.GetHashCode());

        }
    }
}