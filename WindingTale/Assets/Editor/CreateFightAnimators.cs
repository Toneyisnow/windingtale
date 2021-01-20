using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;

public class CreateFightAnimators : MonoBehaviour
{
    [MenuItem("Assets/Create Fight Animators")]
    static void CreateFightAnimator()
    {
        // Creates the controller
        var controller = UnityEditor.Animations.AnimatorController.CreateAnimatorControllerAtPath("Assets/Resources/Fights/001/auto001.controller");



        // Add parameters
        controller.AddParameter("actionState", AnimatorControllerParameterType.Int);

        // Add StateMachines
        var rootStateMachine = controller.layers[0].stateMachine;

        // Add States
        var stateIdle = rootStateMachine.AddState("idle");
        var stateAttack = rootStateMachine.AddState("attack");
        var stateSkill = rootStateMachine.AddState("skill");
        var stateC2 = rootStateMachine.AddState("stateC2"); // don’t add an entry transition, should entry to state by default


        // Add Transitions
        var exitTransition = stateIdle.AddExitTransition();
        exitTransition.AddCondition(UnityEditor.Animations.AnimatorConditionMode.If, 99, "actionState");
        exitTransition.duration = 0;

        var resetTransition = rootStateMachine.AddAnyStateTransition(stateAttack);
        resetTransition.AddCondition(UnityEditor.Animations.AnimatorConditionMode.If, 1, "actionState");
        resetTransition.duration = 0;


        

        AnimationClip clip = new AnimationClip();


        clip.SetCurve("", typeof(Transform), "position.x", AnimationCurve.EaseInOut(0, 0, 2, 10));
        clip.SetCurve("", typeof(Transform), "position.y", AnimationCurve.EaseInOut(0, 10, 2, 0));
        clip.SetCurve("", typeof(Transform), "position.z", AnimationCurve.EaseInOut(0, 5, 2, 2));

        AssetDatabase.CreateAsset(clip, "Assets/Resources/Fights/001/auto001.anim");

    }


    [MenuItem("Assets/Create Fight Animation Clip")]
    static void CreateFlightAnimationClip()
    {
        Sprite[] _sprites;

        Sprite sprite = Resources.Load<Sprite>("Fights/001/act1/Fight-001-1-01");

        AnimationClip animClip = new AnimationClip();
        // First you need to create e Editor Curve Binding
        EditorCurveBinding curveBinding = new EditorCurveBinding();

        // I want to change the sprites of the sprite renderer, so I put the typeof(SpriteRenderer) as the binding type.
        curveBinding.type = typeof(SpriteRenderer);
        // Regular path to the gameobject that will be changed (empty string means root)
        curveBinding.path = "";
        // This is the property name to change the sprite of a sprite renderer
        curveBinding.propertyName = "m_Sprite";

        // An array to hold the object keyframes
        ObjectReferenceKeyframe[] keyFrames = new ObjectReferenceKeyframe[10];
        for (int i = 0; i < 1; i++)
        {
            keyFrames[i] = new ObjectReferenceKeyframe();
            // set the time
            keyFrames[i].time = i;
            // set reference for the sprite you want
            keyFrames[i].value = sprite;
            Debug.LogWarning(sprite);
        }
        AnimationUtility.SetObjectReferenceCurve(animClip, curveBinding, keyFrames);
        AssetDatabase.CreateAsset(animClip, "Assets/Resources/Fights/001/test2.anim");
    }
}
