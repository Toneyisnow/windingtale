using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public interface IFlightAnimation
{
    void StartAttack();

    void StartKnocked();

    void OnAttackCompleteCallback(Action callback);

    void OnAttackHittingCallback(Action<int> callback);



}

public class FightAniEvent : MonoBehaviour, IFlightAnimation
{
    private Action<int> onAttackHitting;
    private Action onAttackComplete;

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
        
        Invoke("TestChangeAnimation", 1.5f);
        
        animator = this.GetComponent<Animator>();
        if (animator == null)
        {
            throw new MissingComponentException("animator is missing");
        }

        /// var clip = UnityEditor.Animations.AnimationClip.CreateAnimatorControllerAtPath("Assets/Resources/Fights/001/auto001.controller");


        AnimationClip clip = animator.runtimeAnimatorController.animationClips[1];

        AnimationEvent evt = new AnimationEvent();
        evt.intParameter = 99;
        evt.time = 0.3f;
        evt.functionName = "OnAttackHitting";

        clip.AddEvent(evt);

        AnimationEvent evt2 = new AnimationEvent();
        evt2.time = clip.length;
        evt2.functionName = "OnAttackComplete";
        
        clip.AddEvent(evt2);
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

    private void TestChangeAnimation()
    {
        SetActionState(ActionState.Attack);
    }

}
