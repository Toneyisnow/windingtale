using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using Assets.Editor;

public class CreateFightAnimators : MonoBehaviour
{

    
    [MenuItem("Assets/Create Fight Animators")]
    static void CreateFightAnimator()
    {
        // Creates the controller
        var controller = AnimatorController.CreateAnimatorControllerAtPath("Assets/Resources/Fights/001/a001.controller");

        // Add parameters
        controller.AddParameter("actionState", AnimatorControllerParameterType.Int);

        // Add StateMachines
        var rootStateMachine = controller.layers[0].stateMachine;

        // Add States
        var stateIdle = rootStateMachine.AddState("anim1");
        var stateAttack = rootStateMachine.AddState("anim2");

        // Add Motions
        Motion idleMotion = Resources.Load<Motion>("Fights/001/anim1");
        Motion attackMotion = Resources.Load<Motion>("Fights/001/anim2");
        stateIdle.motion = idleMotion;
        stateAttack.motion = attackMotion;

        // Not needed for this
        // rootStateMachine.AddEntryTransition(stateIdle);

        // Add Transitions
        var trans1 = stateIdle.AddTransition(stateAttack);
        trans1.AddCondition(AnimatorConditionMode.Equals, 1, "actionState");
        trans1.duration = 0;

        var trans2 = stateAttack.AddTransition(stateIdle);
        trans2.AddCondition(AnimatorConditionMode.Equals, 0, "actionState");
        trans2.duration = 0;

        
    }

    
    [MenuItem("Assets/Create Fight Animation Clip")]
    static void CreateFlightAnimationClip()
    {
        CreateAnimationClip(1, 4);
        CreateAnimationClip(2, 10);
    }
    
    private static void CreateAnimationClip(int type, int frameCount)
    {
        // First you need to create e Editor Curve Binding
        EditorCurveBinding curveBinding = new EditorCurveBinding();

        // I want to change the sprites of the sprite renderer, so I put the typeof(SpriteRenderer) as the binding type.
        curveBinding.type = typeof(SpriteRenderer);
        // Regular path to the gameobject that will be changed (empty string means root)
        curveBinding.path = "";
        // This is the property name to change the sprite of a sprite renderer
        curveBinding.propertyName = "m_Sprite";

        AnimationClip animClip = new AnimationClip();

        // An array to hold the object keyframes
        ObjectReferenceKeyframe[] keyFrames = new ObjectReferenceKeyframe[frameCount];
        for (int i = 0; i < frameCount; i++)
        {
            Sprite sprite = Resources.Load<Sprite>(string.Format("Fights/001/Fight-001-{0}-{1}", type, StringUtils.Digit2(i + 1)));

            keyFrames[i] = new ObjectReferenceKeyframe();
            // set the time
            keyFrames[i].time = 0.15f * i;
            // set reference for the sprite you want
            keyFrames[i].value = sprite;
            Debug.LogWarning(sprite);
        }
        AnimationUtility.SetObjectReferenceCurve(animClip, curveBinding, keyFrames);
        AssetDatabase.CreateAsset(animClip, string.Format(@"Assets/Resources/Fights/001/anim{0}.anim", type));
    }
}
