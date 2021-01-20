using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FightAniEvent : MonoBehaviour
{
    public enum ActionState
    {
        Idle = 0,
        Attack = 1,
        Skill = 2,
        Knocked = 11

    }

    private Animator animator = null;

    
    private void Start()
    {
        Invoke("TestChangeAnimation", 1.5f);

        animator = this.GetComponent<Animator>();
        if (animator == null)
        {
            throw new MissingComponentException("animator is missing");
        }

        /// var clip = UnityEditor.Animations.AnimationClip.CreateAnimatorControllerAtPath("Assets/Resources/Fights/001/auto001.controller");

        Animation anim = this.gameObject.AddComponent<Animation>();
        //// anim.clip


    }

    public void OnAttackHitting(int value)
    {
        Debug.Log("Hitting for " + value + " points.");
    }

    public void OnAttackComplete()
    {
        SetActionState(ActionState.Idle);
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
